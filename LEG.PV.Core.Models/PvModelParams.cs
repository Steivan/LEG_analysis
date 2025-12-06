
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

        internal const int PvModelParamsCount = 9;

        public PvModelParams(double etha, double gamma, double u0, double u1, double lDegr,
            double ldaDSnow = PvPriorConfig.meanLambdaDSnow,
            double ldaAFog = PvPriorConfig.meanLambdaAFog, double bFog = PvPriorConfig.meanBFog, double ldaKFog = PvPriorConfig.meanLambdaKFog)
        {
            Etha = etha;
            Gamma = gamma;
            U0 = u0;
            U1 = u1;
            LDegr = lDegr;
            // Snow and fog parameters with defaults
            LambdaDSnow = ldaDSnow;
            DSnow = Math.Exp(ldaKFog);
            LambdaAFog = ldaAFog;
            var zAFog = Math.Exp(-ldaAFog);
            var aFog = 1.0 / (1 + zAFog);
            AFog = aFog;
            PartialAFog = aFog * aFog * zAFog;
            BFog = bFog;
            LambdaKFog = ldaKFog;
            KFog = Math.Exp(ldaKFog);
        }

        public double Etha { get; init; }
        public double Gamma { get; init; }
        public double U0 { get; init; }
        public double U1 { get; init; }
        public double LDegr { get; init; }

        // Snow and fog parameters with partial derivatives d X / d ldaX
        public double LambdaDSnow { get; init; }
        public double DSnow { get; init; }
        public double PartialDSnow => DSnow;
        public double LambdaAFog { get; init; }
        public double AFog { get; init; }
        public double PartialAFog { get; init; }
        public double BFog { get; init; }
        public double LambdaKFog { get; init; }
        public double KFog { get; init; }
        public double PartialKFog => KFog;
    }
}