using MathNet.Numerics.Distributions;

namespace CalibrationApp.Helpers
{
    internal class GaussianFitter
    {
        internal record GaussianParameters(double A, double Mu, double Variance, double RSquared);
        internal static double EvaluateGaussian(double x, double a, double mu, double variance)
        {
            double diff = x - mu;
            return a * Math.Exp(-diff * diff / 2 / variance);
        }

        internal static GaussianParameters FitGaussian(double[] data, int loIndex, int peakIndex, int hiIndex, int maxIterations = 10, double tolerance = 1e-3)
        {
            // Method of Moments fitting of a Gaussian peak with linear background
            (double mean, double variance ) GetStats(ReadOnlySpan<double> span, int lo, int hi, double gLo, double gTrend)
            {
                var sumWeights = 0.0;
                var weightedSumX = 0.0;
                var weightedSumXSquared = 0.0;
                for (int i = lo; i <= hi; i++)
                {
                    var ii = HelperFunctions.ModuloIndex(i, span.Length);
                    var weight = span[ii] - HelperFunctions.LinearBackground(i, lo, gLo, gTrend);
                    var weightedX = weight * i;
                    sumWeights += weight;
                    weightedSumX += weightedX;
                    weightedSumXSquared += weightedX * i;
                }
                var E1 = weightedSumX / sumWeights;
                var E2 = weightedSumXSquared / sumWeights;

                var mu = (E1 % span.Length + span.Length) % span.Length;
                var variance = E2 - E1 * E1;

                return (mu, variance);
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data array cannot be null or empty.");
            }

            ReadOnlySpan<double> span = data;
            var spanLength = span.Length;
            //(loIndex, hiIndex) = ConfineRange(span, loIndex, peakIndex, hiIndex);

            var deltaIndex = hiIndex - loIndex;
            var priorMu = (double)peakIndex;
            var priorVariance = deltaIndex * deltaIndex / 16.0;

            var gLo = data[HelperFunctions.ModuloIndex(loIndex, spanLength)];
            var gHi = data[HelperFunctions.ModuloIndex(hiIndex, spanLength)];
            var gTrend = (gHi - gLo) / deltaIndex;

            var fPeak = 1.0;
            var g0Mu = HelperFunctions.LinearBackground(priorMu, loIndex, gLo, gTrend);
            var a = (data[peakIndex] - g0Mu) / fPeak;

            var fLoPrior = 0.0;
            var fHiPrior = 0.0;

            var iteration = -1;
            var delta = double.MaxValue;
            while (iteration < maxIterations && delta > tolerance)
            {
                iteration++;
                var (mu, variance) = GetStats(span, loIndex, hiIndex, gLo, gTrend);

                fPeak = EvaluateGaussian(peakIndex, 1.0, mu, variance);
                g0Mu = HelperFunctions.LinearBackground(mu, loIndex, gLo, gTrend);
                a = (data[peakIndex] - g0Mu) / fPeak;

                var fLo = EvaluateGaussian(loIndex, a, mu, variance);
                var fHi = EvaluateGaussian(hiIndex, a, mu, variance);

                gLo = Math.Max(0.0, data[loIndex] - fLo);
                gHi = Math.Max(0.0, data[hiIndex] - fHi);
                gTrend = (gHi - gLo) / deltaIndex;

                delta = Math.Abs(mu - priorMu) + Math.Abs(variance - priorVariance);
                delta += Math.Abs(fLo - fLoPrior) + Math.Abs(fHi - fHiPrior);

                priorMu = mu;
                priorVariance = variance;
                fLoPrior = fLo;
                fHiPrior = fHi;
            }

            // Calculate R-squared using original data
            double rSquared = CalculateRSquared(span, loIndex, hiIndex, gLo, gTrend, a, priorMu, priorVariance);

            return new GaussianParameters(a, priorMu, priorVariance, rSquared);
        }

        private static double CalculateRSquared(ReadOnlySpan<double> data, int loIndex, int hiIndex, double gLo, double gTrend,  double a, double mu, double variance)
        {
            double sumSquaredResiduals = 0;
            double sumSquaredTotal = 0;
            double mean = 0;
            int n = hiIndex - loIndex + 1;

            // Calculate mean
            for (int i = loIndex; i <= hiIndex; i++)
            {
                mean += data[i];
            }
            mean /= n;

            // Calculate R-squared
            for (int i = loIndex; i <= hiIndex; i++)
            {
                double linearBbackground = HelperFunctions.LinearBackground(i, loIndex, gLo, gTrend);
                double gaussian = EvaluateGaussian(i, a, mu, variance);
                double background = data[i] - gaussian;
                if (background < 0) 
                {
                    var debug = 0;
                }
                double residual = background - linearBbackground;

                sumSquaredResiduals += residual * residual;
                sumSquaredTotal += (data[i] - mean) * (data[i] - mean);
            }

            return 1.0 - (sumSquaredResiduals / sumSquaredTotal);
        }

    }
}