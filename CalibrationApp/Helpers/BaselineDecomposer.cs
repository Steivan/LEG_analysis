namespace CalibrationApp.Helpers
{
    internal class BaselineDecomposer
    {
        const int intervalsPerHour = 4;
        const int hoursPerDay = 24;
        const int intervalsPerDay = hoursPerDay * intervalsPerHour;
        const double omegaDay = 2.0 * Math.PI / hoursPerDay;

        internal static double CyclicGaussian(double x, double a, double mu, double variance, double period = hoursPerDay)
        {
            var delta = Math.Abs(x - mu);
            if (delta > period / 2.0)
            {
                delta = period - delta;
            }

            return GaussianFitter.EvaluateGaussian(delta, a, 0, variance);
        }

        internal static (double baseline, double[] amplitudes) DecomposeSeries(double[] data, double[] lagPeaks, double[] variancePeaks)
        {

            var countPeaks = lagPeaks.Length;

            if (data.Length != intervalsPerDay || countPeaks != variancePeaks.Length) 
            {
                throw new ArgumentException("Input data length mismatch.");
            }

            var min = data.Min();
            var max = data.Max();
            var mean = data.Average();

            var dX = 1.0 / intervalsPerHour;
            var dataIntegrals = new double[countPeaks];
            var selfIntegrals = new double[countPeaks, countPeaks];
            for (var i = 0; i < intervalsPerDay; i++)
            {
                var x = dX * i;
                var y = data[i] - min;
                for (var peak1 = 0; peak1 < countPeaks; peak1++)
                {
                    var p1 = CyclicGaussian(x, 1.0, lagPeaks[peak1], variancePeaks[peak1]);
                    var p1_dX = p1 * dX;
                    dataIntegrals[peak1] += y * p1_dX;
                    selfIntegrals[peak1, peak1] += p1 * p1_dX;
                    for (var peak2 = peak1 + 1; peak2 < countPeaks; peak2++)
                    {
                        var p2 = CyclicGaussian(x, 1.0, lagPeaks[peak2], variancePeaks[peak2]);
                        var p2_p1_dX = p2 * p1_dX;
                        selfIntegrals[peak1, peak2] += p2_p1_dX;
                        selfIntegrals[peak2, peak1] += p2_p1_dX;
                    }
                }
            }

            var amplitudes = SolverNNLS.SolveNonNegative(selfIntegrals, dataIntegrals, maxIterations: 20, tolerance: 1e-3);
            
            return (min, amplitudes);
        }
    }
}
