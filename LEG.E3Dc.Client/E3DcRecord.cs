using CsvHelper.Configuration.Attributes;
using LEG.Common;
using LEG.E3Dc.Abstractions;

namespace LEG.E3Dc.Client
{
    // Example CSV file content:
    // timestamp;Battery SOC;Battery (charging);Battery (discharging);NetIn;NetOut;Solar production tracker 1;Solar production tracker 2;Solar production;House consumption
    // 2021-09-13 13:30;33;0;0;0;1294;0;0;0;1294

    public class E3DcRecord : IE3DcRecord
    {
        [Name("timestamp")] public string Timestamp { get; set; } = string.Empty;

        [Name("Battery SOC"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int BatterySoc { get; set; }

        [Name("Battery (charging)"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int BatteryCharging { get; set; }

        [Name("Battery (discharging)"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int BatteryDischarging { get; set; }

        [Name("NetIn"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int NetIn { get; set; }

        [Name("NetOut"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int NetOut { get; set; }

        [Name("Solar production tracker 1"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int SolarProductionTracker1 { get; set; }

        [Name("Solar production tracker 2"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int SolarProductionTracker2 { get; set; }

        [Name("Solar production tracker 3"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int SolarProductionTracker3 { get; set; } = 0;

        [Name("Solar production"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int SolarProduction { get; set; }

        [Name("House consumption"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int HouseConsumption { get; set; }

        [Name("Wallbox (ID 1) total charging power"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int WallBoxId1TotalChargingPower { get; set; } = 0;

        [Name("Wallbox (ID 1) Grid reference"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int WallBoxId1GridReference { get; set; } = 0;

        [Name("Wallbox (ID 1) solar charging power"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int WallBoxId1SolarChargingPower { get; set; } = 0;

        [Name("Wallbox (ID 0) total charging power"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int WallBoxId0TotalChargingPower { get; set; } = 0;

        [Name("Wallbox (ID 0) Grid reference"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int WallBoxId0GridReference { get; set; } = 0;

        [Name("Wallbox (ID 0) solar charging power"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int WallBoxId0SolarChargingPower { get; set; } = 0;

        [Name("Wallbox total charging power"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int WallBoxTotalChargingPower { get; set; } = 0;

        [Name("Î£ Consumption"), TypeConverter(typeof(Int32DefaultZeroConverter))]
        public int SigmaConsumption { get; set; } = 0;
    }
}