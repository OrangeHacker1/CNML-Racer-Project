from unity_env import UnityEnv
from trainer_ppo import PPOTrainer

env = UnityEnv()
trainer = PPOTrainer()

while True:
    state = env.get_state()
    action = trainer.select_action(state)
    env.send_action(action)