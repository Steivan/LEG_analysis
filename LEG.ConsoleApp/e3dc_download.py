from e3dc import E3DC
from datetime import datetime
import argparse
import os

# === PARSE ARGUMENTS ===
parser = argparse.ArgumentParser(description="Download E3DC data")
parser.add_argument("--serial", required=True, help="Serial number (Anlagen-ID)")
parser.add_argument("--start", required=True, help="Start date (YYYY-MM-DD)")
parser.add_argument("--end", required=True, help="End date (YYYY-MM-DD)")
parser.add_argument("--output", required=True, help="Output CSV path")
parser.add_argument("--username", required=True, help="Portal username")
parser.add_argument("--password", required=True, help="Portal password")

args = parser.parse_args()

# === CONFIG ===
USERNAME = args.username
PASSWORD = args.password
SERIAL = args.serial
START_DATE = datetime.strptime(args.start, "%Y-%m-%d").date()
END_DATE = datetime.strptime(args.end, "%Y-%m-%d").date()
OUTPUT_FILE = args.output
# ===============

os.makedirs(os.path.dirname(OUTPUT_FILE), exist_ok=True)

print(f"Downloading for {SERIAL} from {START_DATE} to {END_DATE} → {OUTPUT_FILE}")

# Connect to station
station = E3DC(E3DC.CONNECT_WEB,
               username=USERNAME,
               password=PASSWORD,
               serialNumber=SERIAL)

# Get data
data = station.get_db_data(startDate=START_DATE, endDate=END_DATE)

# Save CSV
with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
    f.write("Timestamp;PV Production;Battery Charge;Battery Discharge;Grid Feed-in;Grid Purchase;House Consumption;Autarky;Self-Consumption\n")
    for row in data["data"]:
        f.write(f"{row['timestamp']};"
                f"{row.get('solarProduction', 0)};"
                f"{row.get('batPowerIn', 0)};"
                f"{row.get('batPowerOut', 0)};"
                f"{row.get('gridPowerOut', 0)};"
                f"{row.get('gridPowerIn', 0)};"
                f"{row.get('consumption', 0)};"
                f"{row.get('autarky', 0)};"
                f"{row.get('selfConsumption', 0)}\n")

print(f"Saved {len(data['data'])} rows → {OUTPUT_FILE}")