## 🔧 Setup & Training

### 1. Environment Setup
- Create a Python virtual environment using version **3.7.9**:
  ```bash
  python3.7 -m venv venv
  source venv/bin/activate  # On Windows: venv\Scripts\activate
  ```

- Install required packages:
  ```bash
  pip install -r requirements.txt
  ```

### 2. Unity Project
- Open the Unity project in the `modCCP` folder.
- Build the project including the scene:
  ```
  Scenes/Training
  ```

### 3. Training
- Use the following command to start training:
  ```bash
  mlagents-learn config/config.yaml --num-envs=N --env=./modCCP/Builds/modCCP.exe --run-id=modCCP_0
  ```
  Replace `N` with the number of environments you wish to run in parallel (depending on your available system memory).

### 4. Monitoring Training
- Launch TensorBoard to monitor training progress:
  ```bash
  tensorboard --logdir results --port 6006
  ```
- Open a browser and navigate to:
  ```
  http://localhost:6006/
  ```


## 🧠 Inference

#### 1. `Scenes/Inference`
- Use this to run inference.




