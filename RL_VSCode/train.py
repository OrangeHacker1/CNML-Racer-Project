import os
import torch
import torch.optim as optim
import torch.nn.functional as F

from unity_client import UnityClient
from ppo_model import PPOModel
from ppo_buffer import PPOBuffer
from ewc import EWC
from config import *

# ---------------- SETUP ----------------

# Create checkpoint directory if it doesn't exist.
# This prevents crashes when saving models later.
os.makedirs(CHECKPOINT_DIR, exist_ok=True)

# Initialize TCP client to connect to Unity.
# This handles sending actions and receiving states.
client = UnityClient()

# Create PPO model (policy + value network).
# STATE_DIM = size of input (rays + speed)
# ACTION_DIM = 3 (steer, throttle, brake)
model = PPOModel(STATE_DIM, ACTION_DIM)



# Adam optimizer for training the neural network.
# PPO_LR controls how fast the model updates.
optimizer = optim.Adam(model.parameters(), lr=PPO_LR)


# PPO experience buffer.
# Stores trajectories before performing updates.
# GAMMA = reward discount
# LAMBDA = GAE smoothing factor
buffer = PPOBuffer(GAMMA, LAMBDA)

# Elastic Weight Consolidation (lifelong learning).
# Starts as None, initialized later after first task.
ewc = None

# Counts how many episodes have been completed.
# Episodes are attempts with the car. Each crash or completion would be a run.
episode = 0

# Counts how many TRACKS (tasks) we've trained on.
# This is CRITICAL for lifelong learning.
# OLD
# task_counter = 0

# Track Training Stuff
# In config.
#max_episodes = 1000
#max_tracks = 100


# Counts how many TRACKS (tasks) we've trained on.
# This is CRITICAL for lifelong learning.
track_count = 0

# Track Tracking
# Track name tracking (optional, useful for debugging/logging).
current_track = None

# Store last task data
# Stores the last batch of training data.
# Used to compute Fisher information for EWC.
last_task_data = None

# ---------------- TRAIN LOOP ----------------
while True:
    packet = client.receive_state()
    if packet is None:
        break

    if packet["newTrack"]:
        print("New track → clearing buffer")
        buffer.clear()

    state = torch.tensor(packet["state"], dtype=torch.float32)

    # ---- ACT ----
    action, log_prob, value = model.act(state)

    steer = torch.tanh(action[0]).item()
    throttle = torch.sigmoid(action[1]).item()
    brake = torch.sigmoid(action[2]).item()

    client.send_action(steer, throttle, brake)

    buffer.store(
        state,
        action.detach(),
        float(packet["reward"]),
        value.item(),
        log_prob.detach(),
        float(packet["done"]),
        packet["newEpisode"]
    )

    # ---------------- TRAIN ----------------
    if len(buffer.states) >= BATCH_SIZE:
        states, actions, old_log_probs, returns, advantages = buffer.get()

        advantages = (advantages - advantages.mean()) / (advantages.std() + 1e-8)

        for _ in range(PPO_EPOCHS):
            mean, std, values = model(states)

            dist = torch.distributions.Normal(mean, std)
            new_log_probs = dist.log_prob(actions).sum(dim=1)

            ratio = torch.exp(new_log_probs - old_log_probs)

            surr1 = ratio * advantages
            surr2 = torch.clamp(ratio, 1 - PPO_CLIP, 1 + PPO_CLIP) * advantages

            policy_loss = -torch.min(surr1, surr2).mean()
            value_loss = F.mse_loss(values.squeeze(), returns)
            entropy = dist.entropy().mean()

            loss = (
                policy_loss
                + VALUE_COEF * value_loss
                - ENTROPY_COEF * entropy
            )

            if USE_EWC and ewc is not None:
                loss += ewc.penalty(model)

            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

        # Save data BEFORE clearing
        last_task_data = (states, actions, old_log_probs, returns, advantages)

        buffer.clear()

    # ---------------- EPISODE END ----------------
    
    if packet["done"]:
        episode += 1
        print(f"Episode {episode} | Reward {packet['reward']}")

        # ---------------- Track / TASK DETECTION ----------------
        if packet["newTrack"]:
            track_count += 1
            print(f"\n=== NEW TRACK: {packet['trackName']} ({track_count}) ===")

            # Apply EWC at TRUE task boundary
            if USE_EWC and last_task_data is not None:
                print("Applying EWC...")
                ewc = EWC(model, last_task_data, EWC_LAMBDA)

        # ---- SAVE MODEL ----
        if episode % SAVE_INTERVAL == 0:
            name = LL_MODEL_NAME if USE_EWC else RL_MODEL_NAME
            path = os.path.join(
                CHECKPOINT_DIR,
                f"{name}_{episode}.pth"
            )
            torch.save(model.state_dict(), path)
            print("Saved:", path)

        # ---- STOP CONDITIONS ----
        if track_count >= MAX_TASKS:
            print("Reached max tasks. Stopping training.")
            break
    
        if episode >= MAX_EPISODES:
            print("Reached max episodes. Stopping training.")
            break

    """
    if packet["done"]:
        episode += 1
        print(f"Episode {episode} | Reward {packet['reward']}")
    
        # ---- TASK SWITCH DETECTION ----
        if packet["newEpisode"]:
            task_counter += 1
            print(f"--- New Task Detected ({task_counter}) ---")

            if USE_EWC and last_task_data is not None:
                print("Updating EWC...")
                ewc = EWC(model, last_task_data, EWC_LAMBDA)

        # ---- SAVE MODEL ----
        if episode % SAVE_INTERVAL == 0:
            name = LL_MODEL_NAME if USE_EWC else RL_MODEL_NAME
            path = os.path.join(
                CHECKPOINT_DIR,
                f"{name}_{episode}.pth"
            )
            torch.save(model.state_dict(), path)
            print("Saved:", path)"""
    
"""
import os
import torch
import torch.optim as optim
import torch.nn.functional as F

from unity_client import UnityClient
#from model import PolicyNet # OLD
from ppo_model import PPOModel
from ppo_buffer import PPOBuffer
from ewc import EWC
from config import *

# ---------------- SETUP ----------------
os.makedirs(CHECKPOINT_DIR, exist_ok=True)

client = UnityClient()
model = PPOModel(STATE_DIM, ACTION_DIM)
# model = PolicyNet(STATE_DIM, ACTION_DIM)   # OLD
optimizer = optim.Adam(model.parameters(), lr=PPO_LR)

buffer = PPOBuffer(GAMMA, LAMBDA)

ewc = None
episode = 0

# ---------------- TRAIN LOOP ----------------
while True:
    packet = client.receive_state()
    if packet is None:
        break

    state = torch.tensor(packet["state"], dtype=torch.float32)

    # ---- ACT ----
    action, log_prob, value = model.act(state)

    steer = torch.tanh(action[0]).item()
    throttle = torch.sigmoid(action[1]).item()
    brake = torch.sigmoid(action[2]).item()

    client.send_action(steer, throttle, brake)

    buffer.store(
        state,
        action.detach(),
        float(packet["reward"]),
        value.item(),
        log_prob.detach(),
        float(packet["done"]),
        packet["newEpisode"]   # ✅ IMPORTANT
    )

    # ---------------- TRAIN ----------------
    if len(buffer.states) >= BATCH_SIZE:
        states, actions, old_log_probs, returns, advantages = buffer.get()

        # normalize advantages (VERY IMPORTANT)
        advantages = (advantages - advantages.mean()) / (advantages.std() + 1e-8)

        for _ in range(PPO_EPOCHS):
            mean, std, values = model(states)

            dist = torch.distributions.Normal(mean, std)
            new_log_probs = dist.log_prob(actions).sum(dim=1)

            # PPO ratio
            ratio = torch.exp(new_log_probs - old_log_probs)

            surr1 = ratio * advantages
            surr2 = torch.clamp(ratio, 1 - PPO_CLIP, 1 + PPO_CLIP) * advantages

            policy_loss = -torch.min(surr1, surr2).mean()
            value_loss = F.mse_loss(values.squeeze(), returns)
            entropy = dist.entropy().mean()

            loss = (
                policy_loss
                + VALUE_COEF * value_loss
                - ENTROPY_COEF * entropy
            )

            if USE_EWC and ewc is not None:
                loss += ewc.penalty(model)

            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

        # SAVE DATA FOR EWC BEFORE CLEARING
        ewc_data = buffer.get()

        buffer.clear()

    # ---------------- EPISODE ----------------
    if packet["done"]:
        episode += 1
        print(f"Episode {episode} | Reward {packet['reward']}")

        # ---- APPLY EWC (TASK BOUNDARY) ----
        if USE_EWC and episode % TASK_INTERVAL == 0:
            print("Updating EWC...")
            ewc = EWC(model, ewc_data)

        # ---- SAVE MODEL ----
        if episode % SAVE_INTERVAL == 0:
            name = LL_MODEL_NAME if USE_EWC else RL_MODEL_NAME
            path = os.path.join(
                CHECKPOINT_DIR,
                f"{name}_{episode}.pth"
            )
            torch.save(model.state_dict(), path)
            print("Saved:", path)
            """