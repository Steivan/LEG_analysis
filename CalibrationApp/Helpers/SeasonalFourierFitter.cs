
namespace CalibrationApp.Helpers
{
    public class SeasonalFourierFitter
    {
        private const double TropicalYearDays = 365.2422;
        const double TwoPi = 2 * Math.PI;

        public static (double[] A, double[] B) FitFromIntervalData(
            Dictionary<DateTime, double> data,
            DateTime referenceDate,
            int harmonics)
        {
            double[] a = new double[harmonics + 1];
            double[] b = new double[harmonics + 1];

            int totalPoints = data.Count;
            if (totalPoints == 0) return (a, b);

            // We treat the data as a continuous stream to find the average and seasonal swings
            double sumA0 = 0;
            double[] sumA = new double[harmonics + 1];
            double[] sumB = new double[harmonics + 1];

            var omega = TwoPi / TropicalYearDays;
            foreach (var kvp in data)
            {
                double daysSinceRef = (kvp.Key - referenceDate).TotalDays;
                double power = kvp.Value;

                sumA0 += power;
                var angle0 = omega * daysSinceRef;
                for (int n = 1; n <= harmonics; n++)
                {
                    double angle = angle0 * n;
                    sumA[n] += power * Math.Cos(angle);
                    sumB[n] += power * Math.Sin(angle);
                }
            }

            // Normalization
            a[0] = sumA0 / totalPoints;
            for (int n = 1; n <= harmonics; n++)
            {
                a[n] = 2.0 * sumA[n] / totalPoints;
                b[n] = 2.0 * sumB[n] / totalPoints;
            }

            return (a, b);
        }
    }
}
