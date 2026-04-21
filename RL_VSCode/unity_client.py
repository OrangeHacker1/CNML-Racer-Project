import socket
import json

class UnityClient:
    def __init__(self, host="127.0.0.1", port=5555):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.connect((host, port))

        self.file = self.sock.makefile("rw")

    def receive_state(self):
        line = self.file.readline()
        if not line:
            return None
        return json.loads(line)

    def send_action(self, steer, throttle, brake):
        msg = {
            "steer": float(steer),
            "throttle": float(throttle),
            "brake": float(brake)
        }
        self.file.write(json.dumps(msg) + "\n")
        self.file.flush()