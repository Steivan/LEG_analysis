namespace LEG.PV.Core.Models
{
    public class PvPriorConfig
    {
        internal const double meanEthaSys = 0.85;
        internal const double sigmaEthaSys = 0.05;
        internal const double minEthaSys = 0.0;
        internal const double maxEthaSys = 1.0;

        internal const double meanGamma = -0.004;                   // [/°C]
        internal const double sigmaGamma = 0.0005;
        internal const double minGamma = double.MinValue;
        internal const double maxGamma = 0;

        internal const double meanU0 = 29;                          // [W/m^2 K]
        internal const double sigmaU0 = 4;
        internal const double minU0 = 1e-6;
        internal const double maxU0 = double.MaxValue;

        internal const double meanU1 = 0.5;                         // [W/m^2 K per km/h]
        internal const double sigmaU1 = 0.1;
        internal const double minU1 = 1e-6;
        internal const double maxU1 = double.MaxValue;

        internal const double meanLDegr = 0.008;                    // [/year]
        internal const double sigmaLDegr = 0.002;
        internal const double minLDegr = 0.0;
        internal const double maxLDegr = 0.03;

        // Fog and Snow priors
        internal const double meanLambdaAFog = 0.0;
        internal const double sigmaLambdaAFog = 0.85;
        internal const double minLambdaAFog = double.MinValue;
        internal const double maxLambdaAFog = double.MaxValue;

        internal const double meanBFog = 1.0;                       // [/°C]                   
        internal const double sigmaBFog = 0.5;
        internal const double minBFog = double.MinValue;
        internal const double maxBFog = double.MaxValue;

        internal const double meanLambdaKFog = 1.95;
        internal const double sigmaKFog = 0.5;
        internal const double minKFog = double.MinValue;
        internal const double maxKFog = double.MaxValue;

        internal const double meanLambdaDSnow = 1.0;
        internal const double sigmaDSnow = 5.0;
        internal const double minDSnow = double.MinValue;
        internal const double maxDSnow = double.MaxValue;

        public static PvModelParams GetAllPriorsMeans()
        {
            return new PvModelParams(meanEthaSys, meanGamma, meanU0, meanU1, meanLDegr, meanLambdaAFog, meanBFog, meanLambdaKFog, meanLambdaDSnow);
        }
        public static PvModelParams GetAllPriorsSigmas()
        {
            return new PvModelParams(sigmaEthaSys, sigmaGamma, sigmaU0, sigmaU1, sigmaLDegr, sigmaLambdaAFog, sigmaBFog, sigmaKFog, sigmaDSnow);
        }
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

        // Fog and Snow priors

        public static (double mean, double sigma, double min, double max) GetPriorsLambdaAFog()
        {
            return (meanLambdaAFog, sigmaLambdaAFog, minLambdaAFog, maxLambdaAFog);
        }

        public static (double mean, double sigma, double min, double max) GetPriorsBFog()
        {
            return (meanBFog, sigmaBFog, minBFog, maxBFog);
        }

        public static (double mean, double sigma, double min, double max) GetPriorsKFog()
        {
            return (meanLambdaKFog, sigmaKFog, minKFog, maxKFog);
        }

        public static (double mean, double sigma, double min, double max) GetPriorsDSnow()
        {
            return (meanLambdaDSnow, sigmaDSnow, minDSnow, maxDSnow);
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
                5 => GetPriorsLambdaAFog(),
                6 => GetPriorsBFog(),
                7 => GetPriorsKFog(),
                8 => GetPriorsDSnow(),
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
            return mean != 0 ? Math.Abs(sigma / mean) : double.NaN;
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
