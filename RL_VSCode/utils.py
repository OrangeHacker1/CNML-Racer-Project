import os
import torch
from config import *

def ensure_dirs(*dirs):
    for d in dirs:
        os.makedirs(d, exist_ok=True)

def save_checkpoint(model, path):
    torch.save(model.state_dict(), path)

def load_checkpoint(model, path):
    model.load_state_dict(torch.load(path))

def save_model(model, name, episode):
    path = f"{CHECKPOINT_DIR}/{name}_{episode}.pth"
    torch.save(model.state_dict(), path)

def load_model(model, path):
    model.load_state_dict(torch.load(path))
    print("Loaded:", path)