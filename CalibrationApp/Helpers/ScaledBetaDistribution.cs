
namespace CalibrationApp.Helpers
{
    public class BetaDistributionEstimator
    {
        public record BetaParams(double Alpha, double Beta, double PMax);

        /// <summary>
        /// Estimates Alpha and Beta for a scaled Beta distribution [0, pMax]
        /// </summary>
        /// <param name="residuals">List of observed residuals (Actual - MeanModel)</param>
        /// <param name="pMax">The physical upper bound (e.g., Fuse Limit in Watts)</param>
        public static BetaParams Estimate(List<double> residuals, double pMax)
        {
            // 1. Normalize residuals to [0, 1] range
            // Note: Ensure residuals are non-negative; if they are (Actual - Mean), 
            // you might need to shift them or only model the positive spikes.
            var normalized = residuals.Select(r => Math.Clamp(r / pMax, 0.001, 0.999)).ToList();

            double mean = normalized.Average();
            double variance = normalized.Average(x => Math.Pow(x - mean, 2));

            // 2. Safety check for variance
            if (variance >= mean * (1 - mean))
            {
                // Fallback: If variance is too high, return a flat distribution
                return new BetaParams(1.0, 1.0, pMax);
            }

            // 3. Method of Moments
            double commonFactor = (mean * (1 - mean) / variance) - 1;
            double alpha = mean * commonFactor;
            double beta = (1 - mean) * commonFactor;

            return new BetaParams(alpha, beta, pMax);
        }
    }
}
