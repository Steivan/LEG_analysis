
using static CalibrationApp.Helpers.BetaDistributionEstimator;

namespace CalibrationApp.Generator
{
    public class StochasticGenerator
    {
    // Example of generating a random Beta value (Simple Rejection Sampling or Inverse Transform)
    public double GenerateStochasticPower(BetaParams p, Random rng)
        {
            // Simplified: Use a library or a basic generator for Beta
            double normalizedValue = MathNet.Numerics.Distributions.Beta.Sample(rng, p.Alpha, p.Beta);
            return normalizedValue * p.PMax;
        }

    }
}
