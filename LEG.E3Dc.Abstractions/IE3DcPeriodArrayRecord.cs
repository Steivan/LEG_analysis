using System;
using System.Collections.Generic;

namespace LEG.E3Dc.Abstractions
{
    public interface IE3DcPeriodArrayRecord
    {
        int Year { get; }
        int GetRecordsPerHour { get; }
        int RecordingStartIndex { get; }
        int RecordingEndIndex { get; }

        void LoadE3DcRecord(IE3DcRecord record);
        bool GetRangeValid(int lo, int hi);
        int GetRangeCount(int lo, int hi);
        int InitialBatterySoc(int lo, int hi);
        int AggregateBatteryCharging(int lo, int hi);
        int AggregateBatteryDischarging(int lo, int hi);
        int AggregateNetIn(int lo, int hi);
        int AggregateNetOut(int lo, int hi);
        int AggregateSolarProduction(int lo, int hi);
        int AggregateHouseConsumption(int lo, int hi);
        int AggregateWallBoxTotalChargingPower(int lo, int hi);
        int AggregateSigmaConsumption(int lo, int hi);
    }
}