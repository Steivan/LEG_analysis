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
        public double GetDirectPoa(bool hasDirectIrradiance, double sinSunElevation)
        {
            if (!hasDirectIrradiance)
                return 0.0;

            var directHorizontalRadiation = Math.Max(0, GlobalRadiation.Value - DiffuseRadiation.Value);

            return directHorizontalRadiation / sinSunElevation;
        }

        public double GetDiffusePoa(bool hasDiffuseIrradiance)
        {
            return hasDiffuseIrradiance ? DiffuseRadiation.Value : 0.0;
        }

        public double DewPointFromRH(double temperature, double relativeHumidity)
        {
            // Magnus formula for dew point approximation
            const double a = 17.62;     // 17.27;
            const double b = 243.12;    // 237.7; // degrees Celsius

            double alpha = a * temperature / (b + temperature) + Math.Log(relativeHumidity / 100.0);

            return (b * alpha) / (a - alpha);
        }
        public double GetDewPoint(double defaultT = 15.0, double defaultRH = 60.0)
        {
            // Measured value
            if (DewPoint.HasValue)
                return DewPoint.Value;

            // Magnus formula for dew point approximation
            var temperature = Temperature ?? defaultT;
            var relativeHumidity = RelativeHumidity ?? defaultRH;

            return DewPointFromRH(temperature, relativeHumidity);
        }

        public double GetDewPointDepression(double defaultT = 15.0, double defaultRH = 60.0)
        {
            var temperature = Temperature ?? defaultT;

            return temperature - GetDewPoint(temperature, defaultRH);
        }

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