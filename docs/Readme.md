# AIML Project: Training and Benchmarking RL Agents in Unity Environments

This repository contains the experiments and results from the AIML project, where we trained agents to play soccer in Unity's SoccerTwos environment using advanced reinforcement learning techniques. The focus is on reproducibility, performance and comparative analysis across algorithms such as PPO, SAC, and MA-POCA.

This README provides instructions on reproducing our experiments, analyzing results, and understanding the methodologies used.

---

## Prerequisites

To reproduce the experiments, ensure you have the following installed:

- **Unity**: Version 2023.x or later with ML-Agents Toolkit (Release 21).
- **Python**: Version 3.10 or later with required dependencies installed.


Install Python dependencies using:
```bash
python ml-agents-envs/setup.py
python ml-agents/setup.py
```

---

## Experimentation Setup

### Environment Configuration
The Unity environment used is configured with modular sensors and customizable reward mechanisms. 
The training environment is located at ```training/SoccerTwos/training_env/UnityEnvironment.exe```

### Configuration files
The configuration files (**see example at ```config/poca/SoccerTwos.yaml```**) is based on the standard documented POCA config, with the only addition being the *environment parameters* which refer to enabled sensors.

In order to enable/disable a sensor, simply change it's value to 1/0.
```yaml
environment_parameters:
  vision: 0
  memory: 0
  sound: 1
```
**Vision** refers to the independent rotation of the head (and the raycast sensor) in regard to the body of the agent.
**Sound** refers to the perception of sound around a given agent (sensing other agents by intensity).
**Memory** refers to the ability to store the last 3 raycast observations.

*In the above example, the vision is coupled to the agent's body rotation, the memory is disabled and sound perception is enabled.*

### Training Algorithms
Our experiments tested the following algorithms:
- **Proximal Policy Optimization (PPO)**: Stable and reliable for single- and multi-agent scenarios.
- **Soft Actor-Critic (SAC)**: Balances reward maximization with entropy to encourage exploration.
- **MA-POCA**: Optimized for cooperative multi-agent settings.

### Running Training Scripts
Scripts for training and evaluation are located in the `scripts/` directory. Example commands:


#### MA-POCA

For training with Unity use:

```bash
mlagents-learn config/poca/SoccerTwos.yaml --run-id=POCA_EXAMPLE 
```

For training with the exectuable file:

```bash
mlagents-learn config/poca/SoccerTwos.yaml --env=training/SoccerTwos/training_env/UnityEnvironment
```

---

## Experiment Reproducibility

To replicate our results:

1. **Clone this repository**:

2. **Install dependencies**:
   ```bash
    python ml-agents-envs/setup.py
    python ml-agents/setup.py
   ```

3. **Use the executable environment (RECOMMENDED)**:
   Execute the training command for the executable training file above. Modify YAML configuration files in `configs/` for custom parameters.

3. **Train using Unity**
  Import the project into Unity from Unity Hub and open it:
    ``Add -> Add Project From Disk -> Select the *Project* folder from the cloned repository -> Open``
  Open the SoccerTwos scene:
  ``Navigate to MLAgents -> Examples -> Soccer -> Scenes -> SoccerTwos`` and double click on the scene.
4. **Run Training**:
   Execute the training command for Unity use seen above and then press the play in the top mid section. Modify YAML configuration files in `configs/` for custom parameters.

5. **Analyze Results**:
   Metrics and logs are stored in the `results/` directory. For visualizing the results we recommend using **Tensorboard**.
\
   Tensorboard usage:
   ```bash
   tensorboard --logdir results/
   ```

---

## SoccerTool
In order to make the experiment process while training and making 1vs1 simulations easier, we have created the *SoccerTool*. 

### How to use
1. With the project open navigate to: Tools -> Soccer Configuration
2. Select the soccer field prefab from Examples -> Soccer -> CustomPrefabs
3. Select the mode (training/playing)
  - **Training Mode**:
    1. Input number of rows and columns of fields to spawn in a table-like manner.
    2. Change or leave as default the spacing between the fields.
    3. Select whether the bots will be trained using: Vision Decoupling, Memory or Sound observations.
    4. Generate fields.

  - **Playing mode (1vs1):**
    1. Select the model the blue team will use.
    2. Select what sensors/functionality the blue team will use (the selected model is trained on).
    3. Repeat for the purple team.
    4. Generate 1v1 Environments

---

## Additional Resources

- [Unity ML-Agents Documentation](https://github.com/Unity-Technologies/ml-agents/tree/release_21_docs/docs/)
- [Research Paper on MA-POCA](http://aaai-rlg.mlanctot.info/papers/AAAI22-RLG_paper_32.pdf)

For a detailed discussion of our methodology and results, refer to the full report in `docs/Phase3_Report.pdf`.

---

## Community and Feedback

If you have questions or run into issues reproducing the experiments, please open a GitHub issue or contact the project maintainers.
