namespace CalibrationApp.Helpers
{
    internal class PeakDetector
    {
        internal static List<(double PeakValue, int PeakIndex, int loIndex, int hiIndex)> FindAllPeaks(double[] data, double peakThresholdRatio = 0.25, bool smoothingTrue=true, double epsilon=0.001)
        {
            int GetSign(double value)
            {
                if (value > epsilon) return 1;
                if (value < -epsilon) return -1;
                return 0;
            }

            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data array cannot be null or empty.");
            }

            var kernel = new double[] { 0.05, 0.2, 0.5, 0.2, 0.05 };
            var smoothedData = smoothingTrue ? Convolution.ConvoluteCircular(data, kernel, centered: true) : data;

            ReadOnlySpan<double> span = smoothedData;
            var spanLength = span.Length;

            // Step 1: Find all local minima and maxima
            List<(double value, int index)> minimaList = new List<(double value, int index)>();
            List<(double value, int index)> maximaList = new List<(double value, int index)>();
            var priorIndex = - 1;
            var priorValue = smoothedData[HelperFunctions.ModuloIndex(priorIndex, data.Length)];
            var priorDirection = GetSign(priorValue - smoothedData[HelperFunctions.ModuloIndex(priorIndex - 1, data.Length)]);
            var newIndex = 0;
            while (newIndex < spanLength)
            {
                var newValue = smoothedData[newIndex];
                var newDirection = GetSign(newValue - priorValue);
                if (newDirection == 0) newDirection = priorDirection;

                if (priorDirection != newDirection )
                {
                    // Local minimum detected at priorIndex
                    if (newDirection == 1)  minimaList.Add((priorValue, priorIndex));

                    // Local maximum detected at priorIndex
                    if (newDirection == -1) maximaList.Add((priorValue, priorIndex));
                }

                priorIndex = newIndex;
                priorValue = newValue;
                priorDirection = newDirection;
                newIndex++;
            }

            if (maximaList.Count != minimaList.Count)
            {
                throw new ArgumentException("Error in detecting minima and maxima.");
            }

            // Step 2: Validate maxima against minima to identify true peaks
            var peaksList = new List<(double PeakValue, int PeakIndex, int loIndex, int hiIndex)>();
            for (var i = 0; i< minimaList.Count; i++)
            {
                var (loMinValue, loMinIndex) = minimaList[i]; 
                var (hiMinValue, hiMinIndex) = minimaList[i];
                var (maxValue, maxIndex) = maximaList[i];
                if (loMinIndex < maxIndex)
                {
                    (hiMinValue, hiMinIndex) = minimaList[HelperFunctions.ModuloIndex(i + 1, maximaList.Count)];
                }
                if (loMinIndex > maxIndex)
                {
                    (loMinValue, loMinIndex) = minimaList[HelperFunctions.ModuloIndex(i - 1, maximaList.Count)];
                }
                if (loMinIndex > maxIndex) loMinIndex -= spanLength;
                if (hiMinIndex < maxIndex) hiMinIndex += spanLength;

                var background = HelperFunctions.LinearBackground(maxIndex, loMinIndex, loMinValue, (hiMinValue - loMinValue) / (hiMinIndex - loMinIndex));
                var amplitude = maxValue - background;

                if (amplitude >= background * peakThresholdRatio)
                {
                    var (max, lo, hi) = ConfineRange(data, maxIndex, loMinIndex, hiMinIndex);
                    peaksList.Add((maxValue, max, lo, hi));
                }

            }

            return peaksList;
        }

        internal static (double[] smoothedData, List<(int peakIndex, double a, double mu, double variance)> peaks) ExtractAllSpikes(double[] data, double minAmplitudeRatio = 0.2, double maxSigma = 5.0)
        {
            var allPeaks = FindAllPeaks(data);
            var peaks = new List<(int peakIndex, double a, double mu, double sigma)>();

            if (allPeaks.Count == 0)
            {
                return (data, peaks);
            }

            var smoothedData = data.Select(v => v).ToArray();
            var minAmplitude = smoothedData.Max() * minAmplitudeRatio;
            var maxVariance = maxSigma * maxSigma;

            foreach (var (_, peakIndex, loIndex, hiIndex) in allPeaks)
            {
                var gaussianParameters = GaussianFitter.FitGaussian(smoothedData, loIndex, peakIndex, hiIndex, maxIterations: 10);

                var a = gaussianParameters.A;
                var mu = gaussianParameters.Mu;
                var variance = gaussianParameters.Variance;

                if (a >= minAmplitude && variance <= maxVariance)
                {
                    for (var i = 0; i < smoothedData.Length; i++)
                    {
                        var valuePeak = GaussianFitter.EvaluateGaussian(i, a, mu, variance);
                        smoothedData[i] = valuePeak < smoothedData[i] ? smoothedData[i] - valuePeak : 0.0;
                    }
                    peaks.Add((peakIndex, a, mu, variance));
                }
            }

            return (smoothedData, peaks);
        }
        private static (int max, int lo, int hi) ConfineRange(ReadOnlySpan<double> span, int peak, int lo, int hi)
        {
            var gLo = span[HelperFunctions.ModuloIndex(lo, span.Length)];
            var gHi = span[HelperFunctions.ModuloIndex(hi, span.Length)];
            var max = peak;
            var gTrend = (gHi - gLo) / (hi - lo);

            var isConfined = false;
            while (!isConfined && hi - lo > 2)
            {
                isConfined = true;
                // Check if the edges are above the linear background
                for (int i = hi - 1; i > peak; i--)
                {
                    var ii = HelperFunctions.ModuloIndex(i, span.Length);
                    if (span[ii] < HelperFunctions.LinearBackground(i, lo, gLo, gTrend))
                    {
                        isConfined = false;
                        hi = i;
                        gHi = span[ii];
                        gTrend = (gHi - gLo) / (hi - lo);
                    }
                    if (span[ii] > span[HelperFunctions.ModuloIndex(max, span.Length)])     // Update max if a higher value is found for unsmoothed data
                    {
                        max = i;
                    }
                }
                for (int i = lo + 1; i < peak; i++)
                {
                    var ii = HelperFunctions.ModuloIndex(i, span.Length);
                    if (span[ii] < HelperFunctions.LinearBackground(i, lo, gLo, gTrend))
                    {
                        isConfined = false;
                        lo = i;
                        gLo = span[ii];
                        gTrend = (gHi - gLo) / (hi - lo);
                    }
                    if (span[ii] > span[HelperFunctions.ModuloIndex(max, span.Length)])
                    {
                        max = i;
                    }
                }
            }
            return (max, lo, hi);
        }
    }
}
