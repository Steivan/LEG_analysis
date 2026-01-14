
namespace CalibrationApp.Helpers
{
    public class FourierSeries
    {
        const double TwoPi = 2 * Math.PI;

        private readonly double[] _a; // Cosine coefficients
        private readonly double[] _b; // Sine coefficients

        public int Terms => Math.Min(_a.Length, _b.Length) - 1;

        public FourierSeries(double[] a, double[] b)
        {
            _a = a;
            _b = b;
        }

        public static FourierSeries FourierSeriesFromData(double[] support, double[] values, double period, int terms)
        {
            var samplePoints = support.Length;
            var normalizationFactor = 2.0 / samplePoints;
            var omega = TwoPi / period;
            var a = new double[terms + 1];
            var b = new double[terms + 1];
            for (var n = 0; n <= terms; n++)
            {
                var omega_n = omega * n;
                var sumA = 0.0;
                var sumB = 0.0;
                for (int k = 0; k < samplePoints; k++)
                {
                    var angle = omega_n * support[k];
                    sumA += values[k] * Math.Cos(angle);
                    sumB += values[k] * Math.Sin(angle);
                }
                a[n] = normalizationFactor * sumA;
                b[n] = normalizationFactor * sumB;
            }
            a[0] *= 0.5; // Adjust the DC component
            b[0] = 0.0;

            return new FourierSeries(a, b);
        }

        public double Evaluate(double t, double period)
        {
            var angle0 = TwoPi / period * t;
            var value = _a[0];
            for (var n = 1; n <= Terms; n++)
            {
                var angle = angle0 * n;
                value += _a[n] * Math.Cos(angle) + _b[n] * Math.Sin(angle);
            }

            return value;
        }

        public double[] EvaluateArray(double[] support, double period)
        {
            var omega = TwoPi / period;
            var samplePoints = support.Length;
            var values = support.Select(v => _a[0]).ToArray();
            for (var n = 1; n <= Terms; n++)
            {
                var omega_n = omega * n;
                for (var k = 0; k < samplePoints; k++)
                {
                    var angle = omega_n * support[k];
                    values[k] += _a[n] * Math.Cos(angle) + _b[n] * Math.Sin(angle);
                }
            }

            return values;
        }
    }
}
