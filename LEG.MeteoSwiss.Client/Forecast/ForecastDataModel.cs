using Newtonsoft.Json;

namespace LEG.MeteoSwiss.Client.Forecast
{
    public record ForecastPeriod(
        DateTime Time, 
        double? TemperatureC, 
        double? RelativeHumidity, 
        double? DewPointC,
        double? PrecipitationMm, 
        double? SnowfallCm, 
        double? WindSpeedKmh, 
        double? WindDirectionDeg,
        double? WindGustsKmh, 
        double? SolarRadiationWm2, 
        double? EvapotranspirationMm, 
        double? PressureHpa
        )
    { 
        public DateTime LocalTime => Time.AddHours(1);
    }

    public record NowcastPeriod(
        DateTime Time, 
        double? TemperatureC,
        double? PrecipitationMm,
        double? WindSpeedKmh, 
        double? WindGustsKmh, 
        double? SolarRadiationWm2
        )
    { 
        public DateTime LocalTime => Time.AddHours(1); 
    }

    public class GeocodeResponse
    {
        [JsonProperty("results")] public List<GeocodeResult>? Results { get; set; }
    }

    public class GeocodeResult
    {
        [JsonProperty("latitude")] public double Latitude { get; set; }
        [JsonProperty("longitude")] public double Longitude { get; set; }
    }

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
        [JsonProperty("precipitation")] public string? Precipitation { get; set; }
        [JsonProperty("snowfall")] public string? Snowfall { get; set; }
        [JsonProperty("wind_speed_10m")] public string? WindSpeed10m { get; set; }
        [JsonProperty("wind_direction_10m")] public string? WindDirection10m { get; set; }
        [JsonProperty("wind_gusts_10m")] public string? WindGusts10m { get; set; }
        [JsonProperty("shortwave_radiation")] public string? ShortwaveRadiation { get; set; }
        [JsonProperty("evapotranspiration")] public string? Evapotranspiration { get; set; }
        [JsonProperty("pressure_msl")] public string? PressureMsl { get; set; }
    }

    public class HourlyData
    {
        [JsonProperty("time")] public List<string>? Time { get; set; }
        [JsonProperty("temperature_2m")] public List<double?>? Temperature2m { get; set; }
        [JsonProperty("relative_humidity_2m")] public List<double?>? RelativeHumidity2m { get; set; }
        [JsonProperty("dew_point_2m")] public List<double?>? DewPoint2m { get; set; }
        [JsonProperty("precipitation")] public List<double?>? Precipitation { get; set; }
        [JsonProperty("snowfall")] public List<double?>? Snowfall { get; set; }
        [JsonProperty("wind_speed_10m")] public List<double?>? WindSpeed10m { get; set; }
        [JsonProperty("wind_direction_10m")] public List<double?>? WindDirection10m { get; set; }
        [JsonProperty("wind_gusts_10m")] public List<double?>? WindGusts10m { get; set; }
        [JsonProperty("shortwave_radiation")] public List<double?>? ShortwaveRadiation { get; set; }
        [JsonProperty("evapotranspiration")] public List<double?>? Evapotranspiration { get; set; }
        [JsonProperty("pressure_msl")] public List<double?>? PressureMsl { get; set; }
    }

    public class NowcastResponse
    {
        [JsonProperty("minutely_1")] public NowcastData? Minutely1 { get; set; }
        [JsonProperty("minutely_10")] public NowcastData? Minutely10 { get; set; }
        [JsonProperty("minutely_15")] public NowcastData? Minutely15 { get; set; }
    }

    public class NowcastData
    {
        [JsonProperty("time")] public List<string>? Time { get; set; }
        [JsonProperty("temperature_2m")] public List<double?>? Temperature2m { get; set; }
        [JsonProperty("precipitation")] public List<double?>? Precipitation { get; set; }
        [JsonProperty("wind_speed_10m")] public List<double?>? WindSpeed10m { get; set; }
        [JsonProperty("wind_gusts_10m")] public List<double?>? WindGusts10m { get; set; }
        [JsonProperty("shortwave_radiation_instant")] public List<double?>? ShortwaveRadiation { get; set; }
    }
}