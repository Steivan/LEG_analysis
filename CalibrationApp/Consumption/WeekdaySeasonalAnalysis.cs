
namespace CalibrationApp.Consumption
{
    public class WeekdaySeasonalAnalysis
    {
        public class WeekdayStats
        {
            public int Period { get; set; }
            public DayOfWeek DayOfWeek { get; set; }
            public double AverageDailyKWh { get; set; }
            public double StdDevKWh { get; set; }
            public int SampleDays { get; set; }
        }

        // Reuse your existing period logic
        private static int Get13x4Period(DateTime dt) => ConsumptionAnalytics.Get13x4Period(dt);

        public static List<WeekdayStats> AnalyzeWeekdaySeasonality(Dictionary<DateTime, double> data)
        {
            // 1. Roll up to Daily Totals (kWh)
            // Assuming 'data' values are Power in kW. For 15-min intervals, kWh = kW * 0.25
            var dailyTotals = data
                .GroupBy(kvp => kvp.Key.Date)
                .Select(g => new {
                    Date = g.Key,
                    TotalKWh = g.Sum(x => x.Value * 0.25)
                })
                .ToList();

            // 2. Aggregate by Period and Weekday
            return dailyTotals
                .Where(d => Get13x4Period(d.Date) > 0)
                .GroupBy(d => new {
                    Period = Get13x4Period(d.Date),
                    DayOfWeek = d.Date.DayOfWeek
                })
                .Select(g => new WeekdayStats
                {
                    Period = g.Key.Period,
                    DayOfWeek = g.Key.DayOfWeek,
                    AverageDailyKWh = g.Average(x => x.TotalKWh),
                    StdDevKWh = Math.Sqrt(g.Average(x => Math.Pow(x.TotalKWh, 2)) - Math.Pow(g.Average(x => x.TotalKWh), 2)),
                    SampleDays = g.Count()
                })
                .OrderBy(s => s.Period).ThenBy(s => (int)s.DayOfWeek)
                .ToList();
        }

        public void ExportWeekdayFactors(List<WeekdayStats> stats, string filePath)
        {
            // Calculate the mean of each 4-week period across all days
            var periodMeans = stats
                .GroupBy(s => s.Period)
                .ToDictionary(g => g.Key, g => g.Average(x => x.AverageDailyKWh));

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Period,DayOfWeek,AvgKWh,PeriodMean,Factor_G");

                foreach (var s in stats)
                {
                    double periodMean = periodMeans[s.Period];
                    double factorG = s.AverageDailyKWh / periodMean;

                    writer.WriteLine($"{s.Period},{s.DayOfWeek},{s.AverageDailyKWh:F2},{periodMean:F2},{factorG:F4}");
                }
            }
        }
    }
}
