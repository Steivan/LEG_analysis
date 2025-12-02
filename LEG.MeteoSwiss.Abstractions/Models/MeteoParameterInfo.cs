namespace LEG.MeteoSwiss.Abstractions.Models
{
    public static class MeteoParameterInfo
    {
        public static readonly Dictionary<string, string> ParameterToUnit = new()
            {
            { nameof(MeteoParameters.SunshineDuration), "min" },
            { nameof(MeteoParameters.DirectRadiation), "W/m²" },
            { nameof(MeteoParameters.DirectNormalIrradiance), "W/m²" },
            { nameof(MeteoParameters.GlobalRadiation), "W/m²" },
            { nameof(MeteoParameters.DiffuseRadiation), "W/m²" },
            { nameof(MeteoParameters.Temperature), "°C" },
            { nameof(MeteoParameters.WindSpeed), "km/h" }, // or "m/s"
            { nameof(MeteoParameters.WindDirection), "°" },
            { nameof(MeteoParameters.SnowDepth), "cm" },
            { nameof(MeteoParameters.RelativeHumidity), "%" },
            { nameof(MeteoParameters.DewPoint), "°C" },
            { nameof(MeteoParameters.DirectRadiationVariance), "(W/m²)²" }
        };
    }
}
