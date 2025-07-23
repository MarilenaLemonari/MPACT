### 🧠 MPACT_Model

The `MPACT_Model` directory contains:
- All the relevant scripts for preparing training data
- All the relevant scripts for training the model
- The trained model

### 🖥️ MPACT_Unity

The `MPACT_Unity` directory contains the MPACT UI.

## Functionalities

### Using a Dataset’s Environment, Spawn/Goal Positions, and Timings

1. Open the main scene named **"Framework"**.
2. Select the **“Environment”** GameObject and set the desired dataset under **“Dataset Name”**.
3. Play the scene and click **“Start”**.

### Using a Custom Environment and Agents Spawning

1. Play the scene.
2. Unselect **“Original Spawn/Goal Positions”** and **“Update Grid’s Behaviors”** from the top-right corner.
3. Click **“Start”**.
4. Refer to the **Functionalities** section below.

### Functionalities

#### Custom Environment
- Under **Modes**, select **“Build”**.
- Use left-click to add a new cell to the environment.
- Use right-click to delete an existing cell.

#### Custom Scene Objects
Spawn **Obstacles**, **POIs**, and **Agents** in the scene.

- **Obstacle/POIs**:
  - Select the object under **Scene Objects**.
  - Click on the environment where you want to spawn it.
  - Hold left-click and drag to adjust the size.
  - Delete using right-click.

- **Agents**:
  - Select **Agents** under **Scene Objects**.
  - Set the number of agents to spawn in a group (between 1 and 5).
  - Click on the environment to place the agent(s).
  - Click again to set the target.

#### Custom Profiles
- Under **Modes**, select **“Behavior”**.

##### Manual
- Use the triangle in the wheel and the slider below to set a custom profile.
- Click **“Demo”** to preview the profile.
- Select one or more cells in the environment to assign the profile for the current time window.
- Click **“Set”** to apply it.
- To set future profiles, move the **Timeline slider** at the bottom of the screen.

##### Prefab
- View and set behavior profiles predicted by the MPACT Model from the current dataset.
- Select one of the white circles to choose a profile.
- Follow the same steps as manual mode to set it in the environment.




