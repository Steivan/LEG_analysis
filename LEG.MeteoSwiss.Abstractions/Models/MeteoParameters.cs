namespace LEG.MeteoSwiss.Abstractions.Models
{
    public record MeteoParameters(
        DateTime Time,
        TimeSpan Interval,
        double? SunshineDuration,
        double? DirectRadiation,
        double? DirectNormalIrradiance,
        double? GlobalRadiation,
        double? DiffuseRadiation,
        double? Temperature,
        double? WindSpeed,
        double? WindDirection,
        double? SnowDepth,
        double? RelativeHumidity,
        double? DewPoint,
        double? DirectRadiationVariance = null, // Optional for history/forecast
        IntervalAnchor Anchor = IntervalAnchor.End // Default to End
    )
    {
        public double? GlobalHRWm2 =>
            GlobalRadiation.HasValue ? GlobalRadiation.Value
            : (DirectRadiation.HasValue && DiffuseRadiation.HasValue)
                ? DirectRadiation.Value + DiffuseRadiation.Value
                : (double?)null;

        public double? SnowDepthCm =>
            SnowDepth.HasValue ? SnowDepth.Value * 100.0 : (double?)null;
    }

    public record StationMeteoData(string StationId, List<MeteoParameters> WeatherData);
}