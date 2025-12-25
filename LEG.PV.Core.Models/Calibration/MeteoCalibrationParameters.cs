
using LEG.CoreLib.SampleData.SampleData;

namespace LEG.PV.Core.Models.MeteoCalibrationParameters
{
    public class MeteoCalibrationParameters
    {
        public static Dictionary<string, PvModelParams> PvModelParamsDictionary = new()
            {
                { "Synthetic", new(                                    // Model parameters fo synthetic data
                    etha: 0.9,
                    gamma: -0.005,
                    u0: 25,
                    u1: 0.4,
                    lDegr: 0.01,
                    dSnow: 15.0,
                    lambdaAFog: 0.1,
                    bFog: 0.5,
                    lambdaKFog: 2.0
                ) },
                { ListSites.Senn, new(
                    etha: 0.525,
                    gamma: -0.00665,
                    u0: 200.0,
                    u1: 20.0,
                    lDegr: 0.0127,
                    dSnow: 1.27,
                    lambdaAFog: -0.252,
                    bFog: 0.920,
                    lambdaKFog: 1.03
                ) },
                { ListSites.SennV, new(                                    // SennV: elevation 35° 
                    etha: 0.467,
                    gamma: -0.0,
                    u0: 5.0,
                    u1: 0.001,
                    lDegr: 0.00797,
                    dSnow: 1.09,
                    lambdaAFog: 0.144,
                    bFog: 1.20,
                    lambdaKFog: 0.928
                ) },
                { ListSites.Studenrain, new(                           // SennV: elevation 35° 
                    etha: 0.836,
                    gamma: -0.00,
                    u0: 47.0,
                    u1: 0.491,
                    lDegr: 0.00845,
                    dSnow: 6.74,
                    lambdaAFog: 0.0630,
                    bFog: 2.01,
                    lambdaKFog: 1.97
                ) },
                { "Senn_Initial", new(                          // initial calibration without Snow/Fog
                    etha: 0.619,
                    gamma: -0.00461,
                    u0: 213.7,
                    u1: 0.173,
                    lDegr: 0.0139,
                    dSnow: 15.0,
                    lambdaAFog: 2.0,
                    bFog: 1.0,
                    lambdaKFog: 2.0
                ) },
            { "SennV_Initial", new(
                    etha: 0.478,
                    gamma: -0.00096,
                    u0: 29.0,
                    u1: 0.500,
                    lDegr: 0.00631,
                    dSnow: 2.0,
                    lambdaAFog: 2.0,
                    bFog: 1.0,
                    lambdaKFog: 2.0
                ) },
        };

    }
}
