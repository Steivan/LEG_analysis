using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.CoreLib.SampleData.ReferenceData
{
    public class MeteoStationProfile
    {

        public static List<MeteoParameterType> MeteoParameterTypeList { get; set; } = new()
        {
            MeteoParameterType.SunshineDuration,
            MeteoParameterType.DirectRadiation,
            MeteoParameterType.DirectNormalIrradiance,
            MeteoParameterType.GlobalRadiation,
            MeteoParameterType.DiffuseRadiation,
            MeteoParameterType.Temperature,
            MeteoParameterType.WindSpeed,
            MeteoParameterType.WindDirection,
            MeteoParameterType.SnowDepth,
            MeteoParameterType.RelativeHumidity,
            MeteoParameterType.DewPoint
        };

        public static readonly Dictionary<MeteoParameterType, bool> ParameterIsAdditive = new()
        {
            { MeteoParameterType.SunshineDuration, true },
            { MeteoParameterType.DirectRadiation, true },
            { MeteoParameterType.DirectNormalIrradiance, true },
            { MeteoParameterType.GlobalRadiation, true },
            { MeteoParameterType.DiffuseRadiation, true },
            { MeteoParameterType.Temperature, false },
            { MeteoParameterType.WindSpeed, false },
            { MeteoParameterType.WindDirection, false },
            { MeteoParameterType.SnowDepth, false },
            { MeteoParameterType.RelativeHumidity, false },
            { MeteoParameterType.DewPoint, false },
        };

        //public Dictionary<MeteoParameterType, double?[]> MeteoValuesArrays { get; set; } = new();
        //public Dictionary<MeteoParameterType, double[]> WeightMeteoArrays { get; set; } = new();
        //public Dictionary<MeteoParameterType, double[]> WeightedSumMeteoValuesArrays { get; set; } = new();

        public static Dictionary<MeteoParameterType, double> OneZeroWeights = new Dictionary<MeteoParameterType, double>
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

        public static Dictionary<MeteoParameterType, double> OneZeroOneWeights = new Dictionary<MeteoParameterType, double>
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

        public static Dictionary<MeteoParameterType, double> OneOneWeights = new Dictionary<MeteoParameterType, double>
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

        public static Dictionary<MeteoParameterType, double> ThreeOneWeights = new Dictionary<MeteoParameterType, double>
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

        public static readonly Dictionary<string, Dictionary<string, WeightMeteoParameters>> ProfileToStationDictionary = new()
        {
            // Key: profile name (e.g., "ZurichGroup", "BernGroup")
            { "MaurGroup", new Dictionary<string, WeightMeteoParameters>
                {
                { "SMA", new WeightMeteoParameters { Weights = ThreeOneWeights } },
                { "KLO", new WeightMeteoParameters { Weights = OneOneWeights } },
                { "HOE", new WeightMeteoParameters { Weights = OneZeroOneWeights } },   // Radiation and SnowDept
                { "UEB", new WeightMeteoParameters { Weights = OneZeroWeights } }       // Radiation only
                }
            },
            { "BinzGroup", new Dictionary<string, WeightMeteoParameters>
                {
                { "SMA", new WeightMeteoParameters { Weights = ThreeOneWeights } },
                { "HOE", new WeightMeteoParameters { Weights = OneZeroOneWeights } },   // Radiation and SnowDepth
                { "UEB", new WeightMeteoParameters { Weights = OneZeroWeights } }       // Radiation only
                }
            }
            // Add more profiles as needed
        };

        public static readonly Dictionary<int, string> SiteToProfilesDictionary = new()
        {
            // Key: site/folder id, Value: profile name
            { 1, "MaurGroup" },
            { 2, "MaurGroup" },
            { 3, "BinzGroup" },
            // etc.
        };

        public static List<string> SelectedStationsIdList = new List<string>();

    }
}
