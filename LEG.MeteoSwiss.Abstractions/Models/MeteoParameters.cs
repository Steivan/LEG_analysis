using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.MeteoSwiss.Abstractions.Models
{
    public class MeteoParameterTypes
    {
        public static double DPFromTAndRH(double temperature, double relativeHumidity)
        {
            // Magnus formula for dew point approximation
            const double a = 17.62;     // 17.27;
            const double b = 243.12;    // 237.7; // degrees Celsius
            relativeHumidity = Math.Max(Math.Min(relativeHumidity, 100.0), 1.0); // Clamp RH to avoid log(0)
            double alpha = a * temperature / (b + temperature) + Math.Log(relativeHumidity / 100.0);

            return alpha * b / (a - alpha);
        }
        public static double RHFromTAndDP(double temperature, double dewPoint)
        {
            // Magnus formula for dew point approximation
            const double a = 17.62;
            const double b = 243.12;
            dewPoint = Math.Min(temperature, dewPoint);
            double beta = a * dewPoint / (b + dewPoint) - a * temperature / (b + temperature);

            return 100.0 * Math.Exp(beta);
        }
        public static double TFromRHAndDP(double relativeHumidity, double dewPoint)
        {
            // Rearranged Magnus formula to get T from DP and RH
            const double a = 17.62;
            const double b = 243.12;
            relativeHumidity = Math.Max(Math.Min(relativeHumidity, 100.0), 1.0); // Clamp RH to avoid log(0)
            double gamma = a * dewPoint / (b + dewPoint) - Math.Log(relativeHumidity / 100.0);

            return gamma * b / (a - gamma);
        }
        public record MeteoParameters
        {
            public MeteoParameters
            (
                DateTime time,
                TimeSpan interval,
                double? sunshineDuration,
                double? directRadiation,
                double? directNormalIrradiance,
                double? globalRadiation,
                double? diffuseRadiation,
                double? temperature,
                double? windSpeed,
                double? windDirection,
                double? snowDepth,
                double? relativeHumidity,
                double? dewPoint,
                double? radiationVariance = null, // Optional for history/forecast
                IntervalAnchor anchor = IntervalAnchor.End // Default to End
            )
            {
                Time = time;
                Interval = interval;
                SunshineDuration = sunshineDuration;
                DirectNormalIrradiance = directNormalIrradiance;
                (DirectRadiation, DiffuseRadiation, GlobalRadiation) = Get_Dr_Df_G_Radiation(directRadiation, diffuseRadiation, globalRadiation);
                (Temperature, DewPoint, RelativeHumidity) = Get_T_DP_RH(temperature, dewPoint, relativeHumidity);
                WindSpeed = windSpeed;
                WindDirection = windDirection;
                SnowDepth = Math.Max(snowDepth?? 0.0, 0.0);
                RadiationVariance = radiationVariance;
                Anchor = anchor;
            }
            public DateTime Time { get; init; }
            public TimeSpan Interval { get; init; }
            public double? SunshineDuration { get; init; }
            public double? DirectRadiation { get; init; }
            public double? DirectNormalIrradiance { get; init; }
            public double? GlobalRadiation { get; init; }
            public double? DiffuseRadiation { get; init; }
            public double? Temperature { get; init; }
            public double? WindSpeed { get; init; }
            public double? WindDirection { get; init; }
            public double? SnowDepth { get; init; }
            public double? RelativeHumidity { get; init; }
            public double? DewPoint { get; init; }
            public double? RadiationVariance { get; init; } = null;     // Optional: Used when blending multiple sources
            public IntervalAnchor Anchor { get; init; } = IntervalAnchor.End; // Default to End

            private (double? Dr, double? Df, double? G) Get_Dr_Df_G_Radiation(double? directRadiation, double? diffuseRadiation, double? globalRadiation)
            {
                double? Dr = directRadiation;
                double? Df = diffuseRadiation;
                double? G = globalRadiation;
                if (G.HasValue && !(Dr.HasValue && Df.HasValue))            // G is known, but Dr or Df is missing
                {
                    if (Dr.HasValue)
                    {
                        Df = G.Value - Dr.Value;
                    }
                    else if (Df.HasValue)
                    {
                        Dr = G.Value - Df.Value;
                    }
                    else
                    {
                        // Assume clear sky with 80% direct radiation
                        Dr = 0.8 * G.Value;
                        Df = 0.2 * G.Value;
                    }
                }
                else if (!G.HasValue && (Dr.HasValue || Df.HasValue))       // G is missing, but Dr or Df is known
                {
                    if (Dr.HasValue && !Df.HasValue)
                    {
                        Df = Dr.Value / 4.0;
                    }
                    else if (!Dr.HasValue && Df.HasValue)
                    {
                        Dr = Df.Value * 4.0;
                    }
                    G = Dr.Value + Df.Value;
                }

                return (Dr, Df, G);
            }
            private (double? T, double? DP, double? RH) Get_T_DP_RH(double? temperature, double? dewPoint, double? relativeHumidity)
            {
                double? T = temperature;
                double? DP = dewPoint;
                double? RH = relativeHumidity;
                if (T.HasValue && DP.HasValue && !RH.HasValue)
                {
                    RH = RHFromTAndDP(T.Value, DP.Value);
                }
                else if (T.HasValue && !DP.HasValue && RH.HasValue)
                {
                    DP = DPFromTAndRH(T.Value, RH.Value);
                }
                else if (!T.HasValue && DP.HasValue && RH.HasValue)
                {
                    T = TFromRHAndDP(RH.Value, DP.Value);
                }

                return (T, DP, RH);
            }
            public double? ValueFromType(MeteoParameterType parameterType)
            {
                return parameterType switch
                {
                    MeteoParameterType.SunshineDuration => SunshineDuration ?? null,
                    MeteoParameterType.DirectRadiation => DirectRadiation ?? null,
                    MeteoParameterType.DirectNormalIrradiance => DirectNormalIrradiance ?? null,
                    MeteoParameterType.GlobalRadiation => GlobalRadiation ?? null,
                    MeteoParameterType.DiffuseRadiation => DiffuseRadiation ?? null,
                    MeteoParameterType.Temperature => Temperature ?? null,
                    MeteoParameterType.WindSpeed => WindSpeed ?? null,
                    MeteoParameterType.WindDirection => WindDirection ?? null,
                    MeteoParameterType.SnowDepth => SnowDepth ?? null,
                    MeteoParameterType.RelativeHumidity => RelativeHumidity ?? null,
                    MeteoParameterType.DewPoint => DewPoint ?? null,
                    _ => null,
                };
            }
            public (double weightR, double weightS, double weightF) GetWeightsRSW(double sinSunElevation)
            {
                const double gammaR = 1.0 / 10.0;           // [1/(W/m2)] for Global Radiation
                const double gammaS = 1.0 / 0.5;            // [1/(cm)] for Snow Depth
                const double gammaF = 1.0 / 0.5;            // [1/(°C)] for Dew Point
                double conjugateWeight(double gamma, double x) => x <= 0 ? 1.0 : 2.0 / (1.0 + Math.Exp(gamma * x));

                // Decompose data into GRTW, S and F
                var nonRadiation = conjugateWeight(gammaR, sinSunElevation > 0 ? sinSunElevation * 1000.0 : 0.0);
                var nonSnow = conjugateWeight(gammaS, SnowDepth ?? 0.0);
                var nonFog = 1.0 - conjugateWeight(gammaF, GetDewPointDepression());

                var radiation = (1.0 - nonRadiation);
                var weightRadiation = radiation * nonSnow * nonFog;
                var weightSnow = radiation * (1.0 - nonSnow);
                var weightFog = radiation * nonSnow * (1.0 - nonFog);
                // Residual = 1.0 - weightRadiation - weightSnow - weigtFog = nonRadiation =>  "nighttime" records 

                if (SnowDepth.Value > 2.0 && sinSunElevation > 0)
                {
                    // DEBUG chexkpoint
                }

                return (weightRadiation, weightSnow, weightFog);
            }
            public double GetDirectPoa(bool hasDirectIrradiance, double sinSunElevation)
            {
                if (!hasDirectIrradiance || sinSunElevation <= 0.0)
                    return 0.0;

                var directHorizontalRadiation = Math.Max(0, GlobalRadiation.Value - DiffuseRadiation.Value);

                return directHorizontalRadiation / sinSunElevation;
            }
            public double GetDiffusePoa(bool hasDiffuseIrradiance)
            {
                return hasDiffuseIrradiance ? DiffuseRadiation.Value : 0.0;
            }
            public double GetDewPoint(double defaultT = 15.0, double defaultRH = 60.0)
            {
                // Measured value
                if (DewPoint.HasValue)
                    return DewPoint.Value;

                // Magnus formula for dew point approximation
                var temperature = Temperature ?? defaultT;
                var relativeHumidity = RelativeHumidity ?? defaultRH;

                return DPFromTAndRH(temperature, relativeHumidity);
            }
            public double GetDewPointDepression(double defaultT = 15.0, double defaultRH = 60.0)
            {
                var temperature = Temperature ?? defaultT;

                return temperature - GetDewPoint(temperature, defaultRH);
            }
            public ValidMeteoParameters GetValidMeteoParameters()
            {
                return new ValidMeteoParameters
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
                    HasValidRadiationVariance = RadiationVariance.HasValue
                };
            }
        }
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
        public bool HasValidRadiationVariance { init; get; }
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
        public double WeightRadiationVariance { get; init; } = 0.0;
    }
}