
using LEG.CoreLib.SampleData.SampleData;

namespace LEG.PV.Core.Models.MeteoCalibrationParameters
{
    public class MeteoCalibrationParameters
    {
        public static Dictionary<string, PvModelParams> PvModelParamsDictionary = new()
            {
                {  SiteNamesList.SyntheticSite, new(                                    // Model parameters fo synthetic data
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
                { SiteNamesList.Senn, new(
                    etha: 0.333,
                    gamma: -0.00278,
                    u0: 29.6,
                    u1: 0.379,
                    lDegr: 0.0129,
                    dSnow: 16.7,
                    lambdaAFog: 0.378,
                    bFog: 1.04,
                    lambdaKFog: 0.833
                ) },
                { SiteNamesList.SennV, new( 
                    etha: 0.567,      // Calibrated value is too low
                    gamma: -0.0176,
                    u0: 200.0,
                    u1: 0.001,
                    lDegr: 0.0144,
                    dSnow: 56.2,
                    lambdaAFog: -0.660,
                    bFog: 0.922,
                    lambdaKFog: 1.78
                ) },
                { SiteNamesList.Studenrain, new(
                    etha: 0.834,
                    gamma: -0.00,
                    u0: 5.0,
                    u1: 0.001,
                    lDegr: 0.0001,
                    dSnow: 12.8,
                    lambdaAFog: -0.904,
                    bFog: 1.89,
                    lambdaKFog: 2.10
                ) },
        };

    }
}
