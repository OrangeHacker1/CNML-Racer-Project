import torch
import torch.optim as optim
import torch.nn.functional as F
from torch.distributions import Categorical

from models import PolicyNet
from config import *

class PPOTrainer:
    def __init__(self):
        self.model = PolicyNet(STATE_SIZE, ACTION_SIZE)
        self.optimizer = optim.Adam(self.model.parameters(), lr=LR)

    def select_action(self, state):
        state = torch.tensor(state).float().unsqueeze(0)

        logits = self.model(state)
        probs = F.softmax(logits, dim=-1)

        dist = Categorical(probs)
        action = dist.sample()

        return action.item()

    def save(self, path="ppo_model.pt"):
        torch.save(self.model.state_dict(), path)