import torch

class PPOBuffer:
    def __init__(self, gamma=0.99, lam=0.95):
        self.gamma = gamma
        self.lam = lam

        self.clear()
        """
        self.states = []
        self.actions = []
        self.rewards = []
        self.values = []
        self.log_probs = []
        self.dones = []

        self.gamma = gamma
        self.lam = lam
        """

    def clear(self):
        self.states = []
        self.actions = []
        self.rewards = []
        self.values = []
        self.log_probs = []
        self.dones = []

        # trajectory tracking
        self.advantages = []
        self.returns = []

        self.path_start_idx = 0

    """""
    def store(self, s, a, r, v, logp, done):
        self.states.append(s)
        self.actions.append(a)
        self.rewards.append(r)
        self.values.append(v)
        self.log_probs.append(logp)
        self.dones.append(done)
    """

    # --------------------------------------------------
    # STORE STEP
    # --------------------------------------------------
    def store(self, state, action, reward, value, log_prob, done, new_episode):
        # If Unity signals new episode → finalize previous trajectory
        if new_episode and len(self.rewards) > 0:
            self.finish_path(last_value=0)

        self.states.append(state)
        self.actions.append(action)
        self.rewards.append(reward)
        self.values.append(value)
        self.log_probs.append(log_prob)
        self.dones.append(done)

    # --------------------------------------------------
    # FINISH TRAJECTORY (GAE)
    # --------------------------------------------------
    def finish_path(self, last_value=0):
        path_slice = slice(self.path_start_idx, len(self.rewards))

        rewards = self.rewards[path_slice]
        values = self.values[path_slice] + [last_value]

        advantages = []
        gae = 0

        for t in reversed(range(len(rewards))):
            delta = (
                rewards[t]
                + self.gamma * values[t + 1]
                - values[t]
            )

            gae = delta + self.gamma * self.lam * gae
            advantages.insert(0, gae)

        returns = [adv + val for adv, val in zip(advantages, values[:-1])]

        self.advantages.extend(advantages)
        self.returns.extend(returns)

        self.path_start_idx = len(self.rewards)

    # --------------------------------------------------
    # GET TRAINING DATA
    # --------------------------------------------------
    def get(self):
        # Finalize last trajectory
        self.finish_path(last_value=0)

        states = torch.stack(self.states)
        actions = torch.stack(self.actions)
        log_probs = torch.stack(self.log_probs).view(-1)

        returns = torch.tensor(self.returns, dtype=torch.float32)
        advantages = torch.tensor(self.advantages, dtype=torch.float32)

        return states, actions, log_probs, returns, advantages

    """""
    def compute_advantages(self):
        advantages = []
        gae = 0

        values = self.values + [0]

        for t in reversed(range(len(self.rewards))):
            delta = (
                self.rewards[t]
                + self.gamma * values[t+1] * (1 - self.dones[t])
                - values[t]
            )

            gae = delta + self.gamma * self.lam * (1 - self.dones[t]) * gae
            advantages.insert(0, gae)

        return torch.tensor(advantages, dtype=torch.float32)

    def get(self):
        states = torch.stack(self.states)
        actions = torch.stack(self.actions)

        log_probs = torch.stack(self.log_probs)
        log_probs = log_probs.view(-1)  # ensure correct shape

        values = torch.tensor(self.values, dtype=torch.float32)

        advantages = self.compute_advantages()
        returns = advantages + values

        return states, actions, log_probs, returns.detach(), advantages.detach()

    def clear(self):
        self.__init__(self.gamma, self.lam)
    """