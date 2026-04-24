import torch
from unity_client import UnityClient
from model import PolicyNet
from utils import load_checkpoint

client = UnityClient()

model = PolicyNet()
load_checkpoint(model, "checkpoints/model_100.pth")
model.eval()

while True:
    packet = client.receive_state()
    if packet is None:
        break

    state = torch.tensor(packet["state"], dtype=torch.float32)

    with torch.no_grad():
        action = model(state)

    steer = torch.tanh(action[0]).item()
    throttle = torch.sigmoid(action[1]).item()
    brake = torch.sigmoid(action[2]).item()

    client.send_action(steer, throttle, brake)