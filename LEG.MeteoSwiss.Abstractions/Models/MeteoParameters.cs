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

        public double? SnowDepthCm => SnowDepth.HasValue ? SnowDepth.Value * 100.0 : (double?)null;

        public ValidMeteoParameters GetValidMeteoParameters = new()
        {
            HasValidSunshineDuration = SunshineDuration.HasValue,
            HasValidDirectRadiation = DirectRadiation.HasValue,
            HasValidDirectNormalIrradiance = DirectNormalIrradiance.HasValue,
            HasValidGlobalRadiation = GlobalRadiation.HasValue,
            HasValidDiffuseRadiation = DiffuseRadiation.HasValue,
            HasValidTemperature = Temperature.HasValue,
            HasValidWindSpeed = WindSpeed.HasValue,
            HasValidWindDirection = WindDirection.HasValue,
            HasValidSnowDepth = SnowDepth.HasValue,
            HasValidRelativeHumidity = RelativeHumidity.HasValue,
            HasValidDewPoint = DewPoint.HasValue,
            HasValidDirectRadiationVariance = DirectRadiationVariance.HasValue
        };
    }

    public record StationMeteoData(string StationId, List<MeteoParameters> WeatherData);

    public record ValidMeteoParameters
    {
        public bool HasValidSunshineDuration { init; get; }
        public bool HasValidDirectRadiation { init; get; }
        public bool HasValidDirectNormalIrradiance { init; get; }
        public bool HasValidGlobalRadiation { init; get; }
        public bool HasValidDiffuseRadiation { init; get; }
        public bool HasValidTemperature { init; get; }
        public bool HasValidWindSpeed { init; get; }
        public bool HasValidWindDirection { init; get; }
        public bool HasValidSnowDepth { init; get; }
        public bool HasValidRelativeHumidity { init; get; }
        public bool HasValidDewPoint { init; get; }
        public bool HasValidDirectRadiationVariance { init; get; }
    }
    public record WeightMeteoParameters
    {
        public double WeightSunshineDuration { get; init; } = 0.0;
        public double WeightDirectRadiation { get; init; } = 0.0;
        public double WeightDirectNormalIrradiance { get; init; } = 0.0;
        public double WeightGlobalRadiation { get; init; } = 0.0;
        public double WeightDiffuseRadiation { get; init; } = 0.0;
        public double WeightTemperature { get; init; } = 0.0;
        public double WeightWindSpeed { get; init; } = 0.0;
        public double WeightWindDirection { get; init; } = 0.0;
        public double WeightSnowDepth { get; init; } = 0.0;
        public double WeightRelativeHumidity { get; init; } = 0.0;
        public double WeightDewPoint { get; init; } = 0.0;
        public double WeightDirectRadiationVariance { get; init; } = 0.0;
    }
}