using static CalibrationApp.Consumption.ConsumptionAnalytics;

namespace CalibrationApp.Consumption
{
    public class DiurnalSeasonalAnalysis
    {
        public class TimeSlotStats
        {
            public int Period13x4 { get; set; }
            public TimeSpan TimeOfDay { get; set; }
            public int Count { get; set; }
            public double Mean { get; set; }
            public double Max { get; set; }
            public double P25 { get; set; } // 25th percentile
            public double P50 { get; set; }
            public double P75 { get; set; } // 75th percentile
            public double P90 { get; set; } // 90th percentile (Peak potential)
            public double InterQuartileRange => P75 - P25; // Measure of "Predictability"
        }

        public static List<TimeSlotStats> AnalyzeSeasonalConsistency(Dictionary<DateTime, double> data)
        {
            return data
                .Where(kvp => Get13x4Period(kvp.Key) > 0)
                .GroupBy(kvp => new {
                    Period = Get13x4Period(kvp.Key),
                    Time = kvp.Key.TimeOfDay
                })
                .Select(g => {
                    var values = g.Select(x => x.Value).OrderBy(v => v).ToList();
                    int count = values.Count;

                    return new TimeSlotStats
                    {
                        Period13x4 = g.Key.Period,
                        TimeOfDay = g.Key.Time,
                        Count = count,
                        Mean = values.Average(),
                        Max = values.Max(),
                        P25 = values[(int)(count * 0.25)],
                        P50 = values[count / 2],
                        P75 = values[(int)(count * 0.75)],
                        P90 = values[(int)(count * 0.9)]
                    };
                })
                .OrderBy(x => x.Period13x4).ThenBy(x => x.TimeOfDay)
                .ToList();
        }
    }
}
