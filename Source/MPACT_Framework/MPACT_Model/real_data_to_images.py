import os
import math
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
from sklearn.cluster import DBSCAN
import json
from tqdm.auto import tqdm
from inference import predict
import time

current_file_dir = os.path.dirname(os.path.abspath(__file__))
width, height = 64, 64
max_dfg_distance = 5
max_env_distance = 15
max_speed_norm = 1
class Agent:
    def __init__(self):
        self.spawn_pos = None
        self.goal_pos = None
        self.timesteps = []
        self.positions = []
        self.speeds = []
        self.stop_group_intervals = []
        self.stop_poi_durations = []
        self.group_poi_ratio = 1
        self.timestep = 0.04
        self.velocity_x = []
        self.velocity_z = []

    def add_trajectory_point(self, point, timestep, speed, vel_x, vel_z):
        self.timestep = timestep
        if self.spawn_pos is None:
            self.spawn_pos = point
        self.goal_pos = point
        speed = np.clip(speed, 0, max_speed_norm)

        self.velocity_x.append(vel_x)
        self.velocity_z.append(vel_z)
        self.timesteps.append(timestep)
        self.positions.append(point)
        self.speeds.append(speed)

    def path_distance(self):
        return euclidean_distance(self.spawn_pos, self.goal_pos)

    def detect_stationary(self):
        N = 5
        for index in range(0, len(self.timesteps), N): 
            current_time = self.timesteps[index]
            current_agent_position = self.positions[index]
            window_speeds = self.speeds[index:index + N]
            avg_speed = sum(window_speeds) / float(N)

            if index <= 10 or index >= (len(self.timesteps) - 10):
                continue
            if avg_speed < 0.1:
                if len(self.stop_group_intervals) == 0:
                    self.stop_group_intervals.append([current_time - self.timestep, current_time, current_agent_position])
                else:
                    self.stop_group_intervals[-1][1] = current_time
                    self.stop_group_intervals[-1][2] = current_agent_position
                    if euclidean_distance(current_agent_position, self.stop_group_intervals[-1][2]) >= 1:
                        self.stop_group_intervals.append([current_time - self.timestep, current_time, current_agent_position])
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

class Cell:
    def __init__(self):
        self.agents = []
        self.multiplier_width = (0,1)
        self.multiplier_height = (0,1)
        self.velocity_x_grid = None
        self.velocity_z_grid = None
        self.dfg_grid = None
        self.group_grid = None
        self.connect_grid = None
        self.coordinates = None

    def set_multiplier(self, m_width, m_height):
        self.multiplier_width = m_width
        self.multiplier_height = m_height

    def add_agent(self, agent):
        self.agents.append(agent)

    def group_agents(self, group_distance=0.25):
        groups = []

        for i, agent in enumerate(self.agents):
            for interval in agent.stop_group_intervals:
                for j, other_agent in enumerate(self.agents):
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

                                elif agent_group != other_agent_group:  # Both agents are in different groups
                                    # Merge groups
                                    agent_group["agents"].update(other_agent_group["agents"])
                                    agent_group["points"].extend(other_agent_group["points"])
                                    agent_group["duration"] += overlap_duration  # Adjust duration
                                    groups.remove(other_agent_group)

        # Convert agent sets to lists and remove duplicate points
        for group in groups:
            group["points"] = list(set(group["points"]))
            group["agents"] = list(group["agents"])

        return groups

    def cluster_agents(self, threshold=0.2, traj_similarity=0.8, timestep_threshold=2):
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
        for agent1 in self.agents:
            for agent2 in self.agents:
                if agent1 == agent2:
                    continue
                if similar_trajectory(agent1, agent2):
                    if cluster_id not in clusters:
                        clusters[cluster_id] = set()
                    clusters[cluster_id].add(agent1)
                    clusters[cluster_id].add(agent2)
            cluster_id += 1
        return clusters
    
    def build_velocity_grid(self):
        self.velocity_x_grid = np.zeros((width, height)) + 0.5
        self.velocity_z_grid = np.zeros((width, height)) + 0.5
        velocity_grid_quantity = np.zeros((width, height))
        for agent in self.agents:
            for i, point in enumerate(agent.positions):
                x = math.floor(point[0] * width)
                y = math.floor(point[1] * height)
                if x < 0 or y < 0 or x >= width or y >= height:
                    continue 
                v_x = agent.velocity_x[i] / (max_speed_norm / 2)
                v_z = agent.velocity_z[i] / (max_speed_norm / 2)
                v_x_norm = normalize(v_x, -1, 1, -0.5, 0.5)
                v_z_norm = normalize(v_z, -1, 1, -0.5, 0.5)
                if abs(v_x_norm) < 0.01:
                    v_x_norm += 0.05
                if abs(v_z_norm) < 0.01:
                    v_z_norm += 0.05    
                self.velocity_x_grid[y, x] += v_x_norm
                self.velocity_z_grid[y, x] += v_z_norm
                velocity_grid_quantity[y, x] += 1
        mask = velocity_grid_quantity > 0
        self.velocity_x_grid[mask] = ((self.velocity_x_grid[mask] - 0.5) / velocity_grid_quantity[mask]) + 0.5
        self.velocity_z_grid[mask] = ((self.velocity_z_grid[mask] - 0.5) / velocity_grid_quantity[mask]) + 0.5

    def build_dfg_grid(self):
        self.dfg_grid = np.zeros((width, height))
        dfg_grid_quantity = np.zeros((width, height))
        for agent in self.agents:
            line_points = agent.create_dfg_line()
            avg_dfg = agent.average_deviation()
            path_dfg = agent.path_distance()
            dfg = (0.3 * avg_dfg + 0.7 * path_dfg) / 2
            if line_points is None:
                continue
            for point in line_points:
                x, y = point
                self.dfg_grid[y, x] = np.clip(1 - dfg, 0, 1)
                dfg_grid_quantity[y, x] += 1
        mask = dfg_grid_quantity > 0
        self.dfg_grid[mask] = self.dfg_grid[mask] / dfg_grid_quantity[mask]

    def build_groups_grid(self, env_grid):
        x = self.coordinates[0]
        z = self.coordinates[1]
        env_part = env_grid[x*64:(x+1)*64, z*64:(z+1)*64]     

        for agent in self.agents:
            agent.detect_stationary()    
        groups = self.group_agents()
        self.group_grid = np.zeros((width, height))
        fill_polygon(self.group_grid, groups)
        for x in range(width):
            for y in range(height):
                if env_part[x, y] > 0:
                    self.group_grid[x, y] = env_part[x, y]

    def build_connect_grid(self):
        self.connect_grid = np.zeros((width, height))
        clusters = self.cluster_agents()
        clusters_objects = [Cluster(cluster) for cluster in clusters.values()]
        draw_clusters(clusters_objects, self.connect_grid)

def euclidean_distance(pos1, pos2):
    return np.sqrt((pos2[0] - pos1[0]) ** 2 + (pos2[1] - pos1[1]) ** 2)

def calculate_speed(distance, timestep):
    return distance / timestep

def overlapping_time(interval1, interval2):
    # Returns the overlapping duration between two intervals
    return max(0, min(interval1[1], interval2[1]) - max(interval1[0], interval2[0]))

def is_within_distance(point1, point2, distance):
    # Check if two points are within a certain distance
    return ((point1[0] - point2[0])**2 + (point1[1] - point2[1])**2)**0.5 <= distance

def normalize(value, min_value, max_value, new_min, new_max):
    normalized = ((value - min_value) / (max_value - min_value)) * (new_max - new_min) + new_min
    return normalized

def get_grid_separation(width, height, multiplier):
    divisor = math.gcd(width, height)
    width_normalized = width / divisor
    height_normalized = height / divisor
    larger_value = max(width_normalized, height_normalized)
    width_normalized /= larger_value
    height_normalized /= larger_value
    width_normalized = math.floor(multiplier * width_normalized)
    height_normalized = math.floor(multiplier * height_normalized)
    return (width_normalized, height_normalized)

def skip_rows(index, step):
    return index % step != 0

def iqr_bounds(df, column_name, low, high):
    Q1 = df[column_name].quantile(low)
    Q3 = df[column_name].quantile(high)
    IQR = Q3 - Q1
    lower_bound = Q1 - 1.5 * IQR
    upper_bound = Q3 + 1.5 * IQR
    return lower_bound, upper_bound

def dist(a, b):
    """Calculates the Euclidean distance between two agents based on their spawn and goal positions."""
    dx = a[1] - b[1]
    dz = a[2] - b[2]
    gx = a[4] - b[4]
    gz = a[5] - b[5]
    return np.sqrt(dx*dx + dz*dz + gx*gx + gz*gz)

def frame_diff(a, b):
    """Calculates the difference in spawn and goal frames between two agents."""
    sf_diff = abs(a[0] - b[0])
    gf_diff = abs(a[3] - b[3])
    return sf_diff + gf_diff

def is_close(a, b, dist_threshold, frame_threshold):
    """Determines whether two agents are 'close' based on their distances and frame differences."""
    return dist(a, b) < dist_threshold and frame_diff(a, b) < frame_threshold

def detect_groups(agent_dict, dist_threshold=0.25, frame_threshold=25):
    group_id = 0
    agent_groups = {}
    for frame in agent_dict:
        for agent in agent_dict[frame]:
            if not agent_groups:
                agent.append(group_id)
                agent_groups[group_id] = [agent]
                group_id += 1
            else:
                added_to_group = False
                for gid, agents in agent_groups.items():
                    if any(is_close(agent, a, dist_threshold, frame_threshold) for a in agents):
                        agent.append(gid)
                        agent_groups[gid].append(agent)
                        added_to_group = True
                        break
                if not added_to_group:
                    agent.append(group_id)
                    agent_groups[group_id] = [agent]
                    group_id += 1
    return agent_dict

def read_csv_files_and_env_json(directory_path, row_step, framerate, separation):
    with open(directory_path + "env.json") as json_file:
        data_json = json.load(json_file)
        scene_objects_dict = data_json["EnvironmentObjects"]
        env_size_dict = data_json['EnvironmentParams']

    pos_x_min, pos_x_max = env_size_dict['min_width'], env_size_dict['max_width']
    pos_z_min, pos_z_max = env_size_dict['min_height'], env_size_dict['max_height']
    width_real = abs(pos_x_min) + abs(pos_x_max)
    height_real = abs(pos_z_min) + abs(pos_z_max)
    cell_width_real = width_real / separation[0]
    cell_height_real = height_real / separation[1]

    max_env_distance = np.sqrt(cell_width_real ** 2 + cell_height_real **  2)
    max_dfg_distance = max_env_distance / 3
    
    csv_files = [f for f in os.listdir(directory_path) if f.endswith('.csv')]
    # Create an empty dictionary to store the data and a list to store all DataFrames for concatenation
    csv_data_norm = {}
    all_dfs = []

    row_threshold = 20

    for filename in csv_files:
        # Read the CSV file into a pandas DataFrame and assign column names
        df = pd.read_csv(os.path.join(directory_path, filename), 
            header=None, names=['frame', 'pos_x', 'pos_z'], sep=';', 
            skiprows=lambda index: index == 0 or skip_rows(index, row_step),
            usecols=[0, 1, 2])
        if df.shape[0] < row_threshold:
            continue
        
        # Calculate the speed based on consecutive positions
        df['distance'] = np.sqrt((df['pos_x'].diff())**2 + (df['pos_z'].diff())**2)
        df['time_diff'] = df['frame'].diff()
        df['delta_x'] = df['pos_x'].diff()
        df['delta_z'] = df['pos_z'].diff()
        df['speed'] = df['distance'] / df['time_diff']
        # Calculate the change in position for x and z axes
        # Calculate velocities
        df['velocity_x'] = df['delta_x'] / df['time_diff']
        df['velocity_z'] = df['delta_z'] / df['time_diff']
        df.dropna(inplace=True)  # Remove the first row with NaN values due to the diff() calculations
        df.drop(columns=['distance', 'time_diff', 'delta_x', 'delta_z'], inplace=True)
        csv_data_norm[filename] = df
        all_dfs.append(df)

    # Concatenate all DataFrames into one large DataFrame
    all_data = pd.concat(all_dfs, ignore_index=True)

    # Calculate the lower and upper bounds for pos_x, pos_z, and speed columns
    pos_x_bounds = iqr_bounds(all_data, 'pos_x', 0.2, 0.8)
    pos_z_bounds = iqr_bounds(all_data, 'pos_z', 0.2, 0.8)
    speed_bounds = (all_data['speed'].min(), all_data['speed'].max())
    speed_min, speed_max = speed_bounds[0], speed_bounds[1]
    csv_data = csv_data_norm.copy()

    agent_dict = {}
    frame_interval = 1 / framerate
    for filename, df in csv_data_norm.items():
        # Normalize columns
        df = df[(df['pos_x'] >= pos_x_bounds[0]) & (df['pos_x'] <= pos_x_bounds[1])]
        df = df[(df['pos_z'] >= pos_z_bounds[0]) & (df['pos_z'] <= pos_z_bounds[1])]
        # Normalize columns
        df['pos_x'] = (df['pos_x'] - pos_x_min) / (pos_x_max - pos_x_min)
        df['pos_z'] = (df['pos_z'] - pos_z_min) / (pos_z_max - pos_z_min)
        df['speed'] = (df['speed'] / speed_max)
        df['velocity_x'] = (df['velocity_x'] / speed_max)
        df['velocity_z'] = (df['velocity_z'] / speed_max)
        start_frame = (int)(df['frame'].iloc[0] / frame_interval)
        start_pos_x, start_pos_z = df['pos_x'].iloc[0], df['pos_z'].iloc[0]
        end_frame = (int)(df['frame'].iloc[-1] / frame_interval)
        end_pos_x, end_pos_z = df['pos_x'].iloc[-1], df['pos_z'].iloc[-1] 
        agent_data = [start_frame, start_pos_x, start_pos_z, end_frame, end_pos_x, end_pos_z]
        if start_frame not in agent_dict:
                agent_dict[start_frame] = []
        agent_dict[start_frame].append(agent_data)

        csv_data_norm[filename] = df

    max_speeds = [df['speed'].max() for df in csv_data_norm.values()]
    max_speed_norm = max(max_speeds)

    sorted_agent_dict = {k: agent_dict[k] for k in sorted(agent_dict, key=int)}

    final_agent_dict = detect_groups(sorted_agent_dict)
    return csv_data, csv_data_norm, scene_objects_dict, final_agent_dict

def create_frame_dict(data):
    frame_dict = {}
    for human_id, df in data.items():
        for index, row in df.iterrows():
            frame = row['frame']
            pos_x = row['pos_x']
            pos_z = row['pos_z']
            speed = row['speed']
            if frame not in frame_dict:
                frame_dict[frame] = []
            frame_dict[frame].append((pos_z, pos_x, speed))
    return frame_dict

def point_to_cell(point, grid_dim):
    """Returns the cell row and column indices for a given point."""
    row = int(point[0] * grid_dim[0])
    col = int(point[1] * grid_dim[1])
    return (row, col)

def draw_clusters(clusters, img, group_distance=0.25):
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
                    value = distances[i]
                    img[z][x] += normalize((1 - value), 0, 1, 0.1, 1)
                    img_quantity[z][x] += 1
    mask = img_quantity > 0
    img[mask] = img[mask] / img_quantity[mask]
    return img

def fill_polygon(grid, groups, max_duration=15):
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
                    grid[y, x] += np.clip(duration / max_duration, 0, 1)
                    grid_quantity[y, x] += 1
        elif len(agent_positions) == 2:
            line_points = create_line_between_points(agent_positions[0], agent_positions[1])
            if line_points is None:
                    continue
            for point in line_points:
                x, y = point
                # Normalize the distance for this point
                grid[y, x] += np.clip(duration / max_duration, 0, 1)
                grid_quantity[y, x] += 1

    mask = grid_quantity > 0
    grid[mask] = grid[mask] / grid_quantity[mask]
    return grid

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
    point_a_x = math.floor(pos_a[0] * width)
    point_a_z = math.floor(pos_a[1] * height)
    point_b_x = math.floor(pos_b[0] * width)
    point_b_z = math.floor(pos_b[1] * height)

    if point_a_x < 0 or point_a_z < 0 or point_a_x > (width - 1) or point_a_z > (height - 1):
        return line_points
    if point_b_x < 0 or point_b_z < 0 or point_b_x > (width - 1) or point_b_z > (height - 1):
        return line_points

    line_points = bresenham_line(point_a_x, point_a_z, point_b_x, point_b_z)
    return line_points

def assign_agents_to_cells(csv_data, grid_dim, duration):
    cells = [[Cell() for _ in range(grid_dim[0])] for _ in range(grid_dim[1])]

    increase_width = 1 / grid_dim[0] 
    increase_height = 1 / grid_dim[1]

    for i in range(grid_dim[1]):
        for j in range(grid_dim[0]):
            multiplier_w = (j * increase_width, (j+1) * increase_width)
            multiplier_h = (i * increase_height, (i+1) * increase_height)
            cells[i][j].set_multiplier(multiplier_w, multiplier_h)
            cells[i][j].coordinates = (i, j)

    min_time = duration[0]
    max_time = duration[1]

    for agent_trajectory in csv_data.values():
        # Start and end are based on the entire trajectory for now
        start_point = agent_trajectory.iloc[1]
        end_point = agent_trajectory.iloc[-1]

        filtered_trajectory = agent_trajectory.loc[(agent_trajectory['frame'] >= min_time) & (agent_trajectory['frame'] <= max_time)]

        new_agent = Agent()
        last_cell = None
        for i, row in filtered_trajectory.iterrows():
            frame = row['frame']
            # TODO: Maybe fliP Z
            point = (row['pos_x'], 1 - row['pos_z'])
            cell_coordinates = point_to_cell(point, grid_dim)
            current_cell = cells[cell_coordinates[1]][cell_coordinates[0]]

            if i == 0:
                last_cell = cell_coordinates
                current_cell.add_agent(new_agent)          

            # Check if the current agent is already added to the cell
            if last_cell != cell_coordinates:
                new_agent = Agent()
                current_cell.add_agent(new_agent)

            last_cell = cell_coordinates

            cell_norm_x = normalize(point[0], current_cell.multiplier_width[0], current_cell.multiplier_width[1] , 0, 1)
            cell_norm_z = normalize(point[1], current_cell.multiplier_height[0], current_cell.multiplier_height[1] , 0, 1)
            cell_point = (cell_norm_x, cell_norm_z)

            current_cell.agents[-1].add_trajectory_point(cell_point, frame, row['speed'], row['velocity_x'], row['velocity_z'])
    return cells

def generate(path, name, frame_interval, row_step, separation, framerate, n_processes):
    csv_directory = current_file_dir + path
    csv_data, csv_data_norm, env_dict, agent_dict = read_csv_files_and_env_json(csv_directory, row_step, framerate, separation)

    env_grid = np.zeros((height * separation[1], width * separation[0]))
    built_env_grid(env_grid, env_dict)

    frame_dict = create_frame_dict(csv_data_norm)

    timestep = 1 / framerate
    max_timestep = max(df['frame'].max() for df in csv_data.values())
    max_frame = int(max_timestep / timestep)

    frame_cells = {}
    for frame in tqdm(range(0, max_frame, frame_interval), position=0, leave=True, desc="Reading: "):
        start_time = frame * timestep
        end_time = (frame + frame_interval) * timestep
    
        cells = assign_agents_to_cells(csv_data_norm, separation, (start_time, end_time))
        frame_cells[frame] = cells
   
    model_images = []
    model_masking = []
    for i, cells in tqdm(enumerate(frame_cells.values()), total=len(frame_cells.values()), position=0, leave=True, desc="Generating: "):
        create_cells_grid(cells, env_grid, model_images, model_masking)
      
    return agent_dict, model_images, model_masking, max_frame

def built_env_grid(env_grid, env_dict):
    width = env_grid.shape[1]
    height = env_grid.shape[0]

    for obj in env_dict:
        scale_x = obj['scale_x']
        scale_z = obj['scale_z']
        x = math.floor(obj['pos_x'] * width) - 1
        z = math.floor(obj['pos_z'] * height) - 1
        range_x = math.floor(scale_x * width) // 2
        range_z = math.floor(scale_z * height) // 2
        env_object = 1 if obj['type'] > 0.5 else 0
        z_min_range, z_max_range = np.clip(z - range_z, 0, height), np.clip(z + range_z + 1, 0, height)
        x_min_range, x_max_range = np.clip(x - range_x, 0, width), np.clip(x + range_x + 1, 0, width)
        env_grid[z_min_range:z_max_range, x_min_range:x_max_range] = env_object

def create_cells_grid(cells, env_grid, model_images, model_masking):
    rows = len(cells)
    cols = len(cells[0])

    for row_idx, row in enumerate(cells):
        for col_idx, cell in enumerate(row):
            model_image = np.zeros((width, height, 5)).astype(np.float32)
            cell.build_velocity_grid()
            cell.build_dfg_grid()
            cell.build_groups_grid(env_grid)
            cell.build_connect_grid()

            model_image[:,:,0] = cell.velocity_x_grid
            model_image[:,:,1] = cell.velocity_z_grid
            model_image[:,:,2] = cell.dfg_grid
            model_image[:,:,3] = cell.group_grid
            model_image[:,:,4] = cell.connect_grid

            model_images.append(model_image)
            if (np.sum(model_image[:,:,0] != 0.5) / (width * height)) > 0.02:
                model_masking.append(1)
            else:
                model_masking.append(0)

def plot_cell_grid(cells):
    rows = len(cells)
    cols = len(cells[0])
    
    fig, axes = plt.subplots(rows, cols, figsize=(10,10))
    
    for row_idx, row in enumerate(cells):
        for col_idx, cell in enumerate(row):
            img = cell.velocity_x_grid

            if rows > 1 and cols > 1:
                ax = axes[row_idx][col_idx]
            elif rows > 1:
                ax = axes[row_idx]
            elif cols > 1:
                ax = axes[col_idx]
            else:
                ax = axes

            ax.imshow(img, cmap='gray', vmin=0, vmax=1)
            ax.axis('off')
            
    plt.tight_layout()
    plt.show()    

def cluster_profiles(profiles, threshold=0.125, samples=1):
    # Remove None profiles
    filtered_data = [t for t in profiles if t is not None]

    # DBSCAN
    clustering = DBSCAN(eps=threshold, min_samples=samples, metric='euclidean').fit(filtered_data)

    # Take representative points from each cluster (one point per cluster)
    unique_labels = np.unique(clustering.labels_)
    distinct_profiles = []
    for label in unique_labels:
        indices = np.where(clustering.labels_ == label)[0]
        distinct_profiles.append(filtered_data[indices[0]])

    return distinct_profiles

def assign_profiles(profiles, separation, frame_interval, max_frame):
    default_profile = (1.0, 0, 0, 0.75)
    final_profiles = {}

    profile_index = 0
    for frame_index in range(0, max_frame, frame_interval):
        frame_profiles = {}
        frame_key = str(frame_index) + "_" + str(frame_index + frame_interval)
        for row in range(separation[1]):
            for col in range(separation[0]): 
                area_key = str(row) + "_" + str(col)
                if profile_index < len(profiles):
                    if profiles[profile_index] != None:
                        p = profiles[profile_index]
                    else:
                        p = default_profile
                    frame_profiles[area_key] = {"goal": p[0], "group": p[1], "interaction": p[2], "connection": p[3]}
                else:
                    continue

                profile_index += 1 
        if len(frame_profiles) > 0:
            final_profiles[frame_key] = frame_profiles

    return final_profiles, cluster_profiles(profiles)

def generate_json(path, predictions, pred_clusters, separation, frame_interval, framerate, agent_dict, ):
    # Write parameters
    env_dict = {'width' : separation[0], 'height' : separation[1], 'frame_interval' : frame_interval, 'framerate' : framerate}
    json_dict = {'Environment' : env_dict, 'Classes' : predictions, 'Clusters' : pred_clusters,'Agents' : agent_dict}
    
    json_path = current_file_dir + path.replace("Trajectories", "JSONs")
    os.makedirs(json_path, exist_ok=True)
    with open(json_path + "simulation_data.json", "w") as outfile:
        json.dump(json_dict, outfile)

def make_grid_image(images, rows, columns, frame, path):
    # Get image size
    img_height, img_width = images[0].shape[:2]
    
    # Adjusting the grid size to accommodate the white lines
    grid_height = img_height * rows + rows - 1
    grid_width = img_width * columns + columns - 1

    # Create an empty grid
    grid = np.zeros((grid_height, grid_width, 5), dtype=images[0].dtype)
    
    for idx, img in enumerate(images[:rows*columns]):
        y = idx // columns
        x = idx % columns
        name = f"_{y}_{x}"
        
        # Calculate position taking the white lines into account
        y_pos = y * (img_height + 1)
        x_pos = x * (img_width + 1)
        
        grid[y_pos:y_pos+img_height, x_pos:x_pos+img_width] = img
        np.savez_compressed(path + name, img)

    # Fill with white lines. Assuming images are normalized between 0 and 1.
    for i in range(1, rows):
        y_pos = i * img_height + i - 1
        grid[y_pos, :] = 1.0

    for j in range(1, columns):
        x_pos = j * img_width + j - 1
        grid[:, x_pos] = 1.0

    return grid

def save_image_grid(path, images, rows, columns, frame):
    titles = ["Velocity X", "Velocity Z", "DFG", "GROUP", "CONNECT"]

    # For each batch of images, make grid and save
    batch_size = rows * columns
    num_batches = len(images) // batch_size

    for i in range(num_batches):
        batch_images = images[i*batch_size:(i+1)*batch_size]
        grid = make_grid_image(batch_images, rows, columns, frame, path)
        
        # Create a 2x2 grid for the channels
        fig, axs = plt.subplots(3, 2, figsize=(12,12))
        
        for c in range(5):
            ax = axs[c//2, c%2]
            ax.imshow(grid[:,:,c], cmap='gray', vmin=0, vmax=1)
            ax.set_title(titles[c])
            ax.axis('off')

        # Save the 2x2 grid image
        plt.savefig(path + ".jpg", bbox_inches='tight', dpi=500)
        plt.close()

def save_reference_images(path, images, separation, frame_interval):
    num_images_per_grid = separation[1] * separation[0] 
    num_grids = len(images) // num_images_per_grid

    path = current_file_dir + path.replace("Trajectories", "Images")
    os.makedirs(path, exist_ok=True)

    for i in range(num_grids):
        batch = images[i*num_images_per_grid:(i+1)*num_images_per_grid]
        frame = str(i * frame_interval)
        frame_path = path + frame
        save_image_grid(frame_path, batch, separation[1], separation[0], frame)

if __name__ ==  '__main__':
    name = "Zara"
    path = r"\\PATH-TO-DATASET\\" + name + "\\"
    frame_interval = 250 # Number of frames per image
    row_step = 1
    video_framerate = 25 # Framerate of the video
    video_width = 60 # Video width to define aspect ratio
    video_height = 48 # Video height to define aspect ratio
    grid_multiplier = 4
    separation = get_grid_separation(video_width, video_height, grid_multiplier)
    thicken_radius = 2
    n_processes = 16 # Number of processes to use for parallel processing. Adjust based on the machine.

    start = time.time()
    print("Running using: " + path)
    # Generate images from trajectories
    agent_dict, model_images, model_masking, max_frame = generate(path, name, frame_interval, row_step, separation, video_framerate, n_processes)
    predicted_profiles = predict(model_images, model_masking)
    final_profiles, profile_clusters = assign_profiles(predicted_profiles, separation, frame_interval, max_frame)

    generate_json(path, final_profiles, profile_clusters, separation, frame_interval, video_framerate, agent_dict)
    save_reference_images(path, model_images, separation, frame_interval)
    print((time.time() - start) / 60.0)