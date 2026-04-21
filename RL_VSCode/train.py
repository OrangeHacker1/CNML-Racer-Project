import torch
import torch.optim as optim
from unity_client import UnityClient
from model import PolicyNet

client = UnityClient()
model = PolicyNet()
optimizer = optim.Adam(model.parameters(), lr=1e-4)

while True:
    packet = client.receive_state()
    if packet is None:
        break

    state = torch.tensor(packet["state"], dtype=torch.float32)

    action = model(state)
    steer = action[0].item()
    throttle = (action[1].item() + 1) / 2
    brake = max(0.0, action[2].item())

    client.send_action(steer, throttle, brake)

    # Placeholder learning step
    loss = -torch.tensor(packet["reward"], requires_grad=True)
    optimizer.zero_grad()
    loss.backward()
    optimizer.step()