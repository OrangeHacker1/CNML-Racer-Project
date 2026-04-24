import random
import torch

class ReplayBuffer:
    def __init__(self, capacity=50000):
        self.capacity = capacity
        self.data = []

    def push(self, state, reward):
        if len(self.data) >= self.capacity:
            self.data.pop(0)

        self.data.append((state, reward))

    def sample(self, batch_size):
        batch = random.sample(self.data, batch_size)
        states, rewards = zip(*batch)

        return (
            torch.stack(states),
            torch.tensor(rewards, dtype=torch.float32)
        )

    def __len__(self):
        return len(self.data)