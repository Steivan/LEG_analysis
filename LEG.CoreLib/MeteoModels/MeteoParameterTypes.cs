using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.CoreLib.MeteoModels
{
    public class MeteoParameterTypes
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
    }
}
