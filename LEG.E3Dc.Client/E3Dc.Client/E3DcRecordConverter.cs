namespace LEG.PvImport.Clients.E3Dc.Client
{
    public partial class E3DcRecord
    {
        public void ConvertPowerFieldsToWh()
        {
            // Multiply only fields that are in W in the new format
            SolarProduction = (int)(SolarProduction * 0.25);
            HouseConsumption = (int)(HouseConsumption * 0.25);
            SolarProductionTracker1 = (int)(SolarProductionTracker1 * 0.25);
            SolarProductionTracker2 = (int)(SolarProductionTracker2 * 0.25);
            SolarProductionTracker3 = (int)(SolarProductionTracker3 * 0.25);
            BatteryCharging = (int)(BatteryCharging * 0.25);
            BatteryDischarging = (int)(BatteryDischarging * 0.25);
            NetIn = (int)(NetIn * 0.25);
            NetOut = (int)(NetOut * 0.25);
            WallBoxTotalChargingPower = (int)(WallBoxTotalChargingPower * 0.25);
            SigmaConsumption = (int)(SigmaConsumption * 0.25);
            // Add more fields if needed
        }
    }
}
