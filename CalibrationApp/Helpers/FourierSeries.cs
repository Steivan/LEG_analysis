
namespace CalibrationApp.Helpers
{
    public class FourierSeries
    {
        const double TwoPi = 2 * Math.PI;

        private readonly double[] _a; // Cosine coefficients
        private readonly double[] _b; // Sine coefficients

        public FourierSeries(double[] a, double[] b)
        {
            _a = a;
            _b = b;
        }

        public double Evaluate(double t, double period)
        {
            double angle0 = TwoPi * t / period;
            double value = _a[0];
            for (int n = 1; n < _a.Length; n++)
            {
                double angle = angle0 * n;
                value += _a[n] * Math.Cos(angle) + _b[n] * Math.Sin(angle);
            }
            return value;
        }
    }
}
