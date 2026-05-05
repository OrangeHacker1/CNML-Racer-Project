import torch
import torch.nn as nn

class PPOModel(nn.Module):
    def __init__(self, state_dim, action_dim):
        super().__init__()

        self.shared = nn.Sequential(
            nn.Linear(state_dim, 128),
            nn.ReLU(),
            nn.Linear(128, 128),
            nn.ReLU()
        )

        self.policy_mean = nn.Linear(128, action_dim)
        self.log_std = nn.Parameter(torch.zeros(action_dim))

        self.value = nn.Linear(128, 1)

    def forward(self, x):
        x = self.shared(x)
        mean = self.policy_mean(x)
        std = torch.exp(self.log_std)
        value = self.value(x)
        return mean, std, value

    def act(self, state):
        mean, std, value = self.forward(state)

        dist = torch.distributions.Normal(mean, std)
        action = dist.sample()
        log_prob = dist.log_prob(action).sum()

        return action, log_prob, value