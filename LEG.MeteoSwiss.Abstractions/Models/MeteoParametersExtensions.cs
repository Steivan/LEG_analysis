namespace LEG.MeteoSwiss.Abstractions.Models
{
    public static class MeteoParametersExtensions
    {
        public static double? GetValue(this MeteoParameters parameters, MeteoParameterType type)
        {
            return type switch
            {
                MeteoParameterType.SunshineDuration => parameters.SunshineDuration,
                MeteoParameterType.DirectRadiation => parameters.DirectRadiation,
                MeteoParameterType.DirectNormalIrradiance => parameters.DirectNormalIrradiance,
                MeteoParameterType.GlobalRadiation => parameters.GlobalRadiation,
                MeteoParameterType.DiffuseRadiation => parameters.DiffuseRadiation,
                MeteoParameterType.Temperature => parameters.Temperature,
                MeteoParameterType.WindSpeed => parameters.WindSpeed,
                MeteoParameterType.WindDirection => parameters.WindDirection,
                MeteoParameterType.SnowDepth => parameters.SnowDepth,
                MeteoParameterType.RelativeHumidity => parameters.RelativeHumidity,
                MeteoParameterType.DewPoint => parameters.DewPoint,
                _ => null
            };
        }
    }
}