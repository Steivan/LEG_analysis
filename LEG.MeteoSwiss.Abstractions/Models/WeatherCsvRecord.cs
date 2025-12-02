using CsvHelper.Configuration.Attributes;

namespace LEG.MeteoSwiss.Abstractions.Models
{
    public class WeatherCsvRecord
    {
        /// <summary>
        /// Station abbreviation.
        /// </summary>
        [Name("station_abbr")]
        public string StationAbbr { get; set; } = string.Empty;

        /// <summary>
        /// Reference timestamp of the measurement.
        /// </summary>
        [Name("reference_timestamp"), TypeConverter(typeof(DateTimeCustomFormatConverter))]
        public DateTime ReferenceTimestamp { get; set; }

        /// <summary>
        /// Air temperature 2 m above ground; current value in °C.
        /// </summary>
        [Name("tre200s0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Temperature2m { get; set; }

        /// <summary>
        /// Air temperature at 5 cm above grass; current value in °C.
        /// </summary>
        [Name("tre005s0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Temperature5cm { get; set; }

        /// <summary>
        /// Air temperature at surface; current value in °C.
        /// </summary>
        [Name("tresurs0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? TemperatureSurface { get; set; }

        /// <summary>
        /// Chill temperature; current value in °C.
        /// </summary>
        [Name("xchills0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? WindChill { get; set; }

        /// <summary>
        /// Relative air humidity 2 m above ground; current value in %.
        /// </summary>
        [Name("ure200s0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? RelativeHumidity2m { get; set; }

        /// <summary>
        /// Dew point 2 m above ground; current value in °C.
        /// </summary>
        [Name("tde200s0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? DewPoint2m { get; set; }

        /// <summary>
        /// Vapour pressure 2 m above ground; current value in hPa.
        /// </summary>
        [Name("pva200s0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? VaporPressure2m { get; set; }

        /// <summary>
        /// Atmospheric pressure at barometric altitude (QFE); current value in hPa.
        /// </summary>
        [Name("prestas0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? PressureAtStation { get; set; }

        /// <summary>
        /// Atmospheric pressure reduced to sea level according to standard atmosphere (QNH); current value in hPa.
        /// </summary>
        [Name("pp0qnhs0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? PressureQNH { get; set; }

        /// <summary>
        /// Atmospheric pressure reduced to sea level (QFF); current value in hPa.
        /// </summary>
        [Name("pp0qffs0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? PressureQFF { get; set; }

        /// <summary>
        /// Geopotential height of the 850 hPa level; current value in gpm.
        /// </summary>
        [Name("ppz850s0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? GeopotentialHeight850hPa { get; set; }

        /// <summary>
        /// Geopotential height of the 700 hPa level; current value in gpm.
        /// </summary>
        [Name("ppz700s0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? GeopotentialHeight700hPa { get; set; }

        /// <summary>
        /// Gust peak (one second); maximum in m/s.
        /// </summary>
        [Name("fkl010z1"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? WindGust1s { get; set; }

        /// <summary>
        /// Wind speed vectorial; ten minutes mean in m/s.
        /// </summary>
        [Name("fve010z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? WindSpeedVectorial10min { get; set; }

        /// <summary>
        /// Wind speed scalar; ten minutes mean in m/s.
        /// </summary>
        [Name("fkl010z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? WindSpeedScalar10min { get; set; }

        /// <summary>
        /// Wind direction; ten minutes mean in °.
        /// </summary>
        [Name("dkl010z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? WindDirection { get; set; }

        /// <summary>
        /// Foehn index; Code.
        /// </summary>
        [Name("wcc006s0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? FoehnIndex { get; set; }

        /// <summary>
        /// Wind speed; ten minutes mean in km/h.
        /// </summary>
        [Name("fu3010z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? WindSpeed10min_kmh { get; set; }

        /// <summary>
        /// Gust peak (three seconds); maximum in m/s.
        /// </summary>
        [Name("fkl010z3"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? WindGust3s { get; set; }

        /// <summary>
        /// Gust peak (one second); maximum in km/h.
        /// </summary>
        [Name("fu3010z1"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? WindGust1s_kmh { get; set; }

        /// <summary>
        /// Gust peak (three seconds); maximum in km/h.
        /// </summary>
        [Name("fu3010z3"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? WindGust3s_kmh { get; set; }

        /// <summary>
        /// Precipitation; ten minutes total in mm.
        /// </summary>
        [Name("rre150z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Precipitation { get; set; }

        /// <summary>
        /// Snow depth (automatic measurement); current value in cm.
        /// </summary>
        [Name("htoauts0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? SnowDepth { get; set; }

        /// <summary>
        /// Global radiation; ten minutes mean in W/m².
        /// </summary>
        [Name("gre000z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? ShortWaveRadiation { get; set; }

        /// <summary>
        /// Diffuse radiation; ten minutes mean in W/m².
        /// </summary>
        [Name("ods000z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? DiffuseRadiation { get; set; }

        /// <summary>
        /// Longwave incoming radiation; ten minutes mean in W/m².
        /// </summary>
        [Name("oli000z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? LongwaveRadiationIncoming { get; set; }

        /// <summary>
        /// Longwave outgoing radiation; ten minute mean in W/m².
        /// </summary>
        [Name("olo000z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? LongwaveRadiationOutgoing { get; set; }

        /// <summary>
        /// Shortwave reflected radiation; ten minute mean in W/m².
        /// </summary>
        [Name("osr000z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? ShortwaveRadiationReflected { get; set; }

        /// <summary>
        /// Sunshine duration; ten minutes total in min.
        /// </summary>
        [Name("sre000z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? SunshineDuration { get; set; }

        /// <summary>
        /// Gets or sets the direct radiation in watts per square meter.
        /// </summary>
        [Name("DiRWm2"), TypeConverter(typeof(NullableDoubleConverter))]                             //  Used in Forecast
        public double? DirectRadiation { get; set; }

        /// <summary>
        /// Gets or sets the direct normal irradiance (DNI) in watts per square meter.
        /// </summary>
        [Name("DniWm2"), TypeConverter(typeof(NullableDoubleConverter))]                             //  Used in Forecast
        public double? DirectNormalIrradiance { get; set; }

        /// <summary>
        /// Mapping to MeteoParameters record.
        /// </summary>
        /// <returns></returns>
        public MeteoParameters ToMeteoParameters()
        {
            return new MeteoParameters(
                Time: ReferenceTimestamp,
                Interval: TimeSpan.FromMinutes(10),
                SunshineDuration: SunshineDuration,
                DirectRadiation: DirectRadiation,
                DirectNormalIrradiance: DirectNormalIrradiance,
                GlobalRadiation: ShortWaveRadiation,
                DiffuseRadiation: DiffuseRadiation,
                Temperature: Temperature2m,
                WindSpeed: WindSpeed10min_kmh,
                WindDirection: WindDirection,
                SnowDepth: SnowDepth,
                RelativeHumidity: RelativeHumidity2m,
                DewPoint: DewPoint2m,
                DirectRadiationVariance: null
            );
        }
    }
    
}