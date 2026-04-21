# python -m pip install -r requirements.txt
import torch
import torch.nn as nn

class PolicyNet(nn.Module):
    def __init__(self, state_dim=5, action_dim=3):
        super().__init__()

        self.net = nn.Sequential(
            nn.Linear(state_dim, 128),
            nn.ReLU(),

            nn.Linear(128, 128),
            nn.ReLU(),

            nn.Linear(128, action_dim),
            nn.Tanh()
        )

    def forward(self, x):
        return self.net(x)