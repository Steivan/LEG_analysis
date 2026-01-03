
namespace CalibrationApp.Helpers
{
    public class FourierAnalysis
    {
        const double TwoPi = 2 * Math.PI;

        // Analyzes a diurnal profile (96 points for 15-min data)
        public static (double[] A, double[] B) ComputeCoefficients(double[] profile, int maxHarmonics)
        {
            int N = profile.Length; // 96
            double[] a = new double[maxHarmonics + 1];
            double[] b = new double[maxHarmonics + 1];

            for (int n = 0; n <= maxHarmonics; n++)
            {
                double sumA = 0;
                double sumB = 0;
                for (int t = 0; t < N; t++)
                {
                    double angle = 2 * Math.PI * n * t / N;
                    sumA += profile[t] * Math.Cos(angle);
                    sumB += profile[t] * Math.Sin(angle);
                }
                a[n] = (n == 0) ? sumA / N : 2 * sumA / N;
                b[n] = 2 * sumB / N;
            }
            return (a, b);
        }

    }
}
