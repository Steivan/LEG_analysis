using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace LEG.MeteoSwiss.Abstractions.Models
{
    // ====================================================================
    // 1. Updated Records for Consumption
    // ====================================================================

    public record ForecastPeriod(
        DateTime Time,
        double? DirectRadiationWm2,
        double? DirectNormalIrradianceWm2,
        double? GlobalRadiationWm2, // This is the old SolarRadiationWm2
        double? DiffuseRadiationWm2,
        double? TemperatureC,
        double? WindSpeedKmh,
        double? WindDirectionDeg,
        double? SnowDepthM,
        double? RelativeHumidity,
        double? DewPointC
        )
    {
        // Note: The Open-Meteo API usually returns UTC time. 
        // Your LocalTime logic seems to assume a fixed +1hr shift, 
        // but it's generally safer to use the UtcOffsetSeconds from the response.
        public DateTime LocalTime => Time.AddHours(1);
        public double? WindSpeedMs => WindSpeedKmh.HasValue ? WindSpeedKmh / 3.6 : null;
        public double? SolarRadiationWm2 => GlobalRadiationWm2;
        public double? SnowDepthCm => SnowDepthM * 100;

        // Maps ForecastPeriod to MeteoParameters
        public MeteoParameters ToMeteoParameters()
        {
            return new MeteoParameters(
                Time,
                TimeSpan.FromHours(1),
                null, // SunshineDuration not available
                DirectRadiation: DirectRadiationWm2,
                DirectNormalIrradiance: DirectNormalIrradianceWm2,
                GlobalRadiation: GlobalRadiationWm2,
                DiffuseRadiation: DiffuseRadiationWm2,
                Temperature: TemperatureC,
                WindSpeed: WindSpeedKmh,
                WindDirection: WindDirectionDeg,
                SnowDepth: SnowDepthM,
                RelativeHumidity: RelativeHumidity,
                DewPoint: DewPointC
            );
        }
    }

    public record NowcastPeriod(
        DateTime Time,
        // DirectRadiationWm2 => derived from ShortwaveRadiationWm2 and DiffuseRadiationWm2
        double? DirectNormalIrradianceWm2,
        double? GlobalRadiationWm2, // This is the old SolarRadiationWm2
        double? DiffuseRadiationWm2,
        double? TemperatureC,
        double? WindSpeedKmh,
        double? WindDirectionDeg,
        double? RelativeHumidity,
        double? DewPointC
        // SnowDepthM: Not available in nowcast => set to null
        )
    {
        public DateTime LocalTime => Time.AddHours(1);
        public double? DirectRadiationWm2 => GlobalRadiationWm2.HasValue && DiffuseRadiationWm2.HasValue
            ? GlobalRadiationWm2 - DiffuseRadiationWm2 : null;
        public double? SolarRadiationWm2 => GlobalRadiationWm2;
        public double? WindSpeedMs => WindSpeedKmh.HasValue ? WindSpeedKmh / 3.6 : null;
        public double? SnowDepthM => null;
        public double? SnowDepthCm => null;
        //    public double? WindGustsMs => WindGustsKmh.HasValue ? WindGustsKmh * 3.6 : null;

        // Maps NowcastPeriod to MeteoParameters
        public MeteoParameters ToMeteoParameters()
        {
            return new MeteoParameters(
                Time,
                TimeSpan.FromMinutes(15),
                null, // SunshineDuration not available
                DirectRadiation: DirectRadiationWm2,
                DirectNormalIrradiance: DirectNormalIrradianceWm2,
                GlobalRadiation: GlobalRadiationWm2,
                DiffuseRadiation: DiffuseRadiationWm2,
                Temperature: TemperatureC,
                WindSpeed: WindSpeedKmh,
                WindDirection: WindDirectionDeg,
                SnowDepth: null,
                RelativeHumidity: RelativeHumidity,
                DewPoint: DewPointC
            );
        }
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
        [JsonProperty("wind_speed_10m")] public string? WindSpeed10m { get; set; }
        [JsonProperty("wind_direction_10m")] public string? WindDirection10m { get; set; }
        //[JsonProperty("wind_gusts_10m")] public string? WindGusts10m { get; set; }
        [JsonProperty("direct_radiation")] public string? DirectRadiation { get; set; } // NEW
        [JsonProperty("diffuse_radiation")] public string? DiffuseRadiation { get; set; } // NEW
        [JsonProperty("direct_normal_irradiance")] public string? DirectNormalIrradiance { get; set; } // NEW
        [JsonProperty("shortwave_radiation")] public string? ShortwaveRadiation { get; set; }
        [JsonProperty("snow_depth")] public string? SnowDepth { get; set; } // NEW
    }

    public class HourlyData
    {
        [JsonProperty("time")] public List<string>? Time { get; set; }
        [JsonProperty("temperature_2m")] public List<double?>? Temperature2m { get; set; }
        [JsonProperty("relative_humidity_2m")] public List<double?>? RelativeHumidity2m { get; set; }
        [JsonProperty("dew_point_2m")] public List<double?>? DewPoint2m { get; set; }
        [JsonProperty("wind_speed_10m")] public List<double?>? WindSpeed10m { get; set; }
        [JsonProperty("wind_direction_10m")] public List<double?>? WindDirection10m { get; set; }
        [JsonProperty("direct_radiation")] public List<double?>? DirectRadiation { get; set; } // NEW
        [JsonProperty("diffuse_radiation")] public List<double?>? DiffuseRadiation { get; set; } // NEW
        [JsonProperty("direct_normal_irradiance")] public List<double?>? DirectNormalIrradiance { get; set; } // NEW
        [JsonProperty("shortwave_radiation")] public List<double?>? ShortwaveRadiation { get; set; }
        [JsonProperty("snow_depth")] public List<double?>? SnowDepth { get; set; } // NEW
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
        [JsonProperty("wind_speed_10m")] public List<double?>? WindSpeed10m { get; set; }
        [JsonProperty("wind_direction_10m")] public List<double?>? WindDirection10m { get; set; } // NEW (from URL)
        [JsonProperty("diffuse_radiation_instant")] public List<double?>? DiffuseRadiationInstant { get; set; } // NEW
        [JsonProperty("direct_normal_irradiance_instant")] public List<double?>? DirectNormalIrradianceInstant { get; set; } // NEW
        [JsonProperty("shortwave_radiation_instant")] public List<double?>? ShortwaveRadiationInstant { get; set; }
    }
}

