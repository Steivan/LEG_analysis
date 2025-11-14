using LEG.E3Dc.Abstractions;
using System;

namespace LEG.E3Dc.Client
{
    public class E3DcAggregationRecord : IE3DcAggregationRecord
    {
        public int CountOfRecords { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public double BatterySocStart { get; set; }
        public double BatterySocEnd { get; set; }
        public double BatterySocIntegral { get; set; }
        public double BatteryCharging { get; set; }
        public double BatteryDischarging { get; set; }
        public double NetIn { get; set; }
        public double NetOut { get; set; }
        public double SolarProduction { get; set; }
        public double HouseConsumption { get; set; }
        public double WallBoxTotalChargingPower { get; set; }
    }
}