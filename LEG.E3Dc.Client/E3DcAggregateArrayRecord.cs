using LEG.E3Dc.Abstractions;

namespace LEG.E3Dc.Client
{
    public class E3DcAggregateArrayRecord : IE3DcAggregateArrayRecord
    {
        private const int MaxDaysPerYear = 366;
        private const double HoursPerDay = 24.0;
        private const double MinutesPerHour = 60.0;
        private const double MinutesPerDay = MinutesPerHour * HoursPerDay;

        public int Year { get; set; }
        private int RecordsPerDay { get; set; }
        public int SubRecordsPerHour { get; set; }
        public int SubRecordsPerRange => (int)HoursPerDay * SubRecordsPerHour / RecordsPerDay;
        public double GetMinutesPerRecord => MinutesPerDay / RecordsPerDay;
        public int GetMaxRecordsPerYear => MaxDaysPerYear * RecordsPerDay;
        public DateTime RecordingStartTime { get; set; }
        public DateTime RecordingEndTime { get; set; }

        public bool[]? IsValid { get; set; }
        public int[]? ValidCount { get; set; }
        public int[]? BatterySoc { get; set; }
        public int[]? BatteryCharging { get; set; }
        public int[]? BatteryDischarging { get; set; }
        public int[]? NetIn { get; set; }
        public int[]? NetOut { get; set; }
        public int[]? SolarProduction { get; set; }
        public int[]? HouseConsumption { get; set; }
        public int[]? WallBoxTotalChargingPower { get; set; }
        public int[]? SigmaConsumption { get; set; }

        public int DateDateIndex(DateTime date) =>
            (date.DayOfYear - 1) * RecordsPerDay + (int)((date.Hour + date.Minute / MinutesPerHour) / HoursPerDay * RecordsPerDay);

        public DateTime IndexDateTime(int dateIndex) =>
            new DateTime(Year, 1, 1, 0, 0, 0).AddMinutes((int)(dateIndex * GetMinutesPerRecord));

        public int RecordingStartIndex => DateDateIndex(RecordingStartTime);
        public int RecordingEndIndex => DateDateIndex(RecordingEndTime);

        private void InitArrayRecord(int year, int subRecordsPerHour, int recordsPerDay)
        {
            Year = year;
            List<int> validRecordsPerDay = subRecordsPerHour switch
            {
                1  => [ 1, 2, 3, 4, 6, 8, 12, 24 ],
                2  => [ 1, 2, 3, 4, 6, 8, 12, 24, 48 ],
                3  => [ 1, 2, 3, 4, 6, 8, 12, 24, 72 ],
                4  => [ 1, 2, 3, 4, 6, 8, 12, 24, 48, 96 ],
                5  => [ 1, 2, 3, 4, 6, 8, 12, 24, 120 ],
                6  => [ 1, 2, 3, 4, 6, 8, 12, 24, 48, 72, 144 ],
                10 => [ 1, 2, 3, 4, 6, 8, 12, 24, 48, 120, 240 ],
                12 => [ 1, 2, 3, 4, 6, 8, 12, 24, 48, 72, 96, 144, 288 ],
                15 => [ 1, 2, 3, 4, 6, 8, 12, 24, 72, 120, 360 ],
                20 => [ 1, 2, 3, 4, 6, 8, 12, 24, 48, 96, 120, 240, 480 ],
                30 => [ 1, 2, 3, 4, 6, 8, 12, 24, 48, 72, 120, 144, 240, 360, 720 ],
                60 => [ 1, 2, 3, 4, 6, 8, 12, 24, 48, 72, 96, 120, 144, 240, 288, 360, 480, 720, 1440 ],
                _ => throw new ArgumentOutOfRangeException(nameof(subRecordsPerHour), $"Unsupported PerHour value: {subRecordsPerHour}")
            };
            if (!validRecordsPerDay.Contains(recordsPerDay))
                throw new ArgumentOutOfRangeException($"Records per day {recordsPerDay} is not valid.");

            RecordsPerDay = recordsPerDay;
            SubRecordsPerHour = subRecordsPerHour;

            RecordingStartTime = new DateTime(Year, 1, 1, 0, 0, 0);
            RecordingEndTime = new DateTime(Year, 12, 31, 23, 59, 59);

            IsValid = new bool[GetMaxRecordsPerYear];
            ValidCount = new int[GetMaxRecordsPerYear];
            BatterySoc = new int[GetMaxRecordsPerYear];
            BatteryCharging = new int[GetMaxRecordsPerYear];
            BatteryDischarging = new int[GetMaxRecordsPerYear];
            NetIn = new int[GetMaxRecordsPerYear];
            NetOut = new int[GetMaxRecordsPerYear];
            SolarProduction = new int[GetMaxRecordsPerYear];
            HouseConsumption = new int[GetMaxRecordsPerYear];
            WallBoxTotalChargingPower = new int[GetMaxRecordsPerYear];
            SigmaConsumption = new int[GetMaxRecordsPerYear];
        }

        public void AggregatePeriodArrayRecord(IE3DcPeriodArrayRecord periodArrayRecord, int recordsPerDay)
        {
            InitArrayRecord(periodArrayRecord.Year, periodArrayRecord.GetRecordsPerHour, recordsPerDay);

            var aggregateStartIndex = (RecordingStartIndex + SubRecordsPerRange - 1) / SubRecordsPerRange;
            var aggregateEndIndex = RecordingEndIndex / SubRecordsPerRange;
            RecordingStartTime = IndexDateTime(aggregateStartIndex);
            RecordingEndTime = IndexDateTime(aggregateEndIndex);

            if (IsValid == null || ValidCount == null || BatterySoc == null || BatteryCharging == null ||
                BatteryDischarging == null || NetIn == null || NetOut == null || SolarProduction == null ||
                HouseConsumption == null || WallBoxTotalChargingPower == null || SigmaConsumption == null)
            {
                return; // Or throw an exception if this state is unexpected
            }

            for (var index = aggregateStartIndex; index <= aggregateEndIndex; index++)
            {
                var loPeriod = index * SubRecordsPerRange;
                var hiPeriod = loPeriod + SubRecordsPerRange - 1;
                IsValid[index] = periodArrayRecord.GetRangeValid(loPeriod, hiPeriod);
                ValidCount[index] = periodArrayRecord.GetRangeCount(loPeriod, hiPeriod);
                BatterySoc[index] = periodArrayRecord.InitialBatterySoc(loPeriod, hiPeriod);
                BatteryCharging[index] = periodArrayRecord.AggregateBatteryCharging(loPeriod, hiPeriod);
                BatteryDischarging[index] = periodArrayRecord.AggregateBatteryDischarging(loPeriod, hiPeriod);
                NetIn[index] = periodArrayRecord.AggregateNetIn(loPeriod, hiPeriod);
                NetOut[index] = periodArrayRecord.AggregateNetOut(loPeriod, hiPeriod);
                SolarProduction[index] = periodArrayRecord.AggregateSolarProduction(loPeriod, hiPeriod);
                HouseConsumption[index] = periodArrayRecord.AggregateHouseConsumption(loPeriod, hiPeriod);
                WallBoxTotalChargingPower[index] = periodArrayRecord.AggregateWallBoxTotalChargingPower(loPeriod, hiPeriod);
                SigmaConsumption[index] = periodArrayRecord.AggregateSigmaConsumption(loPeriod, hiPeriod);
            }
        }

        public bool RecordingPeriodIsComplete()
        {
            if (IsValid == null)
            {
                return false;
            }
            return Enumerable.Range(RecordingStartIndex, RecordingEndIndex - RecordingStartIndex + 1)
                .All(i => IsValid[i]);
        }
    }
}