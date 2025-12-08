
namespace LEG.PV.Core.Models
{

    public record PvModelParams
    {
        internal const int IndexEtha = 0;
        internal const int IndexGamma = 1;
        internal const int IndexU0 = 2;
        internal const int IndexU1 = 3;
        internal const int IndexLDegr = 4;
        internal const int IndexLambdaDSnow = 5;
        internal const int IndexLambdaAFog = 6;
        internal const int IndexBFog = 7;
        internal const int IndexLambdaKFog = 8;

        public const int PvModelParamsCount = 9;

        public const string EthaName = "Etha";
        public const string GammaName = "Gamma";
        public const string U0Name = "U0";
        public const string U1Name = "U1";
        public const string LDegrName = "LDegr";
        public const string DSnowName = "DSnow";
        public const string LambdaDSnowName = "LambdaDSnow";
        public const string AFogName = "AFog";
        public const string LambdaAFogName = "LambdaAFog";
        public const string BFogName = "BFog";
        public const string KFogName = "KFog";
        public const string LambdaKFogName = "LambdaKFog";

        public PvModelParams(double etha, double gamma, double u0, double u1, double lDegr,
            double lambdaDSnow = PvPriorConfig.meanLambdaDSnow,
            double lambdaAFog = PvPriorConfig.meanLambdaAFog, double bFog = PvPriorConfig.meanBFog, double lambdaKFog = PvPriorConfig.meanLambdaKFog)
        {
            Etha = etha;
            Gamma = gamma;
            U0 = u0;
            U1 = u1;
            LDegr = lDegr;
            // Snow and fog parameters with defaults
            var dSnow = Math.Exp(lambdaDSnow);
            var zAFog = Math.Exp(-lambdaAFog);
            var aFog = 1.0 / (1 + zAFog);
            var kFog = Math.Exp(lambdaKFog);
            LambdaDSnow = lambdaDSnow;
            DSnow = dSnow;
            PartialDSnow = dSnow;
            LambdaAFog = lambdaAFog;
            AFog = aFog;
            PartialLambdaAFog = zAFog * aFog * aFog;
            BFog = bFog;
            LambdaKFog = lambdaKFog;
            KFog = kFog;
            PartialLambdaKFog = kFog;
        }

        public double Etha { get; init; }
        public double Gamma { get; init; }
        public double U0 { get; init; }
        public double U1 { get; init; }
        public double LDegr { get; init; }

        // Snow and fog parameters with partial derivatives d X / d ldaX
        public double LambdaDSnow { get; init; }
        public double DSnow { get; init; }
        public double PartialDSnow;
        public double LambdaAFog { get; init; }
        public double AFog { get; init; }
        public double PartialLambdaAFog { get; init; }
        public double BFog { get; init; }
        public double LambdaKFog { get; init; }
        public double KFog { get; init; }
        public double PartialLambdaKFog;

        public (string Name, double Value) GetNameAndValue(int index, bool useLambda = false)
        {
            return index switch
            {
                IndexEtha => ($"{EthaName}", Etha),
                IndexGamma => ($"{GammaName}", Gamma),
                IndexU0 => ($"{U0Name}", U0),
                IndexU1 => ($"{U1Name}", U1),
                IndexLDegr => ($"{LDegrName}", LDegr),
                IndexLambdaDSnow => useLambda ? ($"{LambdaDSnowName}", LambdaDSnow) : ($"{DSnowName}", DSnow),
                IndexLambdaAFog => useLambda ? ($"{LambdaAFogName}", LambdaAFog) : ($"{AFogName}", AFog),
                IndexBFog => ($"{BFogName}", BFog),
                IndexLambdaKFog => useLambda ? ($"{LambdaKFogName}", LambdaKFog) : ($"{KFogName}", KFog),
                _ => throw new ArgumentOutOfRangeException(nameof(index), "Invalid index")
            };
        }
    }
}