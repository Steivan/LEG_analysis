using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace LEG.MeteoSwiss.Client.Forecast
{
    // ====================================================================
    // 1. Updated Records for Consumption
    // ====================================================================

    public record ForecastPeriod(
        DateTime Time,
        double? TemperatureC,
        double? RelativeHumidity,
        double? DewPointC,
        // New Thermal and Pressure
        double? ApparentTemperatureC,
        double? SurfacePressureHpa,
        // New Clouds
        double? CloudCoverPct,
        double? CloudCoverLowPct,
        double? CloudCoverMidPct,
        double? CloudCoverHighPct,
        // Updated Wind (using WindSpeedKmh from old model, keep it for consistency)
        double? WindSpeedMs,
        double? WindDirectionDeg,
        double? WindGustsMs,
        // New Radiation Fields (CRITICAL for PV model)
        double? DirectRadiationWm2,
        double? DiffuseRadiationWm2,
        double? DirectNormalIrradianceWm2,
        double? ShortwaveRadiationWm2, // This is the old SolarRadiationWm2
        double? TerrestrialRadiationWm2,
        // Updated Precipitation/Weather
        double? PrecipitationMm,
        double? SnowfallCm,
        double? PrecipitationProbabilityPct,
        double? WeatherCode,
        double? SnowDepthCm,
        double? EvapotranspirationMm,
        double? PressureHpa
        )
    {
        // Note: The Open-Meteo API usually returns UTC time. 
        // Your LocalTime logic seems to assume a fixed +1hr shift, 
        // but it's generally safer to use the UtcOffsetSeconds from the response.
        public double? WindSpeedKmh => WindSpeedMs.HasValue ? WindSpeedMs * 3.6 : null;
        public double? WindGustsKmh => WindGustsMs.HasValue ? WindGustsMs * 3.6 : null;
        public DateTime LocalTime => Time.AddHours(1);
    }

    public record NowcastPeriod(
        DateTime Time,
        double? TemperatureC,
        // New Humidity/Clouds
        double? RelativeHumidity,
        double? DewPointC,
        double? CloudCoverPct,
        // Updated Wind
        double? WindSpeedMs,
        double? WindDirectionDeg, // Added
        double? WindGustsMs,
        // New Radiation Fields (CRITICAL for PV model)
        double? DirectNormalIrradianceWm2,
        double? DiffuseRadiationWm2,
        double? SolarRadiationWm2, // This is the old shortwave_radiation_instant
        double? PrecipitationMm
        )
    {
        public double? WindSpeedKmh => WindSpeedMs.HasValue ? WindSpeedMs * 3.6 : null;
        public double? WindGustsKmh => WindGustsMs.HasValue ? WindGustsMs * 3.6 : null;
        public DateTime LocalTime => Time.AddHours(1);
    }

    // ====================================================================
    // 2. JSON Response Classes (Only classes with changes are shown)
    // ====================================================================

    public class ForecastResponse
    {
        [JsonProperty("latitude")] public double Latitude { get; set; }
        [JsonProperty("longitude")] public double Longitude { get; set; }
        [JsonProperty("elevation")] public double Elevation { get; set; }
        [JsonProperty("generationtime_ms")] public double GenerationTimeMs { get; set; }
        [JsonProperty("utc_offset_seconds")] public int UtcOffsetSeconds { get; set; }
        [JsonProperty("timezone")] public string? Timezone { get; set; }
        [JsonProperty("timezone_abbreviation")] public string? TimezoneAbbreviation { get; set; }
        [JsonProperty("hourly_units")] public HourlyUnits? HourlyUnits { get; set; }
        [JsonProperty("hourly")] public HourlyData? Hourly { get; set; }
    }

    public class HourlyUnits
    {
        [JsonProperty("time")] public string? Time { get; set; }
        [JsonProperty("temperature_2m")] public string? Temperature2m { get; set; }
        [JsonProperty("relative_humidity_2m")] public string? RelativeHumidity2m { get; set; }
        [JsonProperty("dew_point_2m")] public string? DewPoint2m { get; set; }
        [JsonProperty("apparent_temperature")] public string? ApparentTemperature { get; set; } // NEW
        [JsonProperty("pressure_msl")] public string? PressureMsl { get; set; }
        [JsonProperty("surface_pressure")] public string? SurfacePressure { get; set; } // NEW
        [JsonProperty("cloud_cover")] public string? CloudCover { get; set; } // NEW
        [JsonProperty("cloud_cover_low")] public string? CloudCoverLow { get; set; } // NEW
        [JsonProperty("cloud_cover_mid")] public string? CloudCoverMid { get; set; } // NEW
        [JsonProperty("cloud_cover_high")] public string? CloudCoverHigh { get; set; } // NEW
        [JsonProperty("wind_speed_10m")] public string? WindSpeed10m { get; set; }
        [JsonProperty("wind_direction_10m")] public string? WindDirection10m { get; set; }
        [JsonProperty("wind_gusts_10m")] public string? WindGusts10m { get; set; }
        [JsonProperty("direct_radiation")] public string? DirectRadiation { get; set; } // NEW
        [JsonProperty("diffuse_radiation")] public string? DiffuseRadiation { get; set; } // NEW
        [JsonProperty("direct_normal_irradiance")] public string? DirectNormalIrradiance { get; set; } // NEW
        [JsonProperty("shortwave_radiation")] public string? ShortwaveRadiation { get; set; }
        [JsonProperty("terrestrial_radiation")] public string? TerrestrialRadiation { get; set; } // NEW
        [JsonProperty("precipitation")] public string? Precipitation { get; set; }
        [JsonProperty("snowfall")] public string? Snowfall { get; set; }
        [JsonProperty("precipitation_probability")] public string? PrecipitationProbability { get; set; } // NEW
        [JsonProperty("weather_code")] public string? WeatherCode { get; set; } // NEW
        [JsonProperty("snow_depth")] public string? SnowDepth { get; set; } // NEW
        [JsonProperty("evapotranspiration")] public string? Evapotranspiration { get; set; }
    }

    public class HourlyData
    {
        [JsonProperty("time")] public List<string>? Time { get; set; }
        [JsonProperty("temperature_2m")] public List<double?>? Temperature2m { get; set; }
        [JsonProperty("relative_humidity_2m")] public List<double?>? RelativeHumidity2m { get; set; }
        [JsonProperty("dew_point_2m")] public List<double?>? DewPoint2m { get; set; }
        [JsonProperty("apparent_temperature")] public List<double?>? ApparentTemperature { get; set; } // NEW
        [JsonProperty("pressure_msl")] public List<double?>? PressureMsl { get; set; }
        [JsonProperty("surface_pressure")] public List<double?>? SurfacePressure { get; set; } // NEW
        [JsonProperty("cloud_cover")] public List<double?>? CloudCover { get; set; } // NEW
        [JsonProperty("cloud_cover_low")] public List<double?>? CloudCoverLow { get; set; } // NEW
        [JsonProperty("cloud_cover_mid")] public List<double?>? CloudCoverMid { get; set; } // NEW
        [JsonProperty("cloud_cover_high")] public List<double?>? CloudCoverHigh { get; set; } // NEW
        [JsonProperty("wind_speed_10m")] public List<double?>? WindSpeed10m { get; set; }
        [JsonProperty("wind_direction_10m")] public List<double?>? WindDirection10m { get; set; }
        [JsonProperty("wind_gusts_10m")] public List<double?>? WindGusts10m { get; set; }
        [JsonProperty("direct_radiation")] public List<double?>? DirectRadiation { get; set; } // NEW
        [JsonProperty("diffuse_radiation")] public List<double?>? DiffuseRadiation { get; set; } // NEW
        [JsonProperty("direct_normal_irradiance")] public List<double?>? DirectNormalIrradiance { get; set; } // NEW
        [JsonProperty("shortwave_radiation")] public List<double?>? ShortwaveRadiation { get; set; }
        [JsonProperty("terrestrial_radiation")] public List<double?>? TerrestrialRadiation { get; set; } // NEW
        [JsonProperty("precipitation")] public List<double?>? Precipitation { get; set; }
        [JsonProperty("snowfall")] public List<double?>? Snowfall { get; set; }
        [JsonProperty("precipitation_probability")] public List<double?>? PrecipitationProbability { get; set; } // NEW
        [JsonProperty("weather_code")] public List<double?>? WeatherCode { get; set; } // NEW
        [JsonProperty("snow_depth")] public List<double?>? SnowDepth { get; set; } // NEW
        [JsonProperty("evapotranspiration")] public List<double?>? Evapotranspiration { get; set; }
    }

    // NowcastResponse remains the same.
    public class NowcastResponse

    {

        [JsonProperty("minutely_1")] public NowcastData? Minutely1 { get; set; }

        [JsonProperty("minutely_10")] public NowcastData? Minutely10 { get; set; }

        [JsonProperty("minutely_15")] public NowcastData? Minutely15 { get; set; }

    }

    // GeocodeResponse/GeocodeResult remain the same.
    public class GeocodeResponse

    {

        [JsonProperty("results")] public List<GeocodeResult>? Results { get; set; }

    }

    public class GeocodeResult

    {

        [JsonProperty("latitude")] public double Latitude { get; set; }

        [JsonProperty("longitude")] public double Longitude { get; set; }

    }

    public class NowcastData
    {
        [JsonProperty("time")] public List<string>? Time { get; set; }
        [JsonProperty("temperature_2m")] public List<double?>? Temperature2m { get; set; }
        [JsonProperty("relative_humidity_2m")] public List<double?>? RelativeHumidity2m { get; set; } // NEW
        [JsonProperty("dew_point_2m")] public List<double?>? DewPoint2m { get; set; } // NEW
        [JsonProperty("precipitation")] public List<double?>? Precipitation { get; set; }
        [JsonProperty("precipitation_probability")] public List<double?>? PrecipitationProbability { get; set; } // NEW (from URL)
        [JsonProperty("cloud_cover")] public List<double?>? CloudCover { get; set; } // NEW (from URL)
        [JsonProperty("wind_speed_10m")] public List<double?>? WindSpeed10m { get; set; }
        [JsonProperty("wind_direction_10m")] public List<double?>? WindDirection10m { get; set; } // NEW (from URL)
        [JsonProperty("wind_gusts_10m")] public List<double?>? WindGusts10m { get; set; }
        [JsonProperty("shortwave_radiation_instant")] public List<double?>? ShortwaveRadiationInstant { get; set; }
        [JsonProperty("direct_normal_irradiance_instant")] public List<double?>? DirectNormalIrradianceInstant { get; set; } // NEW
        [JsonProperty("diffuse_radiation_instant")] public List<double?>? DiffuseRadiationInstant { get; set; } // NEW
    }
}

//namespace LEG.MeteoSwiss.Client.Forecast

//{

//    public record ForecastPeriod(

//        DateTime Time,

//        double? TemperatureC,

//        double? RelativeHumidity,

//        double? DewPointC,

//        double? PrecipitationMm,

//        double? SnowfallCm,

//        double? WindSpeedKmh,

//        double? WindDirectionDeg,

//        double? WindGustsKmh,

//        double? SolarRadiationWm2,

//        double? EvapotranspirationMm,

//        double? PressureHpa

//        )

//    {

//        public DateTime LocalTime => Time.AddHours(1);

//    }



//    public record NowcastPeriod(

//        DateTime Time,

//        double? TemperatureC,

//        double? PrecipitationMm,

//        double? WindSpeedKmh,

//        double? WindGustsKmh,

//        double? SolarRadiationWm2

//        )

//    {

//        public DateTime LocalTime => Time.AddHours(1);

//    }



//    public class GeocodeResponse

//    {

//        [JsonProperty("results")] public List<GeocodeResult>? Results { get; set; }

//    }



//    public class GeocodeResult

//    {

//        [JsonProperty("latitude")] public double Latitude { get; set; }

//        [JsonProperty("longitude")] public double Longitude { get; set; }

//    }



//    public class ForecastResponse

//    {

//        [JsonProperty("latitude")] public double Latitude { get; set; }

//        [JsonProperty("longitude")] public double Longitude { get; set; }

//        [JsonProperty("elevation")] public double Elevation { get; set; }

//        [JsonProperty("generationtime_ms")] public double GenerationTimeMs { get; set; }

//        [JsonProperty("utc_offset_seconds")] public int UtcOffsetSeconds { get; set; }

//        [JsonProperty("timezone")] public string? Timezone { get; set; }

//        [JsonProperty("timezone_abbreviation")] public string? TimezoneAbbreviation { get; set; }

//        [JsonProperty("hourly_units")] public HourlyUnits? HourlyUnits { get; set; }

//        [JsonProperty("hourly")] public HourlyData? Hourly { get; set; }

//    }



//    public class HourlyUnits

//    {

//        [JsonProperty("time")] public string? Time { get; set; }

//        [JsonProperty("temperature_2m")] public string? Temperature2m { get; set; }

//        [JsonProperty("relative_humidity_2m")] public string? RelativeHumidity2m { get; set; }

//        [JsonProperty("dew_point_2m")] public string? DewPoint2m { get; set; }

//        [JsonProperty("precipitation")] public string? Precipitation { get; set; }

//        [JsonProperty("snowfall")] public string? Snowfall { get; set; }

//        [JsonProperty("wind_speed_10m")] public string? WindSpeed10m { get; set; }

//        [JsonProperty("wind_direction_10m")] public string? WindDirection10m { get; set; }

//        [JsonProperty("wind_gusts_10m")] public string? WindGusts10m { get; set; }

//        [JsonProperty("shortwave_radiation")] public string? ShortwaveRadiation { get; set; }

//        [JsonProperty("evapotranspiration")] public string? Evapotranspiration { get; set; }

//        [JsonProperty("pressure_msl")] public string? PressureMsl { get; set; }

//    }



//    public class HourlyData

//    {

//        [JsonProperty("time")] public List<string>? Time { get; set; }

//        [JsonProperty("temperature_2m")] public List<double?>? Temperature2m { get; set; }

//        [JsonProperty("relative_humidity_2m")] public List<double?>? RelativeHumidity2m { get; set; }

//        [JsonProperty("dew_point_2m")] public List<double?>? DewPoint2m { get; set; }

//        [JsonProperty("precipitation")] public List<double?>? Precipitation { get; set; }

//        [JsonProperty("snowfall")] public List<double?>? Snowfall { get; set; }

//        [JsonProperty("wind_speed_10m")] public List<double?>? WindSpeed10m { get; set; }

//        [JsonProperty("wind_direction_10m")] public List<double?>? WindDirection10m { get; set; }

//        [JsonProperty("wind_gusts_10m")] public List<double?>? WindGusts10m { get; set; }

//        [JsonProperty("shortwave_radiation")] public List<double?>? ShortwaveRadiation { get; set; }

//        [JsonProperty("evapotranspiration")] public List<double?>? Evapotranspiration { get; set; }

//        [JsonProperty("pressure_msl")] public List<double?>? PressureMsl { get; set; }

//    }



//    public class NowcastResponse

//    {

//        [JsonProperty("minutely_1")] public NowcastData? Minutely1 { get; set; }

//        [JsonProperty("minutely_10")] public NowcastData? Minutely10 { get; set; }

//        [JsonProperty("minutely_15")] public NowcastData? Minutely15 { get; set; }

//    }



//    public class NowcastData

//    {

//        [JsonProperty("time")] public List<string>? Time { get; set; }

//        [JsonProperty("temperature_2m")] public List<double?>? Temperature2m { get; set; }

//        [JsonProperty("precipitation")] public List<double?>? Precipitation { get; set; }

//        [JsonProperty("wind_speed_10m")] public List<double?>? WindSpeed10m { get; set; }

//        [JsonProperty("wind_gusts_10m")] public List<double?>? WindGusts10m { get; set; }

//        [JsonProperty("shortwave_radiation_instant")] public List<double?>? ShortwaveRadiation { get; set; }

//    }

//}