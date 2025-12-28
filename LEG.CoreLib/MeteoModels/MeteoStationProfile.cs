using LEG.CoreLib.Abstractions.ReferenceData;
using LEG.MeteoSwiss.Abstractions.Models;
using static LEG.CoreLib.MeteoModels.StationNamesList;
using static LEG.MeteoSwiss.Abstractions.ReferenceData.MeteoStations;

namespace LEG.CoreLib.MeteoModels
{
    public class MeteoStationProfile : IProfileToStationDictionary
    {
        public Dictionary<string, Dictionary<string, WeightMeteoParameters>> ProfileToStationDictionary { get; }

        public MeteoStationProfile()
        {
            ProfileToStationDictionary = new Dictionary<string, Dictionary<string, WeightMeteoParameters>>
            {
           // Key: profile name (e.g., "ZurichGroup", "BernGroup")
            { MaurGroup, new Dictionary<string, WeightMeteoParameters>
                {
                    { SMA, new WeightMeteoParameters { Weights = ThreeOneWeights } },
                    { KLO, new WeightMeteoParameters { Weights = OneOneWeights } },
                    { HOE, new WeightMeteoParameters { Weights = OneZeroOneWeights } },   // Radiation and SnowDept
                    { UEB, new WeightMeteoParameters { Weights = OneZeroWeights } }       // Radiation only
                }
            },
            { BinzGroup, new Dictionary<string, WeightMeteoParameters>
                {
                    { SMA, new WeightMeteoParameters { Weights = ThreeOneWeights } },
                    { HOE, new WeightMeteoParameters { Weights = OneZeroOneWeights } },   // Radiation and SnowDepth
                    { UEB, new WeightMeteoParameters { Weights = OneZeroWeights } }       // Radiation only
                }
            }
            // Add more profiles as needed
            };
        }

        private static Dictionary<MeteoParameterType, double> OneZeroWeights = new Dictionary<MeteoParameterType, double>
        {
            { MeteoParameterType.SunshineDuration, 1.0 },
            { MeteoParameterType.DirectRadiation, 1.0 },
            { MeteoParameterType.DirectNormalIrradiance, 1.0 },
            { MeteoParameterType.GlobalRadiation, 1.0 },
            { MeteoParameterType.DiffuseRadiation, 1.0 },
            { MeteoParameterType.Temperature, 0.0 },
            { MeteoParameterType.WindSpeed, 0.0 },
            { MeteoParameterType.WindDirection, 0.0 },
            { MeteoParameterType.SnowDepth, 0.0 },
            { MeteoParameterType.RelativeHumidity, 0.0 },
            { MeteoParameterType.DewPoint, 0.0 },
            { MeteoParameterType.RadiationVariance, 1.0 }
        };

        private static Dictionary<MeteoParameterType, double> OneZeroOneWeights = new Dictionary<MeteoParameterType, double>
        {
            { MeteoParameterType.SunshineDuration, 1.0 },
            { MeteoParameterType.DirectRadiation, 1.0 },
            { MeteoParameterType.DirectNormalIrradiance, 1.0 },
            { MeteoParameterType.GlobalRadiation, 1.0 },
            { MeteoParameterType.DiffuseRadiation, 1.0 },
            { MeteoParameterType.Temperature, 0.0 },
            { MeteoParameterType.WindSpeed, 0.0 },
            { MeteoParameterType.WindDirection, 0.0 },
            { MeteoParameterType.SnowDepth, 1.0 },
            { MeteoParameterType.RelativeHumidity, 0.0 },
            { MeteoParameterType.DewPoint, 0.0 },
            { MeteoParameterType.RadiationVariance, 1.0 }
        };

        private static Dictionary<MeteoParameterType, double> OneOneWeights = new Dictionary<MeteoParameterType, double>
        {
            { MeteoParameterType.SunshineDuration, 1.0 },
            { MeteoParameterType.DirectRadiation, 1.0 },
            { MeteoParameterType.DirectNormalIrradiance, 1.0 },
            { MeteoParameterType.GlobalRadiation, 1.0 },
            { MeteoParameterType.DiffuseRadiation, 1.0 },
            { MeteoParameterType.Temperature, 1.0 },
            { MeteoParameterType.WindSpeed, 1.0 },
            { MeteoParameterType.WindDirection, 1.0 },
            { MeteoParameterType.SnowDepth, 1.0 },
            { MeteoParameterType.RelativeHumidity, 1.0 },
            { MeteoParameterType.DewPoint, 1.0 },
            { MeteoParameterType.RadiationVariance, 1.0 }
        };

        private static Dictionary<MeteoParameterType, double> ThreeOneWeights = new Dictionary<MeteoParameterType, double>
        {
            { MeteoParameterType.SunshineDuration, 3.0 },
            { MeteoParameterType.DirectRadiation, 3.0 },
            { MeteoParameterType.DirectNormalIrradiance, 3.0 },
            { MeteoParameterType.GlobalRadiation, 3.0 },
            { MeteoParameterType.DiffuseRadiation, 3.0 },
            { MeteoParameterType.Temperature, 1.0 },
            { MeteoParameterType.WindSpeed, 1.0 },
            { MeteoParameterType.WindDirection, 1.0 },
            { MeteoParameterType.SnowDepth, 1.0 },
            { MeteoParameterType.RelativeHumidity, 1.0 },
            { MeteoParameterType.DewPoint, 1.0 },
            { MeteoParameterType.RadiationVariance, 1.0 }
        };

    }
}
