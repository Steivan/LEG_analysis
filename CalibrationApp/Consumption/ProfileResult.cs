using static CalibrationApp.Consumption.ConsumptionAnalytics;

namespace CalibrationApp.Consumption
{
    public class ProfileResult
    {
        public int Period { get; set; }
        public TimeSpan TimeOfDay { get; set; }
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public int SampleCount { get; set; }

        public List<ProfileResult> GenerateMeanProfiles(Dictionary<DateTime, double> data)
        {
            return data
                .Where(kvp => Get13x4Period(kvp.Key) > 0)
                .GroupBy(kvp => new
                {
                    Period = Get13x4Period(kvp.Key),
                    Time = kvp.Key.TimeOfDay
                })
                .Select(g => new ProfileResult
                {
                    Period = g.Key.Period,
                    TimeOfDay = g.Key.Time,
                    Mean = g.Average(x => x.Value),
                    StdDev = Math.Sqrt(g.Average(x => Math.Pow(x.Value, 2)) - Math.Pow(g.Average(x => x.Value), 2)),
                    SampleCount = g.Count()
                })
                .OrderBy(r => r.Period).ThenBy(r => r.TimeOfDay)
                .ToList();
        }
    }
}
