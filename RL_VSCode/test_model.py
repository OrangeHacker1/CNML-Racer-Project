#
#   LEGACY CODE
#

import torch
from unity_client import UnityClient
from ppo_model import PPOModel
from config import *
from utils import load_model

client = UnityClient()
model = PPOModel(STATE_DIM, ACTION_DIM)

load_model(model, "checkpoints/baseline_rl_100.pth")

while True:
    packet = client.receive_state()
    if packet is None:
        break

    state = torch.tensor(packet["state"], dtype=torch.float32)

    with torch.no_grad():
        logits, _ = model(state)

    steer = torch.tanh(logits[0]).item()
    throttle = torch.sigmoid(logits[1]).item()
    brake = torch.sigmoid(logits[2]).item()

    client.send_action(steer, throttle, brake)