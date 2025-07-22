import os
import glob
import json
import random
from PIL import Image
import math
import numpy as np
import pandas as pd
from scipy.signal import savgol_filter
from matplotlib.path import Path
import matplotlib.pyplot as plt
from scipy.spatial import ConvexHull, Delaunay
from tqdm import tqdm
from concurrent.futures import ProcessPoolExecutor
import uuid

# ------------GLOBAL PARAMETERS STARTS------------ 
directory_path = './IN-PATH-TRAJECTORIES/'
output_images_path = './OUT-PATH-IMAGES/Images/'
output_labels_file_path = './OUT-PATH-LABELS/labels.json'
width, height = 64, 64
channels = 5 # 1:Vx, 2:Vz, 3:DFG, 4:GROUP, 5:CONNECT
norm_factor = 6.5
scale_factor =  width / (2 * norm_factor)
interval = 0.04
step = 2
timestep = interval * step
group_distance = 3.6
poi_distance = 2.5
max_neighbors = 5
max_speed = 2.5
max_env_distance = 15
max_dfg_distance = 5
# -------------GLOBAL PARAMETERS ENDS-------------

# ----------------CLASSES STARTS------------------
class Agent:
    def __init__(self, spawn_pos, goal_pos):
        self.spawn_pos = spawn_pos
        self.goal_pos = goal_pos
        self.timesteps = []
        self.positions = []
        self.speeds = []
        self.stop_group_intervals = []
        self.stop_poi_durations = []
        self.group_poi_ratio = 1
        self.velocity_x = []
        self.velocity_z = []

    def add_position(self, timestep_local, position, speed):
        if len(self.positions) > 0:
            delta_x = position[0] - self.positions[-1][0]
            delta_z = position[1] - self.positions[-1][1]
            v_x = delta_x / timestep
            v_z = delta_z / timestep 
            self.velocity_x.append(v_x)
            self.velocity_z.append(v_z)
        else:
            self.velocity_x.append(0)
            self.velocity_z.append(0)

        self.timesteps.append(timestep_local)
        self.positions.append(position)
        self.speeds.append(speed)

    def path_distance(self):
        return euclidean_distance(self.spawn_pos, self.goal_pos)

    def detect_stationary(self):
        for current_time, current_agent_position, speed in zip(self.timesteps, self.positions, self.speeds):
            if current_time <= 3:
                continue
            if speed < 0.5:
                if len(self.stop_group_intervals) == 0:
                    self.stop_group_intervals.append([current_time - timestep, current_time, current_agent_position])
                else:
                    self.stop_group_intervals[-1][1] = current_time
                    self.stop_group_intervals[-1][2] = current_agent_position
                    if euclidean_distance(current_agent_position, self.stop_group_intervals[-1][2]) >= 4:
                        self.stop_group_intervals.append([current_time - timestep, current_time, current_agent_position])
            elif len(self.stop_group_intervals) > 0:
                self.stop_group_intervals[-1][1] = current_time
                self.stop_group_intervals[-1][2] = current_agent_position

    def compute_deviation(self, position):
        x1, y1 = self.spawn_pos
        x2, y2 = self.goal_pos
        x0, y0 = position
        A = y2 - y1
        B = x1 - x2

        if A < 0.0001 and B < 0.0001:
            return 0

        C = (x2*y1) - (x1*y2)
        return abs(A*x0 + B*y0 + C) / (A**2 + B**2)**0.5

    def average_deviation(self):
        total_deviation = sum(self.compute_deviation(pos) for pos in self.positions)
        return total_deviation / len(self.positions)

    def create_dfg_line(self, n=1):
        line_points = create_line_between_points(self.spawn_pos, self.goal_pos)
        return line_points

class Cluster:
    def __init__(self, agents):
        self.agents = agents
        self.interpersonal_distances = self.calculate_interpersonal_distances()
        self.center_of_mass_trajectory = self.calculate_center_of_mass_trajectory()

    def euclidean_distance(self, pos1, pos2):
        return np.linalg.norm(np.array(pos1) - np.array(pos2))

    def calculate_interpersonal_distances(self):
        distances_per_timestep = []

        max_timesteps = max([len(agent.positions) for agent in self.agents])
        for timestep in range(max_timesteps):
            total_distance = 0
            num_pairs = 0

            # Gather agents that have a position at this timestep
            valid_agents = [agent for agent in self.agents if len(agent.positions) > timestep]

            for i, agent1 in enumerate(valid_agents):
                for j, agent2 in enumerate(valid_agents):
                    if i < j:  # Avoid comparing an agent with itself and double counting
                        total_distance += self.euclidean_distance(agent1.positions[timestep], agent2.positions[timestep])
                        num_pairs += 1

            avg_distance = total_distance / num_pairs if num_pairs != 0 else 0
            distances_per_timestep.append(avg_distance)

        return distances_per_timestep

    def calculate_center_of_mass_trajectory(self):
        trajectory = []

        max_timesteps = max([len(agent.positions) for agent in self.agents])
        for timestep in range(max_timesteps):
            sum_x, sum_y = 0, 0
            num_valid_agents = 0  # Number of agents that have a position at this timestep

            for agent in self.agents:
                if len(agent.positions) > timestep:
                    sum_x += agent.positions[timestep][0]
                    sum_y += agent.positions[timestep][1]
                    num_valid_agents += 1

            if num_valid_agents > 0:
                center_of_mass = (sum_x / num_valid_agents, sum_y / num_valid_agents)
                trajectory.append(center_of_mass)
            else:
                # No agents have positions for this timestep; append None or a placeholder
                trajectory.append(None)

        return trajectory
# -----------------CLASSES ENDS-------------------

#--------------UTIL FUNCTIONS STARTS--------------
def euclidean_distance(pos1, pos2):
    return np.sqrt((pos2[0] - pos1[0]) ** 2 + (pos2[1] - pos1[1]) ** 2)

def get_normalized_distance(point1, point2, max_distance=3.6):
    """Return the normalized distance between two points in the range [0,1]."""
    distance = euclidean_distance(point1, point2)
    normalized_distance = distance / max_distance
    return normalized_distance

def calculate_speed(distance):
    return distance / timestep

def bresenham_line(x1, y1, x2, y2):
    """Generate points forming a line between (x1, y1) and (x2, y2)."""
    points = []
    dx = abs(x2 - x1)
    dy = abs(y2 - y1)
    sx = 1 if x1 < x2 else -1
    sy = 1 if y1 < y2 else -1
    err = dx - dy

    while True:
        points.append((x1, y1))
        if x1 == x2 and y1 == y2:
            break
        e2 = 2 * err
        if e2 > -dy:
            err -= dy
            x1 += sx
        if e2 < dx:
            err += dx
            y1 += sy
    return points

def create_line_between_points(pos_a, pos_b):
    line_points = []
    point_a_x = np.clip(math.floor((pos_a[0] + norm_factor) * scale_factor), 0, width - 1)
    point_a_z = np.clip(math.floor((pos_a[1] + norm_factor) * scale_factor), 0, height - 1)
    point_b_x = np.clip(math.floor((pos_b[0] + norm_factor) * scale_factor), 0, width - 1)
    point_b_z = np.clip(math.floor((pos_b[1] + norm_factor) * scale_factor), 0, height - 1)

    line_points = bresenham_line(point_a_x, point_a_z, point_b_x, point_b_z)
    return line_points

def is_inside(p, hull):
    """
    Checks if point p is inside the convex hull.
    """
    hull_delaunay = Delaunay(hull.points)
    return hull_delaunay.find_simplex(p) >= 0

def get_enclosed_points(points, grid_size=1):
    """
    Returns all points in the grid that are inside the convex hull of the given points.
    """
    # Compute the convex hull of the points
    hull = ConvexHull(points)
    hull_path = Path(hull.points[hull.vertices])
    # Calculate the bounding box of the convex hull
    min_x, min_y = np.min(points, axis=0)
    max_x, max_y = np.max(points, axis=0)
    # Create a grid within the bounding box
    xx, yy = np.mgrid[min_x:max_x:grid_size, min_y:max_y:grid_size]
    grid_points = np.c_[xx.ravel(), yy.ravel()]
    # Determine which grid points are inside the convex hull
    inside = hull_path.contains_points(grid_points)
    # Extract enclosed points
    enclosed_points = grid_points[inside]
    return [tuple(point) for point in enclosed_points]

def fill_polygon(grid, groups, max_duration=18):
    grid_quantity = np.zeros((width, height))
    for group in groups:
        agent_positions = group["points"] # Now, we get positions from the group object
        duration = group["duration"]  # We get the duration from the group object

        if len(agent_positions) > 2:
            for i in range(len(agent_positions) - 1):
                line_points = create_line_between_points(agent_positions[i], agent_positions[i+1])
                if line_points is None:
                     continue
                for point in line_points:
                    x, y = point
                    # Normalize the distance for this point
                    grid[x, y] += np.clip(duration / max_duration, 0, 1)
                    grid_quantity[x, y] += 1
        elif len(agent_positions) == 2:
            line_points = create_line_between_points(agent_positions[0], agent_positions[1])
            if line_points is None:
                    continue
            for point in line_points:
                x, y = point
                # Normalize the distance for this point
                grid[x, y] += np.clip(duration / max_duration, 0, 1)
                grid_quantity[x, y] += 1

    mask = grid_quantity > 0
    grid[mask] = grid[mask] / grid_quantity[mask]
    return grid

def cluster_agents(agents, threshold=3.0, traj_similarity = 0.6, timestep_threshold=2):
    def similar_trajectory(agent1, agent2):
        similar_points = 0
        for t1, pos1 in enumerate(agent1.positions):
            for t2, pos2 in enumerate(agent2.positions):
                if abs(agent1.timesteps[t1] - agent2.timesteps[t2]) <= timestep_threshold:
                    if euclidean_distance(pos1, pos2) < threshold:
                        similar_points += 1
                        break
        percentage_similarity = similar_points / len(agent1.positions)
        return percentage_similarity > traj_similarity

    clusters = {}
    cluster_id = 0
    for agent1 in agents:
        for agent2 in agents:
            if agent1 == agent2:
                continue
            if similar_trajectory(agent1, agent2):
                if cluster_id not in clusters:
                    clusters[cluster_id] = set()
                clusters[cluster_id].add(agent1)
                clusters[cluster_id].add(agent2)
        cluster_id += 1
    return clusters

def overlapping_time(interval1, interval2):
    # Returns the overlapping duration between two intervals
    return max(0, min(interval1[1], interval2[1]) - max(interval1[0], interval2[0]))

def is_within_distance(point1, point2, distance):
    # Check if two points are within a certain distance
    return ((point1[0] - point2[0])**2 + (point1[1] - point2[1])**2)**0.5 <= distance

def group_agents(agents):
    groups = []

    for i, agent in enumerate(agents):
        for interval in agent.stop_group_intervals:
            for j, other_agent in enumerate(agents):
                # Avoid checking the agent against itself and avoid checking pairs more than once
                if i < j:
                    for other_interval in other_agent.stop_group_intervals:
                        overlap_duration = overlapping_time(interval, other_interval)
                        if overlap_duration > 0 and is_within_distance(interval[2], other_interval[2], group_distance):

                            agent_group = next((g for g in groups if agent in g["agents"]), None)
                            other_agent_group = next((g for g in groups if other_agent in g["agents"]), None)

                            if not agent_group and not other_agent_group:  # Both agents are not in any group
                                groups.append({
                                    "agents": {agent, other_agent},
                                    "points": [interval[2], other_interval[2]],
                                    "duration": overlap_duration
                                })

                            elif agent_group and not other_agent_group:  # Only agent is in a group
                                agent_group["agents"].add(other_agent)
                                agent_group["points"].append(other_interval[2])

                            elif not agent_group and other_agent_group:  # Only other_agent is in a group
                                other_agent_group["agents"].add(agent)
                                other_agent_group["points"].append(interval[2])

    # Convert agent sets to lists and remove duplicate points
    for group in groups:
        group["points"] = list(set(group["points"]))
        group["agents"] = list(group["agents"])

    return groups

def draw_clusters(clusters, img):
    img_quantity = np.zeros((width, height))
    if len(clusters) == 0:
        return

    max_distance = group_distance
    min_distance = 0

    for cluster in clusters:
        trajectory = cluster.center_of_mass_trajectory
        distances = cluster.interpersonal_distances

        for i in range(len(trajectory) - 1):
            start_point = trajectory[i]
            end_point = trajectory[i + 1]

            if start_point is not None and end_point is not None:
                # Get all points for the line segment between the two points
                line_points = create_line_between_points(start_point, end_point)
                if line_points is None:
                    continue

                for point in line_points:
                    x, z = point
                    # Normalize the distance for this point
                    distances = np.clip(distances, 0, max_distance)
                    value = (distances[i] - min_distance) / (max_distance - min_distance)
                    img[x][z] += normalize((1 - value), 0, 1, 0.1, 1)
                    img_quantity[x][z] += 1
    mask = img_quantity > 0
    img[mask] = img[mask] / img_quantity[mask]
    return img

def distance_to_poi(agent_pos, poi):
    x, z = agent_pos
    x_center, z_center, x_dim, z_dim = poi
    x_min = x_center - x_dim/2
    x_max = x_center + x_dim/2
    z_min = z_center - z_dim/2
    z_max = z_center + z_dim/2
    
    # Check if agent is inside the rectangle
    if x_min <= x <= x_max and z_min <= z <= z_max:
        return 0
    
    # If agent is outside, calculate distance to each corner and edge, then take minimum
    corners = [(x_min, z_min), (x_min, z_max), (x_max, z_min), (x_max, z_max)]
    distances = [euclidean_distance(agent_pos, corner) for corner in corners]
    return min(distances)

def normalize_array(arr, new_min, new_max):
    normalized_arr = np.zeros_like(arr)
    for channel in range(arr.shape[2]):
        arr_min = np.min(arr[:, :, channel])
        arr_max = np.max(arr[:, :, channel])
        if (arr_max - arr_min) > 0:
            normalized_arr[:, :, channel] = new_min + (arr[:, :, channel] - arr_min) * (new_max - new_min) / (arr_max - arr_min)  
    return normalized_arr

def normalize(value, min_value, max_value, new_min, new_max):
    normalized = ((value - min_value) / (max_value - min_value)) * (new_max - new_min) + new_min
    return normalized

def print_grid_image(img, name, weights):
    title = "Weights: (Goal: " + format(weights[0], '.2f') + ", Group: " + format(weights[1], '.2f') + ", Interact: " + format(weights[2], '.2f') + ", Conn: " + format(weights[3], '.2f') + ")"   
    # Split the channels
    paths = img[:,:,0].copy()
    paths[np.abs(paths - 0.5) > 1e-6] = 0
    paths[np.abs(paths - 0.5) <= 1e-6] = 1
    channels = [img[:,:,i] for i in range(img.shape[2])] + [paths]
    combined_img = np.stack(channels, axis=2)
    
    # Set up a grid
    fig, axarr = plt.subplots(2, 3, figsize=(12, 12))
    labels = ["VEL_X", "VEL_Z", "DFG", "GROUP", "CONNECT", "PATHS"]    
    for i, ax in enumerate(axarr.ravel()):
        if i < 6:
            ax.imshow(channels[i], cmap='gray', vmin=0, vmax=1)
            ax.set_title(labels[i])
        ax.axis('off')    
    # Add a title on top of the exported image
    fig.suptitle(title, fontsize=14)    
    # Adjust layout and space for title
    plt.tight_layout()
    fig.subplots_adjust(top=0.925)  # Adjust this value as needed to position the title correctly
    
    # Save the grid visualization as an image
    plt.savefig(name + '.png', dpi=300)
    plt.close()

def save_demo_image(img, name, shift_x=10, shift_y=10, border_size=1):
    canvas_size_x = img.shape[1] + img.shape[2] * shift_x + 2 * border_size
    canvas_size_y = img.shape[0] + img.shape[2] * shift_y + 2 * border_size
    canvas = np.zeros((canvas_size_y, canvas_size_x), dtype=np.uint8) + 255

    # Loop through each channel and add it to the canvas
    for channel in range(img.shape[2] - 1, -1, -1):
        # Extract the channel and scale it to [0, 255]
        channel_img = (img[:, :, channel] * 255).astype(np.uint8)

        # Create a bordered image for the channel
        bordered_img = np.full((img.shape[0] + 2 * border_size, img.shape[1] + 2 * border_size), 255, dtype=np.uint8)
        bordered_img[border_size:-border_size, border_size:-border_size] = channel_img

        # Determine where to place the bordered image on the canvas
        start_x = max(0, (img.shape[2] - 1 - channel) * shift_x)
        start_y = max(0, (img.shape[2] - 1 - channel) * shift_y)
        end_x = start_x + bordered_img.shape[1]
        end_y = start_y + bordered_img.shape[0]

        # Place the bordered image on the canvas
        canvas[start_y:end_y, start_x:end_x] = bordered_img

    # Save the image
    output_path = name + '_demo.png'
    Image.fromarray(canvas).save(output_path)
#---------------UTIL FUNCTIONS ENDS---------------

#---------IMAGE CREATION FUNCTIONS STARTS---------
def add_environment(input_data, environment, channel):    
    input_data[:,:,channel] = environment

def add_velocity_x(input_data, velocity_x, channel):    
    input_data[:,:,channel] = velocity_x

def add_velocity_z(input_data, velocity_z, channel):    
    input_data[:,:,channel] = velocity_z

def add_dfg(input_data, dfg, channel):
    input_data[:,:,channel] = dfg

def add_groups(input_data, groups, channel):
    input_data[:,:,channel] = groups

def add_connect(input_data, connect, channel):
    input_data[:,:,channel] = connect

def add_poi(input_data, poi, channel):
    input_data[:,:,channel] = poi

def create_image(env_grid, velocity_x_grid, velocity_z_grid, dfg_grid, group_grid, connect_grid, poi_grid, weights, filename, local_labels):
    input_data = np.zeros((width, height, 5), np.float16)

    add_velocity_x(input_data, velocity_x_grid, 0)
    add_velocity_z(input_data, velocity_z_grid, 1)
    add_dfg(input_data, dfg_grid, 2)
    add_groups(input_data, group_grid, 3)   
    add_connect(input_data, connect_grid, 4)
    cw, ch = 64, 64
    stride = 64
    for x in range(0, height-cw+1, stride):
        for y in range(0, width-ch+1, stride):
            img_out = input_data[y:y+ch, x:x+cw, :]
            if np.sum(img_out[:,:,0] != 0.5) / (cw * ch) > 0.01:
                filename_temp = filename + "_" + str(x) + "_" + str(y)
                name = output_images_path + filename_temp
                local_labels[filename_temp] = weights
                np.savez_compressed(name, img_out)
                #print_grid_image(img_out, name, weights)
                #save_demo_image(img_out, name)    

def process_item(item):
    local_labels = {}

    with open(directory_path + item + "/env.json") as json_file:
        data_json = json.load(json_file)
        parameters_json = data_json["ParametersGrid"]
        environment_json = data_json["EnvironmentGrid"]            

        weights = (parameters_json[0]['goal'], parameters_json[0]['group'], parameters_json[0]['interaction'], parameters_json[0]['interconn'])

        env_grid = np.zeros((width, height), np.float16)
        pois_array = []
        # Draw objects
        for obj in environment_json:
            pos_x = float(obj['pos_x'])
            pos_z = float(obj['pos_z'])
            scale_x = float(obj['scale_x'])
            scale_z = float(obj['scale_z'])
            x = math.floor(np.clip(((pos_x + norm_factor) * scale_factor), 0, width - 1))
            z = math.floor(np.clip(((pos_z + norm_factor) * scale_factor), 0, height - 1))
            range_x = math.floor((scale_x * scale_factor) / 2)
            range_z = math.floor((scale_z * scale_factor) / 2)
            env_object = float(obj['type'])
            env_grid[x-range_x:x+range_x+1, z-range_z:z+range_z+1] = env_object
            if env_object > 0.75:
                pois_array.append((pos_x, pos_z, scale_x, scale_z))

    # List all csv files in current run dir
    csv_files = glob.glob(directory_path + item + "/*.csv")
    
    agents = []
    for file_name in csv_files:
        csv_data = pd.read_csv(file_name, names=['timestep', 'pos_x', 'pos_z', 'a', 'b'], sep=';', skiprows=1)
        
        poly_order = 4 # Polynomial order, usually between 2 and 6
        window_size = 191  # Must be an odd integer
        csv_size = len(csv_data)
        if csv_size >= window_size:
            smoothed_timestep = savgol_filter(csv_data['timestep'], window_size, poly_order)
            smoothed_pos_x = savgol_filter(csv_data['pos_x'], window_size, poly_order)
            smoothed_pos_z = savgol_filter(csv_data['pos_z'], window_size, poly_order)
        else:
            smoothed_timestep = csv_data['timestep'].values
            smoothed_pos_x = csv_data['pos_x'].values
            smoothed_pos_z = csv_data['pos_z'].values

        smoothed_data = pd.DataFrame({'timestep': smoothed_timestep, 'pos_x': smoothed_pos_x, 'pos_z': smoothed_pos_z})

        second_row = smoothed_data.iloc[1]
        last_row = smoothed_data.iloc[-1]
        spawn_pos = (float(second_row['pos_x']), float(second_row['pos_z']))
        goal_pos = (float(last_row['pos_x']), float(last_row['pos_z']))

        agent = Agent(spawn_pos, goal_pos)

        for row_index, row in smoothed_data.iloc[1:].iterrows():
            if row_index % step == 0:
                timestep = float(row['timestep']) 
                x_raw = float(row['pos_x'])
                z_raw = float(row['pos_z'])
                current_agent_position = (x_raw, z_raw)

                # Calculate speed                
                if len(agent.positions) == 0:
                    speed = 1
                else:
                    dis = euclidean_distance(agent.positions[-1], (x_raw, z_raw))
                    speed = calculate_speed(dis)
                speed = np.clip(speed, 0, max_speed)

                agent.add_position(timestep, current_agent_position, speed)

        agents.append(agent)        

    # CALCULATE VELOCITY GRID
    velocity_x_grid = np.zeros((width, height)) + 0.5
    velocity_z_grid = np.zeros((width, height)) + 0.5
    velocity_grid_quantity = np.zeros((width, height))
    for agent in agents:
        for i, point in enumerate(agent.positions):
            x = math.floor((point[0] + norm_factor) * scale_factor)
            y = math.floor((point[1] + norm_factor) * scale_factor)
            if x < 0 or y < 0 or x >= width or y >= height:
                continue 
            v_x = agent.velocity_x[i] / max_speed
            v_z = agent.velocity_z[i] / max_speed
            v_x_norm = normalize(v_x, -1, 1, -0.5, 0.5)
            v_z_norm = normalize(v_z, -1, 1, -0.5, 0.5)
            if abs(v_x_norm) < 0.01:
                v_x_norm += 0.05
            if abs(v_z_norm) < 0.01:
                v_z_norm += 0.05    
            velocity_x_grid[x, y] += v_x_norm
            velocity_z_grid[x, y] += v_z_norm
            velocity_grid_quantity[x, y] += 1
    mask = velocity_grid_quantity > 0
    velocity_x_grid[mask] = ((velocity_x_grid[mask] - 0.5) / velocity_grid_quantity[mask]) + 0.5
    velocity_z_grid[mask] = ((velocity_z_grid[mask] - 0.5) / velocity_grid_quantity[mask]) + 0.5

    # CALCULATE DFG GRID
    dfg_grid = np.zeros((width, height))
    dfg_grid_quantity = np.zeros((width, height))
    for agent in agents:
        line_points = agent.create_dfg_line()
        avg_dfg = agent.average_deviation() / max_dfg_distance
        path_dfg = agent.path_distance() / max_env_distance
        dfg = (0.3 * avg_dfg + 0.7 * path_dfg) / 2
        if line_points is None:
                    continue
        for point in line_points:
            x, y = point
            dfg_grid[x, y] = np.clip(1 - dfg, 0, 1)
            dfg_grid_quantity[x, y] += 1
    mask = dfg_grid_quantity > 0
    dfg_grid[mask] = dfg_grid[mask] / dfg_grid_quantity[mask]

    # CALCULATE GROUP GRID
    for agent in agents:
        agent.detect_stationary()    
    groups = group_agents(agents)
    group_grid = np.zeros((width, height))
    fill_polygon(group_grid, groups)
    for x in range(env_grid.shape[0]):
        for y in range(env_grid.shape[1]):
            if env_grid[x, y] > 0:
                group_grid[x, y] = env_grid[x, y]

    # CALCULATE CONNECT GRID
    connect_grid = np.zeros((width, height))
    clusters = cluster_agents(agents, threshold=2.5)
    clusters_objects = [Cluster(cluster) for cluster in clusters.values()]
    draw_clusters(clusters_objects, connect_grid)

    # CALCULATE DPOI GRID
    poi_grid = np.zeros((width, height))

    unique_id = uuid.uuid4().hex
    create_image(env_grid, velocity_x_grid, velocity_z_grid, dfg_grid, group_grid, connect_grid, poi_grid, weights, "img_" + item + str(unique_id), local_labels)
    return local_labels
#----------IMAGE CREATION FUNCTIONS ENDS----------

if __name__ == '__main__':
    # Get a list of all the items (files and directories) in the directory
    items = os.listdir(directory_path)
    random.shuffle(items)
    parts = items[:]

    with ProcessPoolExecutor(max_workers=12) as executor:
        labels = list(tqdm(executor.map(process_item, parts), total=len(parts)))
        
    merged_data = {}
    for d in labels:
        for key, value_tuple in d.items():
            merged_data[key] = {
                "wg": value_tuple[0],
                "wgr": value_tuple[1],
                "wi": value_tuple[2],
                "wc": value_tuple[3]
            }
    os.makedirs(os.path.dirname(output_labels_file_path), exist_ok=True)

    # Write the JSON file
    with open(output_labels_file_path, 'w') as json_file:
        json.dump(merged_data, json_file, indent=4)