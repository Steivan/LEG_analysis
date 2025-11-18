# e3dc_download.py – MINIMAL AUTH TEST
from e3dc import E3DC
import argparse
import sys

parser = argparse.ArgumentParser()
parser.add_argument("--serial", required=True)
parser.add_argument("--user", required=True)
parser.add_argument("--password", required=True)
args = parser.parse_args()

print(f"Testing connection for {args.serial} with user '{args.user}'...")

try:
    e3dc = E3DC(
        E3DC.CONNECT_WEB,
        username=args.user,
        password=args.password,
        serialNumber=args.serial,
        isPasswordMd5=False,
        configuration={}
    )
    
    # This line triggers the full auth + first request – if it works, you're golden
    status = e3dc.poll(keepAlive=True)
    print("+ CONNECTION SUCCESS!")
    print("Live status snippet:", list(status.keys())[:5])  # Shows first 5 keys (e.g., 'batteryPower', 'pvPower')
    e3dc.disconnect()
    sys.exit(0)
    
except Exception as e:
    print("X CONNECTION FAILED")
    print("Exact error:", str(e))
    sys.exit(1)