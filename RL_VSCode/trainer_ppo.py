#
#   OLD LEGACY
#

import torch
import torch.optim as optim
import torch.nn.functional as F

from unity_client import UnityClient
from ppo_model import PPOModel
from ppo_buffer import PPOBuffer
from config import *

client = UnityClient()
model = PPOModel(STATE_DIM, ACTION_DIM)
optimizer = optim.Adam(model.parameters(), lr=PPO_LR)

buffer = PPOBuffer()

def compute_advantages(rewards, values, gamma=0.99):
    advantages = []
    G = 0
    for r in reversed(rewards):
        G = r + gamma * G
        advantages.insert(0, G)
    return torch.tensor(advantages) - torch.tensor(values)

episode = 0

while True:
    packet = client.receive_state()
    if packet is None:
        break

    state = torch.tensor(packet["state"], dtype=torch.float32)
    done = packet["done"]

    logits, value = model(state)

    # Action distribution
    dist = torch.distributions.Normal(logits, 1.0)
    action = dist.sample()
    log_prob = dist.log_prob(action).sum()

    steer = torch.tanh(action[0]).item()
    throttle = torch.sigmoid(action[1]).item()
    brake = torch.sigmoid(action[2]).item()

    client.send_action(steer, throttle, brake)

    reward = packet["reward"]

    buffer.store(state, action, reward, log_prob, value.item(), done)

    if done:
        episode += 1

        states = torch.stack(buffer.states)
        actions = torch.stack(buffer.actions)
        old_log_probs = torch.stack(buffer.log_probs)
        values = torch.tensor(buffer.values)

        advantages = compute_advantages(buffer.rewards, values)

        # PPO UPDATE
        for _ in range(PPO_EPOCHS):
            logits, new_values = model(states)
            dist = torch.distributions.Normal(logits, 1.0)
            new_log_probs = dist.log_prob(actions).sum(dim=1)

            ratio = torch.exp(new_log_probs - old_log_probs)

            surr1 = ratio * advantages
            surr2 = torch.clamp(ratio, 1 - PPO_CLIP, 1 + PPO_CLIP) * advantages

            policy_loss = -torch.min(surr1, surr2).mean()
            value_loss = F.mse_loss(new_values.squeeze(), advantages)

            loss = policy_loss + VALUE_COEF * value_loss

            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

        print(f"[PPO] Episode {episode} Reward: {sum(buffer.rewards):.2f}")

        buffer.clear()