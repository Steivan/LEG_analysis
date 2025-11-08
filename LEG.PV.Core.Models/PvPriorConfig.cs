namespace LEG.PV.Core.Models
{
    public class PvPriorConfig
    {
        internal const double meanEthaSys = 0.85;
        internal const double sigmaEthaSys = 0.05;
        internal const double minEthaSys = 0.0;
        internal const double maxEthaSys = 1.0;

        internal const double meanGamma = -0.004;            // [/°C]
        internal const double sigmaGamma = 0.0005;
        internal const double minGamma = double.MinValue;
        internal const double maxGamma = 0;

        internal const double meanU0 = 29;                   // [W/m^2 K]
        internal const double sigmaU0 = 4;
        internal const double minU0 = 1e-6;
        internal const double maxU0 = double.MaxValue;

        internal const double meanU1 = 0.5;                  // [W/m^2 K per km/h]
        internal const double sigmaU1 = 0.1;
        internal const double minU1 = 1e-6;
        internal const double maxU1 = double.MaxValue;

        internal const double meanLDegr = 0.008;             // [/year]
        internal const double sigmaLDegr = 0.002;
        internal const double minLDegr = 0.0;
        internal const double maxLDegr = 0.03;

        //// eta_sys: [0, 1]
        //theta[0] = Math.Min(1.0, Math.Max(0.0, theta[0]));

        //// gamma: Must be non-positive (efficiency decreases with temp)
        //theta[1] = Math.Min(0.0, theta[1]);

        //// U0, U1: Must be positive (heat loss must occur)
        //theta[2] = Math.Max(1e-6, theta[2]); // U0 > 0
        //theta[3] = Math.Max(1e-6, theta[3]); // U1 >= 0

        //// L_degr: [0, 0.03] (Degradation loss)
        //theta[4] = Math.Min(0.03, Math.Max(0.0, theta[4]));

        public static (double mean, double sigma, double min, double max) GetPriorsEtha()
        {
            return (meanEthaSys, sigmaEthaSys, minEthaSys, maxEthaSys);
        }

        public static (double mean, double sigma, double min, double max) GetPriorsGamma()
        {
            return (meanGamma, sigmaGamma, minGamma, maxGamma);
        }

        public static (double mean, double sigma, double min, double max) GetPriorsU0()
        {
            return (meanU0, sigmaU0, minU0, maxU0);
        }

        public static (double mean, double sigma, double min, double max) GetPriorsU1()
        {
            return (meanU1, sigmaU1, minU1, maxU1);
        }

        public static (double mean, double sigma, double min, double max) GetPriorsLDegr()
        {
            return (meanLDegr, sigmaLDegr, minLDegr, maxLDegr);
        }
        public static (double mean, double sigma, double min, double max) GetPriorSignature(int priorIndex)
        {
            return (priorIndex % 5) switch
            {
                0 => GetPriorsEtha(),
                1 => GetPriorsGamma(),
                2 => GetPriorsU0(),
                3 => GetPriorsU1(),
                4 => GetPriorsLDegr(),
                _ => throw new ArgumentOutOfRangeException(nameof(priorIndex), "Invalid prior index")
            };
        }
        public static double GetPriorMean(int priorIndex)
        {
            var (mean, _, _, _) = GetPriorSignature(priorIndex);
            return mean;
        }

        public static double GetPriorSigma(int priorIndex)
        {
            var (_, sigma, _, _) = GetPriorSignature(priorIndex);
            return sigma;
        }

        public static double GetPriorCv(int priorIndex)
        {
            var (mean, sigma, _, _) = GetPriorSignature(priorIndex);
            return sigma / mean;
        }
        public static double GetPriorMin(int priorIndex)
        {
            var (_, _, min, _) = GetPriorSignature(priorIndex);
            return min;
        }

        public static double GetPriorMax(int priorIndex)
        {
            var (_, _, _, max) = GetPriorSignature(priorIndex);
            return max;
        }

    }
}
