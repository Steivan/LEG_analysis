using System;

namespace LEG.E3Dc.Abstractions
{
    public interface IE3DcAggregateArrayRecord
    {
        int Year { get; set; }
        int SubRecordsPerHour { get; set; }
        int SubRecordsPerRange { get; }
        double GetMinutesPerRecord { get; }
        int GetMaxRecordsPerYear { get; }
        DateTime RecordingStartTime { get; set; }
        DateTime RecordingEndTime { get; set; }
        bool[]? IsValid { get; set; }
        int[]? ValidCount { get; set; }
        int[]? BatterySoc { get; set; }
        int[]? BatteryCharging { get; set; }
        int[]? BatteryDischarging { get; set; }
        int[]? NetIn { get; set; }
        int[]? NetOut { get; set; }
        int[]? SolarProduction { get; set; }
        int[]? HouseConsumption { get; set; }
        int[]? WallBoxTotalChargingPower { get; set; }
        int[]? SigmaConsumption { get; set; }

        int DateDateIndex(DateTime date);
        DateTime IndexDateTime(int dateIndex);
        void AggregatePeriodArrayRecord(IE3DcPeriodArrayRecord periodArrayRecord, int recordsPerDay);
        bool RecordingPeriodIsComplete();
    }
}