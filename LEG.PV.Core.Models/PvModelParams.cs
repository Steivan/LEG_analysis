
namespace LEG.PV.Core.Models
{
    public record PvModelParams
    {
        public PvModelParams(double etha, double gamma, double u0, double u1, double lDegr, 
            double ldaAFog = 0.0, double bFog = 1.0, double ldaKFog = 1.95, double ldaDSnow = 1.6)
        {
            Etha = etha;
            Gamma = gamma;
            U0 = u0;
            U1 = u1;
            LDegr = lDegr;
            // Fog and snow parameters with defaults
            LambdaAFog = ldaAFog;
            var zAFog = Math.Exp(-ldaAFog);
            var aFog = 1.0 / (1 + zAFog);
            AFog = aFog;
            PartialAFog = aFog * aFog * zAFog;
            BFog = bFog;
            LambdaKFog = ldaKFog;
            KFog = Math.Exp(ldaKFog);
            LambdaDSnow = ldaDSnow; 
            DSnow = Math.Exp(ldaKFog);
        }

        public double Etha { get; init; }
        public double Gamma { get; init; }
        public double U0 { get; init; }
        public double U1 { get; init; }
        public double LDegr { get; init; }

        // Fog and snow parameters with partial derivatives d X / d ldaX
        public double LambdaAFog { get; init; }
        public double AFog { get; init; }
        public double PartialAFog { get; init; }
        public double BFog { get; init; }
        public double LambdaKFog { get; init; }
        public double KFog { get; init; }
        public double PartialKFog => KFog;
        public double LambdaDSnow { get; init; }
        public double DSnow { get; init; }
        public double PartialDSnow => DSnow;
    }
}