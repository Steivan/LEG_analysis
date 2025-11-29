using System;

namespace LEG.MeteoSwiss.Abstractions.Models
{
    public enum IntervalAnchor
    {
        Start,
        End,
        Midpoint
    }
    public record UnifiedWeatherData
    (
        DateTime Timestamp,
        TimeSpan Interval,
        string StationId,
        MeteoParameterType Parameter,
        double? Value,
        WeatherDataSource Source,
        IntervalAnchor Anchor = IntervalAnchor.End // Default to End
    );

    public enum MeteoParameterType
    {
        SunshineDuration,
        DirectRadiation,
        DirectNormalIrradiance,
        GlobalRadiation,
        DiffuseRadiation,
        Temperature,
        WindSpeed,
        WindDirection,
        SnowDepth,
        RelativeHumidity,
        DewPoint
        // Add more as needed
    }
}