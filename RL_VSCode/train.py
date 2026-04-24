import os
import torch
import torch.optim as optim
import torch.nn.functional as F

from config import *
from unity_client import UnityClient
from model import PolicyNet
from replay_buffer import ReplayBuffer
from utils import *

ensure_dirs(CHECKPOINT_DIR, LOG_DIR)

client = UnityClient()
model = PolicyNet(STATE_DIM, ACTION_DIM)
optimizer = optim.Adam(model.parameters(), lr=LR)
buffer = ReplayBuffer()

episode = 0

while True:
    packet = client.receive_state()
    
    # Exit condition.
    if packet is None:
        break

    state = torch.tensor(packet["state"], dtype=torch.float32)
    reward = float(packet["reward"])
    done = packet["done"]

    with torch.no_grad():
        logits = model(state)

    steer = torch.tanh(logits[0]).item()
    throttle = torch.sigmoid(logits[1]).item()
    brake = torch.sigmoid(logits[2]).item()

    client.send_action(steer, throttle, brake)

    buffer.push(state, reward)

    # ---------------- TRAIN ----------------
    if len(buffer) >= BATCH_SIZE:
        states, rewards = buffer.sample(BATCH_SIZE)

        pred = model(states)

        # maximize reward proxy
        loss = -(pred.mean(dim=1) * rewards).mean()

        optimizer.zero_grad()
        loss.backward()
        optimizer.step()

    # ---------------- LOG ----------------
    if done:
        episode += 1
        print(f"Episode {episode} | Reward {reward:.2f}")

        if episode % SAVE_INTERVAL == 0:
            path = os.path.join(
                CHECKPOINT_DIR,
                f"model_{episode}.pth"
            )
            save_checkpoint(model, path)
            print("Saved:", path)