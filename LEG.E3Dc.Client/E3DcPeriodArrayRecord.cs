using CsvHelper.Configuration.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LEG.Common;
using LEG.E3Dc.Abstractions;

namespace LEG.E3Dc.Client
{
    public class E3DcPeriodArrayRecord : IE3DcPeriodArrayRecord
    {
        private const int MaxDaysPerYear = 366;
        private const int HoursPerDay = 24;
        private const int RecordsPerHour = 4;
        private const int MaxRecordsPerYear = MaxDaysPerYear * HoursPerDay * RecordsPerHour;
        public int Year { get; set; }
        public int GetRecordsPerHour => RecordsPerHour;
        public static double GetMinutesPerPeriod => 60.0 / RecordsPerHour;
        public static int GetMaxRecordsPerYear => MaxRecordsPerYear;
        public DateTime RecordingStartTime { get; set; }
        public DateTime RecordingEndTime { get; set; }
        public static int DateDateIndex(DateTime date) =>
            ((date.DayOfYear - 1) * HoursPerDay + date.Hour) * RecordsPerHour + (date.Minute * RecordsPerHour) / 60;
        public DateTime IndexDateTime(int dateIndex) =>
            new DateTime(Year, 1, 1, 0, 0, 0).AddMinutes((int)(dateIndex * GetMinutesPerPeriod));
        public int RecordingStartIndex => DateDateIndex(RecordingStartTime);
        public int RecordingEndIndex => DateDateIndex(RecordingEndTime);
        public bool[] IsValid { get; set; } = new bool[MaxRecordsPerYear];
        public int[] BatterySoc { get; set; } = new int[MaxRecordsPerYear];
        public int[] BatteryCharging { get; set; } = new int[MaxRecordsPerYear];
        public int[] BatteryDischarging { get; set; } = new int[MaxRecordsPerYear];
        public int[] NetIn { get; set; } = new int[MaxRecordsPerYear];    // Power feed into network
        public int[] NetOut { get; set; } = new int[MaxRecordsPerYear];    // Power drawn from network
        public int[] SolarProduction { get; set; } = new int[MaxRecordsPerYear];
        public int[] HouseConsumption { get; set; } = new int[MaxRecordsPerYear];
        public int[] WallBoxTotalChargingPower { get; set; } = new int[MaxRecordsPerYear];
        public int[] SigmaConsumption { get; set; } = new int[MaxRecordsPerYear];
        private static void ValidateRange(int loIndex, int hiIndex)
        {
            if (loIndex < 0 || hiIndex >= MaxRecordsPerYear || loIndex > hiIndex)
                throw new ArgumentOutOfRangeException($"Index range {loIndex} ... {hiIndex} is out of bounds.");
        }
        public static int GetRangeSum(int loIndex, int hiIndex, int[] intArray)
        {
            ValidateRange(loIndex, hiIndex);
            return Enumerable.Range(loIndex, hiIndex - loIndex + 1).Select(i => intArray[i]).Sum();
        }
        public bool RecordingPeriodIsComplete() => Enumerable.Range(RecordingStartIndex, RecordingEndIndex - RecordingStartIndex + 1)
            .All(i => IsValid[i]);
        public bool GetRangeValid(int loIndex, int hiIndex)
        {
            ValidateRange(loIndex, hiIndex);
            return Enumerable.Range(loIndex, hiIndex - loIndex + 1).All(i => IsValid[i]);
        }
        public int GetRangeCount(int loIndex, int hiIndex)
        {
            ValidateRange(loIndex, hiIndex);
            return Enumerable.Range(loIndex, hiIndex - loIndex + 1).Select(i => IsValid[i] ? 1 : 0).Sum();
        }
        public int InitialBatterySoc(int loIndex, int hiIndex) => BatterySoc[loIndex];
        public int AggregateBatteryCharging(int loIndex, int hiIndex) => GetRangeSum(loIndex, hiIndex, BatteryCharging);
        public int AggregateBatteryDischarging(int loIndex, int hiIndex) => GetRangeSum(loIndex, hiIndex, BatteryDischarging);
        public int AggregateNetIn(int loIndex, int hiIndex) => GetRangeSum(loIndex, hiIndex, NetIn);
        public int AggregateNetOut(int loIndex, int hiIndex) => GetRangeSum(loIndex, hiIndex, NetOut);
        public int AggregateSolarProduction(int loIndex, int hiIndex) => GetRangeSum(loIndex, hiIndex, SolarProduction);
        public int AggregateHouseConsumption(int loIndex, int hiIndex) => GetRangeSum(loIndex, hiIndex, HouseConsumption);
        public int AggregateWallBoxTotalChargingPower(int loIndex, int hiIndex) => GetRangeSum(loIndex, hiIndex, WallBoxTotalChargingPower);
        public int AggregateSigmaConsumption(int loIndex, int hiIndex) => GetRangeSum(loIndex, hiIndex, SigmaConsumption);

        public void InitArrayRecord(int shortOrLongYear)
        {
            Year = shortOrLongYear < 100 ? 2000 + shortOrLongYear : shortOrLongYear;
            RecordingStartTime = new DateTime(Year, 12, 31, 23, 59, 59);
            RecordingEndTime = new DateTime(Year, 1, 1, 20, 0, 0);
            Array.Fill(IsValid, false);
            Array.Fill(BatterySoc, 0);
            Array.Fill(BatteryCharging, 0);
            Array.Fill(BatteryDischarging, 0);
            Array.Fill(NetIn, 0);
            Array.Fill(NetOut, 0);
            Array.Fill(SolarProduction, 0);
            Array.Fill(HouseConsumption, 0);
            Array.Fill(WallBoxTotalChargingPower, 0);
            Array.Fill(SigmaConsumption, 0);
        }
        public void LoadE3DcRecord(IE3DcRecord record)
        {
            var timestamp = E3DcFileHelper.ParseTimestamp(record.Timestamp);
            // Validate year
            if (timestamp.Year != Year)
                throw new ArgumentException($"Record year {timestamp.Year} does not match array record year {Year}.");
            // ComputePvSiteAggregateProduction index
            var index = DateDateIndex(timestamp);
            if (timestamp.Year != Year)
                throw new ArgumentException($"Record year {timestamp.Year} does not match array record year {Year}.");
            // Update start and end dates
            if (timestamp < RecordingStartTime) RecordingStartTime = timestamp;
            if (timestamp > RecordingEndTime) RecordingEndTime = timestamp;
            // Add record data to arrays
            if (IsValid[index])
                Console.WriteLine($" -> Warning: duplicate record found for {timestamp}");
            IsValid[index] = true;
            BatterySoc[index] = record.BatterySoc;
            BatteryCharging[index] = record.BatteryCharging;
            BatteryDischarging[index] = record.BatteryDischarging;
            NetIn[index] = record.NetIn;
            NetOut[index] = record.NetOut;
            SolarProduction[index] = record.SolarProduction;
            HouseConsumption[index] = record.HouseConsumption;
            WallBoxTotalChargingPower[index] = record.WallBoxId0TotalChargingPower + record.WallBoxId1TotalChargingPower;
            SigmaConsumption[index] = HouseConsumption[index] + BatteryCharging[index] + WallBoxTotalChargingPower[index];
        }

    }
}