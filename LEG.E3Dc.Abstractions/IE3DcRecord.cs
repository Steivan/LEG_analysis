namespace LEG.E3Dc.Abstractions
{
    public interface IE3DcRecord
    {
        string Timestamp { get; set; }
        int BatterySoc { get; set; }
        int BatteryCharging { get; set; }
        int BatteryDischarging { get; set; }
        int NetIn { get; set; }
        int NetOut { get; set; }
        int SolarProductionTracker1 { get; set; }
        int SolarProductionTracker2 { get; set; }
        int SolarProductionTracker3 { get; set; }
        int SolarProduction { get; set; }
        int HouseConsumption { get; set; }
        int WallBoxId1TotalChargingPower { get; set; }
        int WallBoxId1GridReference { get; set; }
        int WallBoxId1SolarChargingPower { get; set; }
        int WallBoxId0TotalChargingPower { get; set; }
        int WallBoxId0GridReference { get; set; }
        int WallBoxId0SolarChargingPower { get; set; }
        int WallBoxTotalChargingPower { get; set; }
        int SigmaConsumption { get; set; }
    }
}