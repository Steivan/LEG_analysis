
using NPOI.HPSF;
using NPOI.SS.Formula.Functions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace CalibrationApp.Helpers
{
    internal class BaselineDecomposer
    {
        const int intervalsPerHour = 4;
        const int hoursPerDay = 24;
        const int intervalsPerDay = hoursPerDay * intervalsPerHour;
        const double omegaDay = 2.0 * Math.PI / hoursPerDay;

        internal static double ShiftedSinus(double x, double a, double lag)
        {
            return a * (1.0 + Math.Sin(omegaDay * (x - lag)));
        }

        internal static double CyclicGaussian(double x, double a, double mu, double variance, double period = hoursPerDay)
        {
            var delta = Math.Abs(x - mu);
            if (delta > period / 2.0)
            {
                delta = period - delta;
            }

            return GaussianFitter.EvaluateGaussian(delta, a, 0, variance);
        }

        internal static double ComputdSeries(double x, double aBackgroung, double aSinus, double lagSinus, double[] aPeaks, double[] lagPeaks, double[] variancePeaks)
        {
            var power = aBackgroung + ShiftedSinus(x, aSinus, lagSinus);
            for (var peak = 0; peak < aPeaks.Length; peak++)
            {
                power += CyclicGaussian(x, aPeaks[peak], lagPeaks[peak], variancePeaks[peak]);
            }

            return power;
        }

        internal static double SquaredErrorSeries(double[] data, double aBackgroung, double aSinus, double lagSinus, double[] aPeaks, double[] lagPeaks, double[] variancePeaks)
        {
            var error = 0.0;
            for (var i = 0; i < intervalsPerDay; i++)
            {
                var x = (double)i / intervalsPerHour;
                var modelValue = ComputdSeries(x, aBackgroung, aSinus, lagSinus, aPeaks, lagPeaks, variancePeaks);
                var diff = data[i] - modelValue;
                error += diff * diff;
            }
            return error;
        }

        internal static void DecomposeSeries(double[] data, double lagSinus, double[] lagPeaks, double[] variancePeaks)
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
            var dataIntegrals = new double[1 + countPeaks];
            var selfIntegrals = new double[1 + countPeaks, 1 + countPeaks];
            for (var i = 0; i < intervalsPerDay; i++)
            {
                var x = dX * i;
                var y = data[i] - min;
                var s = ShiftedSinus(x, 1.0, lagSinus);
                var s_dX = s * dX;
                dataIntegrals[0] += y * s_dX;
                selfIntegrals[0, 0] += s * s_dX;
                for (var peak1 = 0; peak1 < countPeaks; peak1++)
                {
                    var p1 = CyclicGaussian(x, 1.0, lagPeaks[peak1], variancePeaks[peak1]);
                    var p1_dX = p1 * dX;
                    var s_p1_dX = s * p1_dX;  
                    dataIntegrals[1 + peak1] += y * p1_dX;
                    selfIntegrals[0, 1 + peak1] += s_p1_dX;
                    selfIntegrals[1 + peak1, 0] += s_p1_dX;
                    selfIntegrals[1 + peak1, 1 + peak1] += p1 * p1_dX;
                    for (var peak2 = peak1 + 1; peak2 < countPeaks; peak2++)
                    {
                        var p2 = CyclicGaussian(x, 1.0, lagPeaks[peak2], variancePeaks[peak2]);
                        var p2_p1_dX = p2 * p1_dX;
                        selfIntegrals[1 + peak1, 1 + peak2] += p2_p1_dX;
                        selfIntegrals[1 + peak2, 1 + peak1] += p2_p1_dX;
                    }
                }
            }

            var amplitudes = SolverNNLS.SolveNonNegativeSpecial((mean  - min)* hoursPerDay, selfIntegrals, dataIntegrals, maxIterations: 20, tolerance: 1e-3);


        }
    }
}
