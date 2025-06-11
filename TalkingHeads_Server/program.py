import socket
from string import whitespace

while True:
    s = socket.socket()
    s.bind(("0.0.0.0", 1338))
    s.listen(1)
    print("[*] Waiting for connection...")

    conn, addr = s.accept()
    print(f"[+] Connected from {addr}")

    while True:
        cmd = input(">>> ")
        if(cmd == "dc"):
            conn.send()
            break

        if(not cmd.strip()):
            continue

        conn.send(cmd.encode())
        result = conn.recv(4096).decode()
        print(result)
