using static LEG.PV.Core.Models.PvModelParamsMetaData;

namespace LEG.PV.Core.Models
{

    public record PvModelParams
    {


        public PvModelParams(double etha, double gamma, double u0, double u1, double lDegr,
            double dSnow = PvPriorConfig.meanDSnow,
            double lambdaAFog = PvPriorConfig.meanLambdaAFog, double bFog = PvPriorConfig.meanBFog, double lambdaKFog = PvPriorConfig.meanLambdaKFog)
        {
            Etha = etha;
            Gamma = gamma;
            U0 = u0;
            U1 = u1;
            LDegr = lDegr;
            // Snow and fog parameters with defaults
            var zAFog = Math.Exp(-lambdaAFog);
            var aFog = 1.0 / (1 + zAFog);
            var kFog = Math.Exp(lambdaKFog);
            DSnow = dSnow;
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
        public double DSnow { get; init; }
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
                IndexDSnow => ($"{DSnowName}", DSnow),
                IndexLambdaAFog => useLambda ? ($"{LambdaAFogName}", LambdaAFog) : ($"{AFogName}", AFog),
                IndexBFog => ($"{BFogName}", BFog),
                IndexLambdaKFog => useLambda ? ($"{LambdaKFogName}", LambdaKFog) : ($"{KFogName}", KFog),
                _ => throw new ArgumentOutOfRangeException(nameof(index), "Invalid index")
            };
        }

        public bool IsNan()
        {
            return double.IsNaN(Etha) || double.IsNaN(Gamma) || double.IsNaN(U0) || double.IsNaN(U1) || double.IsNaN(LDegr) ||
                   double.IsNaN(DSnow) || double.IsNaN(AFog) || double.IsNaN(BFog) || double.IsNaN(KFog);
        }
    }
}