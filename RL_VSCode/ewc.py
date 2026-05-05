import torch

class EWC:
    def __init__(self, model, data, lambda_=10.0):
        self.model = model
        self.lambda_ = lambda_

        self.params = {
            n: p for n, p in model.named_parameters()
            if p.requires_grad
        }

        self.means = {}
        self.fisher = {}

        self._compute_fisher(data)

    # --------------------------------------------------
    # CORRECT RL FISHER
    # --------------------------------------------------
    def _compute_fisher(self, data):
        states, actions, old_log_probs, returns, advantages = data

        # Initialize fisher
        for n, p in self.params.items():
            self.fisher[n] = torch.zeros_like(p)

        self.model.eval()

        for i in range(len(states)):
            state = states[i]
            action = actions[i]

            self.model.zero_grad()

            mean, std, _ = self.model(state)

            dist = torch.distributions.Normal(mean, std)
            log_prob = dist.log_prob(action).sum()

            # Fisher = grad(log pi)^2
            loss = -log_prob
            loss.backward()

            for n, p in self.params.items():
                if p.grad is not None:
                    self.fisher[n] += p.grad.data.clone().pow(2)

        # Normalize
        for n in self.fisher:
            self.fisher[n] /= len(states)

        # Store means
        for n, p in self.params.items():
            self.means[n] = p.data.clone()

    # --------------------------------------------------
    # PENALTY
    # --------------------------------------------------
    def penalty(self, model):
        loss = 0

        for n, p in model.named_parameters():
            if n in self.fisher:
                loss += (
                    self.fisher[n]
                    * (p - self.means[n]).pow(2)
                ).sum()

        return self.lambda_ * loss