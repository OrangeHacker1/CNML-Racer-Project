import socket
import numpy as np
from config import HOST, PORT

class UnityEnv:
    def __init__(self):
        self.server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server.bind((HOST, PORT))
        self.server.listen(1)

        print("Waiting for Unity...")
        self.conn, _ = self.server.accept()
        print("Unity Connected.")

    def get_state(self):
        data = self.conn.recv(1024).decode().strip()
        values = [float(x) for x in data.split(",")]
        return np.array(values, dtype=np.float32)

    def send_action(self, action):
        self.conn.sendall(f"{action}\n".encode())
        