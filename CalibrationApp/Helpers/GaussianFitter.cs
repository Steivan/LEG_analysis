using System.Data;

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
            (int thresholdLo, int thresholdHi) GetPeakRange(ReadOnlySpan<double> span, int peak, int lo, int hi, double gLo, double gTrend, double a, double thresholdRatio=0.1)
            {
                var gLoThreshold = gLo + a * thresholdRatio;

                var thresholdLo = lo;
                while (span[HelperFunctions.ModuloIndex(thresholdLo, span.Length)] < HelperFunctions.LinearBackground(thresholdLo, lo, gLoThreshold, gTrend) && thresholdLo < peak)
                {
                    thresholdLo++;
                }
                thresholdLo--;

                var thresholdHi = hi;
                while (span[HelperFunctions.ModuloIndex(thresholdHi, span.Length)] < HelperFunctions.LinearBackground(thresholdHi, lo, gLoThreshold, gTrend) && thresholdHi > peak)
                {
                    thresholdHi--;
                }
                thresholdHi++;

                return (thresholdLo, thresholdHi);
            }

            (double mean, double variance, double a) GetStats(ReadOnlySpan<double> span, int peak, int lo, int hi, double gLo, double gTrend, double a, double thresholdRatio = 0.1)
            {
                (lo, hi) = GetPeakRange(span, peak, lo, hi, gLo, gTrend, a, thresholdRatio: thresholdRatio);

                var sumWeights = 0.0;
                var weightedSumX = 0.0;
                var weightedSumXSquared = 0.0;
                for (int i = lo; i <= hi; i++)
                {
                    var weight = span[HelperFunctions.ModuloIndex(i, span.Length)] - HelperFunctions.LinearBackground(i, lo, gLo, gTrend);
                    var weightedX = weight * i;
                    sumWeights += weight;
                    weightedSumX += weightedX;
                    weightedSumXSquared += weightedX * i;
                }
                var E1 = weightedSumX / sumWeights;
                var E2 = weightedSumXSquared / sumWeights;

                var mu = (E1 % span.Length + span.Length) % span.Length;
                var variance = E2 - E1 * E1;

                var fSum = 0.0;
                for (int i = lo; i <= hi; i++)
                {
                    fSum += EvaluateGaussian(i, 1.0, mu, variance);
                }
                a = sumWeights / fSum;

                return (mu, variance, a);
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data array cannot be null or empty.");
            }

            ReadOnlySpan<double> span = data;
            var spanLength = span.Length;

            var deltaIndex = hiIndex - loIndex;

            var gLo = data[HelperFunctions.ModuloIndex(loIndex, spanLength)];
            var gHi = data[HelperFunctions.ModuloIndex(hiIndex, spanLength)];
            var gTrend = (gHi - gLo) / deltaIndex;

            var a = data[HelperFunctions.ModuloIndex(peakIndex, spanLength)] - HelperFunctions.LinearBackground(peakIndex, loIndex, gLo, gTrend);
            var mu = (double)peakIndex;
            var variance = deltaIndex * deltaIndex / 16.0;

            var delta = double.MaxValue;
            var priorA = a;
            var priorMu = mu;
            var priorVariance = variance;
            var iteration = 0;
            while (delta > tolerance && iteration < maxIterations)
            {
                (mu, variance, a) = GetStats(span, peakIndex, loIndex, hiIndex, gLo, gTrend, priorA);

                var deltaA = a - priorA;
                var deltaMu = mu - priorMu;
                delta = deltaA * deltaA + deltaMu * deltaMu + Math.Abs(variance - priorVariance);

                priorA = a;
                priorMu = mu;
                priorVariance = variance;
                iteration++;
            }

            // Calculate R-squared using original data
            double rSquared = CalculateRSquared(span, loIndex, hiIndex, gLo, gTrend, a, mu, variance);

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
                mean += data[HelperFunctions.ModuloIndex(i, data.Length)];
            }
            mean /= n;

            // Calculate R-squared
            for (int i = loIndex; i <= hiIndex; i++)
            {
                var ii = HelperFunctions.ModuloIndex(i, data.Length);
                double linearBbackground = HelperFunctions.LinearBackground(i, loIndex, gLo, gTrend);
                double gaussian = EvaluateGaussian(i, a, mu, variance);
                double background = data[ii] - gaussian;
                if (background < 0) 
                {
                    var debug = 0;
                }
                double residual = background - linearBbackground;

                sumSquaredResiduals += residual * residual;
                sumSquaredTotal += (data[ii] - mean) * (data[ii] - mean);
            }

            return 1.0 - (sumSquaredResiduals / sumSquaredTotal);
        }

    }
}