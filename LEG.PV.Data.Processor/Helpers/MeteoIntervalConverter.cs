using System;
using System.Collections.Generic;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.PV.Data.Processor.Helpers
{
    internal class MeteoIntervalConverter
    {

        internal static Dictionary<DateTime, MeteoParameters> MeteoIntervalSplitter(Dictionary<DateTime, MeteoParameters> inputSeriesDict, int subIntervalsTo)
        {
            if (subIntervalsTo < 1) throw new ArgumentException("Target interval must be a finite fraction of the input interval.");
            if (subIntervalsTo == 1) return inputSeriesDict;

            var keys = inputSeriesDict.Keys.ToList();
            var inputInterval = inputSeriesDict[keys[0]].Interval;
            var targetInterval = inputInterval / subIntervalsTo;

            var splitSeriesDict = new Dictionary<DateTime, MeteoParameters>();
            foreach (var key in keys)
            {
                var inputRecord = inputSeriesDict[key];
                var inputTimeStamp = inputRecord.Time;

                for (int i = 0; i < subIntervalsTo; i++)
                {
                    var subIntervalTime = inputTimeStamp.Add(targetInterval * i);
                    splitSeriesDict[subIntervalTime] = SplitMeteoRecord(inputRecord, subIntervalTime, subIntervalsTo);
                }
            }

            return splitSeriesDict;
        }
        internal static Dictionary<DateTime, MeteoParameters> MeteoIntervalAggregator(Dictionary<DateTime, MeteoParameters> inputSeriesDict, int subIntervalsFrom)
        {
            if (subIntervalsFrom < 1) throw new ArgumentException("Target interval must be greater than or equal to input interval.");
            if (subIntervalsFrom == 1) return inputSeriesDict;

            var aggregatedSeries = new Dictionary<DateTime, MeteoParameters>();
            if (inputSeriesDict.Count == 0) return aggregatedSeries;

            var keys = inputSeriesDict.Keys.ToList();
            keys.Sort();
            var inputInterval = inputSeriesDict[keys[0]].Interval;
            var targetInterval = inputInterval * subIntervalsFrom;

            for (int i = 0; i < inputSeriesDict.Count; i += subIntervalsFrom)
            {
                var group = inputSeriesDict.Skip(i).Take(subIntervalsFrom).ToList();
                var groupRecords = group.Select(kv => kv.Value).ToList();
                if (group.Count == subIntervalsFrom)
                {
                    var aggregatedRecord = AggregateMeteoRecords(groupRecords);
                    aggregatedSeries.Add(aggregatedRecord.Time, aggregatedRecord);
                }
            }
            return aggregatedSeries;
        }

        internal static Dictionary<DateTime, MeteoParameters> MeteoFromToConvertor(Dictionary<DateTime, MeteoParameters> inputSeriesDict, TimeSpan targetInterval)
        {
            var inputMinutesPerInterval = (int)inputSeriesDict.Values.First().Interval.TotalMinutes;
            var targetMinutesPerInterval = (int)targetInterval.TotalMinutes;
            if (targetMinutesPerInterval == inputMinutesPerInterval) return inputSeriesDict;

            var validInputIntervals = 1440 / (1440 / inputMinutesPerInterval) == inputMinutesPerInterval;
            var validTargetIntervals = 1440 / (1440 / targetMinutesPerInterval) == targetMinutesPerInterval;
            if (!validTargetIntervals || !validInputIntervals) throw new ArgumentException("Intervals must be fractions of 1440 [minutes]");

            var gcd = GCD(inputMinutesPerInterval, targetMinutesPerInterval);
            var subIntervalsTo = inputMinutesPerInterval / gcd;
            var subIntervalsFrom = targetMinutesPerInterval / gcd;
            var convertedSeries = MeteoIntervalSplitter(inputSeriesDict, subIntervalsTo);

            return MeteoIntervalAggregator(convertedSeries, subIntervalsFrom);
        }

        // *****************************************************************************************

        // Euclidean algorithm for Greatest Common Divisor
        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return Math.Abs(a);
        }

        private static MeteoParameters SplitMeteoRecord(MeteoParameters inputRecord, DateTime newTime, int countSubIntervals)
        {
            return inputRecord with
            {
                Time = newTime,
                Interval = inputRecord.Interval / countSubIntervals,
                SunshineDuration = inputRecord.SunshineDuration / countSubIntervals,
                DirectRadiation = inputRecord.DirectRadiation / countSubIntervals,
                DirectNormalIrradiance = inputRecord.DirectNormalIrradiance / countSubIntervals,
                GlobalRadiation = inputRecord.GlobalRadiation / countSubIntervals,
                DiffuseRadiation = inputRecord.DiffuseRadiation / countSubIntervals,
                RadiationVariance = inputRecord.RadiationVariance / countSubIntervals
            };
        }

        private static MeteoParameters AggregateMeteoRecords(List<MeteoParameters> inputRecords)
        {
            var time = inputRecords.Max(r => r.Time);
            var interval = TimeSpan.FromTicks(inputRecords.Sum(r => r.Interval.Ticks));
            double? sunshineDuration = inputRecords.Sum(r => r.SunshineDuration ?? 0.0);
            double? directRadiation = inputRecords.Sum(r => r.DirectRadiation ?? 0.0);
            double? directNormalIrradiance = inputRecords.Average(r => r.DirectNormalIrradiance);
            double? globalRadiation = inputRecords.Sum(r => r.GlobalRadiation ?? 0.0);
            double? diffuseRadiation = inputRecords.Sum(r => r.DiffuseRadiation ?? 0.0);
            double? temperature = inputRecords.Average(r => r.Temperature);
            var (windSpeed, windDirection) = WindVectorsAggregator.AggregatedWindVectorsFromList(inputRecords);
            double? snowDepth = inputRecords.Average(r => r.SnowDepth);
            double? dewPoint = inputRecords.Average(r => r.DewPoint);
            double? relativeHumidity = inputRecords.Average(r => r.RelativeHumidity);

            return new MeteoParameters(
                time: time,
                interval: interval,
                sunshineDuration: sunshineDuration,
                directRadiation: directRadiation,
                directNormalIrradiance: directNormalIrradiance,
                globalRadiation: globalRadiation,
                diffuseRadiation: diffuseRadiation,
                temperature: temperature,
                windSpeed: windSpeed,
                windDirection: windDirection,
                snowDepth: snowDepth,
                relativeHumidity: relativeHumidity,
                dewPoint: dewPoint
            );
        }
    }
}
