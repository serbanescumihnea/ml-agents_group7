# AIML Project: Training and Benchmarking RL Agents in Unity Environments

This repository contains the experiments and results from the AIML project, where we trained agents in Unity environments using advanced reinforcement learning techniques. The focus is on reproducibility, parameter tuning, and comparative analysis across algorithms such as PPO, SAC, and MA-POCA.

This README provides instructions on reproducing our experiments, analyzing results, and understanding the methodologies used.

---

## Prerequisites

To reproduce the experiments, ensure you have the following installed:

- **Unity**: Version 2023.x or later with ML-Agents Toolkit (Release 21).
- **Python**: Version 3.10 or later with required dependencies installed.
- **Hardware**: A machine with at least an Intel Core i7, 16GB RAM, and an NVIDIA RTX 4070 (or equivalent).

Install Python dependencies using:
```bash
pip install -r requirements.txt
```

---

## Experimentation Setup

### Environment Configuration
The Unity environment used is configured with modular sensors and customizable reward mechanisms. The environment files are located in `Unity/Environment/`. Key configurations include:

- **Sound Detection**: Enabled using sphere colliders.
- **Memory Sensor**: Maintains a rolling history of observations.
- **Vision Decoupling**: Allows agents to rotate their heads independently.

### Training Algorithms
Our experiments tested the following algorithms:
- **Proximal Policy Optimization (PPO)**: Stable and reliable for single- and multi-agent scenarios.
- **Soft Actor-Critic (SAC)**: Balances reward maximization with entropy to encourage exploration.
- **MA-POCA**: Optimized for cooperative multi-agent settings.

### Running Training Scripts
Scripts for training and evaluation are located in the `scripts/` directory. Example commands:

#### PPO
```bash
python train.py --algorithm ppo --config configs/ppo_config.yaml
```

#### SAC
```bash
python train.py --algorithm sac --config configs/sac_config.yaml
```

#### MA-POCA
```bash
python train.py --algorithm ma_poca --config configs/ma_poca_config.yaml
```

---

## Experiment Reproducibility

To replicate our results:

1. **Clone this repository**:
   ```bash
   git clone <repository_url>
   cd <repository_name>
   ```

2. **Install dependencies**:
   ```bash
   pip install -r requirements.txt
   ```

3. **Run Unity Environment**:
   Open Unity, load the `Unity/Environment/` project, and start the environment.

4. **Run Training**:
   Execute one of the training scripts as shown in the examples above. Modify YAML configuration files in `configs/` for custom parameters.

5. **Analyze Results**:
   Metrics and logs are stored in the `results/` directory. Visualization scripts are provided in `scripts/plot_results.py`.

---

## Results Overview

### Sensor Configurations
Our experiments evaluated the following sensor setups:
1. **Sound Only**
2. **Memory Only**
3. **Vision Decoupling**
4. **All Sensors Combined**

| Configuration       | Average Reward | Training Time (hrs) |
|---------------------|----------------|---------------------|
| Sound Only          | 0.85           | 3.2                 |
| Memory Only         | 0.92           | 3.5                 |
| Vision Decoupling   | 1.05           | 4.0                 |
| All Sensors Combined| 1.15           | 4.5                 |

### Algorithm Comparisons
PPO demonstrated stable convergence but struggled in complex multi-agent scenarios. SAC agents exhibited robustness with longer training times. MA-POCA excelled in multi-agent coordination, achieving the highest team-based metrics.

---

## Additional Resources

- [Unity ML-Agents Documentation](https://github.com/Unity-Technologies/ml-agents/tree/release_21_docs/docs/)
- [Research Paper on MA-POCA](http://aaai-rlg.mlanctot.info/papers/AAAI22-RLG_paper_32.pdf)

For a detailed discussion of our methodology and results, refer to the full report in `docs/Phase3_Report.pdf`.

---

## Community and Feedback

If you have questions or run into issues reproducing the experiments, please open a GitHub issue or contact the project maintainers.
