using LEG.PV.Core.Models.Structures;
using static LEG.PV.Core.Models.Structures.PvModelParamsMetaData;

namespace LEG.PV.Core.Models.PvProductionModel
{
    public class PvPriorConfig
    {
        internal const double meanEthaSys = 0.85;
        internal const double sigmaEthaSys = 0.05;
        internal const double minEthaSys = 0.01;
        internal const double maxEthaSys = 10.0;

        internal const double meanGamma = -0.004;                   // [/°C]
        internal const double sigmaGamma = 0.0005;
        internal const double minGamma = -0.1;
        internal const double maxGamma = -1e-6;

        internal const double meanU0 = 29;                          // [W/m^2 K]
        internal const double sigmaU0 = 4;
        internal const double minU0 = 5.0;
        internal const double maxU0 = 200.0;

        internal const double meanU1 = 0.5;                         // [W/m^2 K per km/h]
        internal const double sigmaU1 = 0.1;
        internal const double minU1 = 0.001;
        internal const double maxU1 = 20.0;

        internal const double meanLDegr = 0.008;                    // [/year]
        internal const double sigmaLDegr = 0.002;
        internal const double minLDegr = 0.0001;
        internal const double maxLDegr = 0.1;

        // Snow and fog priors

        internal const double meanDSnow = 15.0;
        internal const double sigmaDSnow = 5.0;
        internal const double minDSnow = 1.0;
        internal const double maxDSnow = 100.0;

        internal const double meanLambdaAFog = 2.0;
        internal const double sigmaLambdaAFog = 0.85;
        internal const double minLambdaAFog = -10.0;
        internal const double maxLambdaAFog = 10.0;

        internal const double meanBFog = 1.0;                       // [/°C]                   
        internal const double sigmaBFog = 0.5;
        internal const double minBFog = -5.0;
        internal const double maxBFog = 5.0;

        internal const double meanLambdaKFog = 1.95;
        internal const double sigmaLambdaKFog = 0.5;
        internal const double minLambdaKFog = -10.0;
        internal const double maxLambdaKFog = 10.0;

        public static PvModelParams GetAllPriorsMeans()
        {
            return new PvModelParams(meanEthaSys, meanGamma, meanU0, meanU1, meanLDegr, meanDSnow, meanLambdaAFog, meanBFog, meanLambdaKFog);
        }
        public static PvModelParams GetAllPriorsSigmas()
        {
            return new PvModelParams(sigmaEthaSys, sigmaGamma, sigmaU0, sigmaU1, sigmaLDegr, sigmaDSnow, sigmaLambdaAFog, sigmaBFog, sigmaLambdaKFog);
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

        // Snow and fog priors
        public static (double mean, double sigma, double min, double max) GetPriorsDSnow()
        {
            return (meanDSnow, sigmaDSnow, minDSnow, maxDSnow);
        }

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
            return (meanLambdaKFog, sigmaLambdaKFog, minLambdaKFog, maxLambdaKFog);
        }

        public static (double mean, double sigma, double min, double max) GetPriorSignature(int priorIndex)
        {
            return (priorIndex % PvModelParamsCount) switch
            {
                IndexEtha => GetPriorsEtha(),
                IndexGamma => GetPriorsGamma(),
                IndexU0 => GetPriorsU0(),
                IndexU1 => GetPriorsU1(),
                IndexLDegr => GetPriorsLDegr(),
                IndexDSnow => GetPriorsDSnow(),
                IndexLambdaAFog => GetPriorsLambdaAFog(),
                IndexBFog => GetPriorsBFog(),
                IndexLambdaKFog => GetPriorsKFog(),
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
