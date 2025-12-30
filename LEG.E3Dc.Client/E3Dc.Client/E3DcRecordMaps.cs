using CsvHelper.Configuration;
using LEG.Common;

namespace LEG.PvImport.Clients.E3Dc.Client
{
    // Old portal mapping
    public sealed class E3DcRecordOldMap : ClassMap<E3DcRecord>
    {
        public E3DcRecordOldMap()
        {
            Map(m => m.Timestamp).Name("timestamp");
            Map(m => m.BatterySoc).Name("Battery SOC").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.BatteryCharging).Name("Battery (charging)").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.BatteryDischarging).Name("Battery (discharging)").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.NetIn).Name("NetIn").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.NetOut).Name("NetOut").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.SolarProductionTracker1).Name("Solar production tracker 1").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.SolarProductionTracker2).Name("Solar production tracker 2").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.SolarProductionTracker3).Name("Solar production tracker 3").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.SolarProduction).Name("Solar production").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.HouseConsumption).Name("House consumption").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.WallBoxId1TotalChargingPower).Name("Wallbox (ID 1) total charging power").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.WallBoxId1GridReference).Name("Wallbox (ID 1) Grid reference").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.WallBoxId1SolarChargingPower).Name("Wallbox (ID 1) solar charging power").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.WallBoxId0TotalChargingPower).Name("Wallbox (ID 0) total charging power").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.WallBoxId0GridReference).Name("Wallbox (ID 0) Grid reference").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.WallBoxId0SolarChargingPower).Name("Wallbox (ID 0) solar charging power").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.WallBoxTotalChargingPower).Name("Wallbox total charging power").TypeConverter<Int32DefaultZeroConverter>();
            Map(m => m.SigmaConsumption).Name("Î£ Consumption").TypeConverter<Int32DefaultZeroConverter>();
        }
    }

    // New portal mapping (unchanged)
    public sealed class E3DcRecordNewMap : ClassMap<E3DcRecord>
    {
        public E3DcRecordNewMap()
        {
            Map(m => m.Timestamp).Name("Timestamp");
            Map(m => m.BatterySoc).Name("\"State of charge [%]\"");
            Map(m => m.SolarProduction).Name("Solar production [W]");
            Map(m => m.BatteryCharging).Name("Battery charge [W]");
            Map(m => m.BatteryDischarging).Name("Battery discharge [W]");
            Map(m => m.NetOut).Name("Grid export [W]");
            Map(m => m.NetIn).Name("Grid import [W]");
            Map(m => m.HouseConsumption).Name("House consumption [W]");
            Map(m => m.WallBoxTotalChargingPower).Name("Sum Wallbox charge [W]");
            Map(m => m.SigmaConsumption).Name("Sum Consumption [W]");
            // Add Deration limit if you want to store it
        }
    }
}