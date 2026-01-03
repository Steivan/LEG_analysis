using static CalibrationApp.Consumption.ConsumptionAnalytics;

namespace CalibrationApp.Consumption
{
    public class ResidualAnalytics
    {
        public void ExportResidualAnalysis(Dictionary<DateTime, double> data, string filePath)
        {
            // 1. Pre-calculate the means for every possible (Period, DayOfWeek, Time) slot
            var lookup = data
                .Where(kvp => Get13x4Period(kvp.Key) > 0)
                .GroupBy(kvp => new
                {
                    P = Get13x4Period(kvp.Key),
                    D = kvp.Key.DayOfWeek,
                    T = kvp.Key.TimeOfDay
                })
                .ToDictionary(g => g.Key, g => g.Average(x => x.Value));

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Timestamp,Actual,Expected,Residual,AbsRelativeError");

                foreach (var kvp in data)
                {
                    var key = new
                    {
                        P = Get13x4Period(kvp.Key),
                        D = kvp.Key.DayOfWeek,
                        T = kvp.Key.TimeOfDay
                    };

                    if (lookup.TryGetValue(key, out double expected))
                    {
                        double actual = kvp.Value;
                        double residual = actual - expected;
                        double relError = expected > 0 ? Math.Abs(residual) / expected : 0;

                        writer.WriteLine($"{kvp.Key:yyyy-MM-dd HH:mm},{actual},{expected},{residual},{relError}");
                    }
                }
            }
        }
    }
}
