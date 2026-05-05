# ---------------- NETWORK ----------------
# IP address of the Unity environment
# 127.0.0.1 = local machine (Unity + Python on same PC)
HOST = "127.0.0.1"

# Port used for socket communication
# Must match Unity RLServerBridge
PORT = 5555



# ---------------- STATE / ACTION SPACE ----------------

# Size of input to neural network
# 7 rays + 1 speed = 8
STATE_DIM = 8 # (Rays + Speed)

# Number of outputs from policy
# steer, throttle, brake
ACTION_DIM = 3

# ---------------- PPO HYPERPARAMETERS ----------------

# Learning rate for optimizer
# Lower = more stable, slower learning
PPO_LR = 3e-4

# PPO clipping range
# Prevents policy from changing too much per update
PPO_CLIP = 0.2

# Number of passes over each batch
# More epochs = more learning per batch, but risk overfitting

#PPO_EPOCHS	Behavior
#3–5	Stable
#10+	Risky
#1	    Under-training
PPO_EPOCHS = 5

# Weight of value function loss
# Balances policy vs value learning
VALUE_COEF = 0.5

# Encourages exploration
# Higher = more randomness
ENTROPY_COEF = 0.01

# Reward discount factor
# 0.99 = long-term rewards matter
GAMMA = 0.99

# GAE smoothing factor
# Controls bias vs variance in advantage estimates
LAMBDA = 0.95

# Number of steps before PPO update
# Larger batch = more stable gradients
# Training happens when enough data is collected.
# Episode length is around 50 is steps. Therefore it would require 5 episodes before training.
# If episodes are too shor, the training will rearely happen.
BATCH_SIZE = 64

# ---------------- BASELINE RL ----------------

# Name used when saving NON-lifelong model
RL_MODEL_NAME = "baseline_rl"

# ---------------- LIFELONG ----------------


# Enable Elastic Weight Consolidation
# Prevents catastrophic forgetting
USE_EWC = True

# Strength of EWC penalty
# Higher = stronger memory retention, less flexibility
EWC_LAMBDA = 10.0

# Name used when saving lifelong model
LL_MODEL_NAME = "lifelong_rl"

# OLD system (episode-based tasks) — no longer used
# TASK_INTERVAL = 20   # episodes per task

# ---------------- SAVE ----------------

# Save model every N episodes
# Save after N runs on the car. One crash is a run.
SAVE_INTERVAL = 10

# Folder where models are stored
CHECKPOINT_DIR = "checkpoints"

# Folder for logs (optional usage)
LOG_DIR = "logs"

# -------------- TASK TRACKING -------------

# Maximum number of tracks (tasks) to train on
# A track is one enviornment / tash
# Task (for EWC) = A track
# Training stops when this is reached
MAX_TASKS = 100

# Maximum number of episodes total
# Safety stop to prevent infinite training
# An Episode is one attempt to drive.
MAX_EPISODES = 1000
