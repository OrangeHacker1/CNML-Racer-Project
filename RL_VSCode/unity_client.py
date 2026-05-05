import socket
import json
import time
import torch
from config import HOST, PORT, CHECKPOINT_DIR

class UnityClient:
    def __init__(self, retries=20, delay=1):

        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

        connected = False

        for attempt in range(retries):
            try:
                print(f"Connecting to Unity... ({attempt+1}/{retries})")
                self.sock.connect((HOST, PORT))
                connected = True
                break
            except ConnectionRefusedError:
                time.sleep(delay)

        
        #self.sock.connect((HOST, PORT))

        if not connected:
            raise Exception(
                "Could not connect to Unity. "
                "Make sure Unity is running in Play mode."
            )

        self.file = self.sock.makefile("rw")

        # Connection Established.
        print("Connected to Unity.")

    def receive_state(self):
        line = self.file.readline()
        if not line:
            return None
        return json.loads(line)

    def send_action(self, steer, throttle, brake):
        packet = {
            "steer": float(steer),
            "throttle": float(throttle),
            "brake": float(brake)
        }
        self.file.write(json.dumps(packet) + "\n")
        self.file.flush()


    def close(self):
        self.sock.close()
