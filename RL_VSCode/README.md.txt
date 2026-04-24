UNITY ↔ PYTHON RL TRAINING BRIDGE
================================

STEP 1 — UNITY SETUP
--------------------
1. Open Unity scene
2. Add RLServerBridge.cs to empty GameObject
3. Port = 5555
4. Press Play

STEP 2 — PYTHON SETUP
---------------------
Install packages:

pip install torch
python -m pip install torch

STEP 3 — TRAIN
--------------
Run:

python train.py

Python connects to Unity automatically.

STEP 4 — EVALUATE
-----------------
Run:

python evaluate.py

Loads saved model.

STEP 5 — CHECKPOINTS
--------------------
Models saved to:

checkpoints/

STEP 6 — NOTES
--------------
Unity must start first.
Then Python connects.

If disconnected:
restart Python.



# Why This Is Powerful

Now you can plug in:

PPO
DQN
SAC
A2C
EWC
Replay buffers
Multi-task learning

without changing Unity much.

