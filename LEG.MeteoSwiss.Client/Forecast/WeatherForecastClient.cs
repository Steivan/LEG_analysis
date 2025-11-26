using LEG.MeteoSwiss.Client.MeteoSwiss;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Globalization;

// Documentation
// https://open-meteo.com/en/docs
// Browser url:
// https://api.open-meteo.com/v1/forecast?latitude=47.38&longitude=8.54&minutely_15=temperature_2m,precipitation,wind_speed_10m,wind_gusts_10m,shortwave_radiation_instant&forecast_minutely_15=360&timezone=UTC

namespace LEG.MeteoSwiss.Client.Forecast
{
    public class WeatherForecastClient : IDisposable
    {
        private readonly HttpClient _httpClient = new();
        private const string GeocodeBaseUrl = "https://geocoding-api.open-meteo.com/v1/search";
        private const string ForecastBaseUrl = "https://api.open-meteo.com/v1/forecast";

        private static readonly ConcurrentDictionary<string, (DateTime Expires, object Response)> Cache = new();
        private static readonly TimeSpan HourlyCacheDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan NowcastCacheDuration = TimeSpan.FromMinutes(5);

        // 10-Day Forecast using ECMWF model
        // =======================================================
        public async Task<ForecastResponse> Get16DayForecastAsync(string zipCode)
        {
            var (lat, lon) = await GetLatLonAsync(zipCode);
            return await Get16DayForecastAsync(lat, lon);
        }

        public async Task<ForecastResponse> Get16DayForecastAsync(double lat, double lon)
        {
            string key = $"16day|{lat:F4},{lon:F4}";
            if (Cache.TryGetValue(key, out var c) && DateTime.UtcNow < c.Expires)
                return (ForecastResponse)c.Response;

            // URL 1: Long-Term Base (ECMWF, up to 10 days)
            var url = string.Format(
            CultureInfo.InvariantCulture,
            "{0}?latitude={1:F4}&longitude={2:F4}" +
            "&hourly=temperature_2m,relative_humidity_2m,dew_point_2m," + 
            "wind_speed_10m,wind_direction_10m," +
            "direct_radiation,diffuse_radiation,direct_normal_irradiance,shortwave_radiation" +
            "&models=ecmwf_ifs" + // ECMWF model for long horizon
            "&forecast_days=16" + // Requesting max forecast length
            "&timezone=UTC",
            ForecastBaseUrl, lat, lon);

            var json = await _httpClient.GetStringAsync(url);
            var resp = JsonConvert.DeserializeObject<ForecastResponse>(json)!;
            Cache[key] = (DateTime.UtcNow.Add(HourlyCacheDuration), resp);
            return resp;
        }

        public async Task<ForecastResponse> Get16DayForecastByZipCodeAsync(string zipCode)
        {
            var (lat, lon) = await GetLatLonAsync(zipCode);
            return await Get16DayForecastAsync(lat, lon);
        }

        public async Task<ForecastResponse> Get16DayForecastByStationIdAsync(string stationId)
        {
            var (lat, lon) = GetStationLatLon(stationId);
            return await Get16DayForecastAsync(lat, lon);
        }

        public async Task<List<ForecastPeriod>> Get16DayPeriodsAsync(double lat, double lon)
            => ConvertToForecastPeriods(await Get16DayForecastAsync(lat, lon));

        public async Task<List<ForecastPeriod>> Get16DayPeriodsByZipCodeAsync(string zipCode)
            => ConvertToForecastPeriods(await Get16DayForecastByZipCodeAsync(zipCode));

        public async Task<List<ForecastPeriod>> Get16DayPeriodsByStationIdAsync(string stationId)
            => ConvertToForecastPeriods(await Get16DayForecastByStationIdAsync(stationId));

        // 7-Day Forecast using ICON-D2 model
        // =======================================================
        public async Task<ForecastResponse> Get7DayForecastAsync(double lat, double lon)
        {
            string key = $"7day|{lat:F4},{lon:F4}";
            if (Cache.TryGetValue(key, out var c) && DateTime.UtcNow < c.Expires)
                return (ForecastResponse)c.Response;

            // URL 2: Mid-Term High-Res (ICON-D2, up to ~3 days)
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}?latitude={1:F4}&longitude={2:F4}" +
                "&hourly=temperature_2m,relative_humidity_2m,dew_point_2m," +
                "wind_speed_10m,wind_direction_10m," +
                "direct_radiation,diffuse_radiation,direct_normal_irradiance,shortwave_radiation," + // <-- ADDED COMMA HERE
                "snow_depth" + // This is now correctly separated by the preceding comma
                "&models=icon_d2" +
                "&forecast_days=3" +
                "&timezone=UTC",
                ForecastBaseUrl, lat, lon);

            var json = await _httpClient.GetStringAsync(url);
            var resp = JsonConvert.DeserializeObject<ForecastResponse>(json)!;
            Cache[key] = (DateTime.UtcNow.Add(HourlyCacheDuration), resp);
            return resp;
        }

        public async Task<ForecastResponse> Get7DayForecastByZipCodeAsync(string zipCode)
        {
            var (lat, lon) = await GetLatLonAsync(zipCode);
            return await Get7DayForecastAsync(lat, lon);
        }

        public async Task<ForecastResponse> Get7DayForecastByStationIdAsync(string stationId)
        {
            var (lat, lon) = GetStationLatLon(stationId);
            return await Get7DayForecastAsync(lat, lon);
        }

        public async Task<List<ForecastPeriod>> Get7DayPeriodsAsync(double lat, double lon)
            => ConvertToForecastPeriods(await Get7DayForecastAsync(lat, lon));

        public async Task<List<ForecastPeriod>> Get7DayPeriodsByZipCodeAsync(string zipCode)
            => ConvertToForecastPeriods(await Get7DayForecastByZipCodeAsync(zipCode));

        public async Task<List<ForecastPeriod>> Get7DayPeriodsByStationIdAsync(string stationId)
            => ConvertToForecastPeriods(await Get7DayForecastByStationIdAsync(stationId));

        // 6-Hour Forecast (Nowcast) using ICON-D2 model
        // =======================================================
        public async Task<List<NowcastPeriod>> GetNowcast15MinuteAsync(double lat, double lon)
        {
            string cacheKey = $"nowcast15|{lat:F4},{lon:F4}";

            if (Cache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.Expires)
            {
                var cachedResponse = (NowcastResponse)cached.Response;
                var list = ConvertToNowcastPeriods(cachedResponse);
                if (list.Count > 0) return list;
            }

            using var handler = new HttpClientHandler();
            using var client = new HttpClient(handler);

            // URL 3: Short-Term Nearcast (ICON-D2, 15-min)
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}?latitude={1:F4}&longitude={2:F4}" +
                "&minutely_15=temperature_2m,relative_humidity_2m,dew_point_2m," + 
                "wind_speed_10m,wind_direction_10m," +
                "direct_normal_irradiance_instant,diffuse_radiation_instant,shortwave_radiation_instant" +
                "&forecast_minutely_15=360" + // Requesting 360 records of 15-min data (90 hours)
                "&models=icon_d2" +
                "&timezone=UTC",
                ForecastBaseUrl, lat, lon);        // &daily=weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset,uv_index_max

            var json = await client.GetStringAsync(url);
            var freshResponse = JsonConvert.DeserializeObject<NowcastResponse>(json)!;
            var result = ConvertToNowcastPeriods(freshResponse);

            if (result.Count > 0)
            {
                Cache[cacheKey] = (DateTime.UtcNow.Add(NowcastCacheDuration), freshResponse);
            }

            return result;
        }

        public async Task<List<NowcastPeriod>> GetNowcast15MinuteByZipCodeAsync(string zipCode)
        {
            var (lat, lon) = await GetLatLonAsync(zipCode);
            return await GetNowcast15MinuteAsync(lat, lon);
        }

        public async Task<List<NowcastPeriod>> GetNowcast15MinuteByStationIdAsync(string stationId)
        {
            var (lat, lon) = GetStationLatLon(stationId);
            return await GetNowcast15MinuteAsync(lat, lon);
        }

        // Helper Methods
        // =======================================================

        // Helper method to get latitude and longitude from ZIP code
        private async Task<(double lat, double lon)> GetLatLonAsync(string zip)
        {
            var url = $"{GeocodeBaseUrl}?name={zip}&count=1&language=en&format=json&country=CH";
            var json = await _httpClient.GetStringAsync(url);
            var geo = JsonConvert.DeserializeObject<GeocodeResponse>(json)!;
            if (geo.Results?.Count is null or 0) throw new Exception($"ZIP {zip} not found");

            return (geo.Results[0].Latitude, geo.Results[0].Longitude);
        }

        // Helper method to get station latitude and longitude from station ID
        public static (double lat, double lon) GetStationLatLon(string stationId)
        {
            // Initialize list with valid ground stations
            MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetAllGroundStations();

            // Load station metadata
            var groundStationsMetaDict = StationMetaImporter.Import(MeteoSwissConstants.GroundStationsMetaFile);
            var stationMeta = groundStationsMetaDict[stationId];

            var lat = stationMeta.StationCoordinatesWgs84Lat ?? throw new Exception($"Station ID {stationId} not found");
            var lon = stationMeta.StationCoordinatesWgs84Lon ?? throw new Exception($"Station ID {stationId} not found");

            return (lat, lon);

        }
            // Assuming the Get helper method and other necessary helper methods (like DateTime.Parse) 
            // are available and unchanged in the containing class.

            private static List<ForecastPeriod> ConvertToForecastPeriods(ForecastResponse r)
        {
            if (r.Hourly?.Time is not { Count: > 0 }) return new();
            var list = new List<ForecastPeriod>(r.Hourly.Time.Count);
            for (int i = 0; i < r.Hourly.Time.Count; i++)
            {
                var t = DateTime.Parse(r.Hourly.Time[i]);
                list.Add(new ForecastPeriod(
                    Time: t,
                    TemperatureC: Get(r.Hourly.Temperature2m, i),
                    RelativeHumidity: Get(r.Hourly.RelativeHumidity2m, i),
                    DewPointC: Get(r.Hourly.DewPoint2m, i),
                    // New Thermal and Pressure
                    ApparentTemperatureC: Get(r.Hourly.ApparentTemperature, i),
                    SurfacePressureHpa: Get(r.Hourly.SurfacePressure, i),
                    // New Clouds
                    CloudCoverPct: Get(r.Hourly.CloudCover, i),
                    CloudCoverLowPct: Get(r.Hourly.CloudCoverLow, i),
                    CloudCoverMidPct: Get(r.Hourly.CloudCoverMid, i),
                    CloudCoverHighPct: Get(r.Hourly.CloudCoverHigh, i),
                    // Wind
                    WindSpeedMs: Get(r.Hourly.WindSpeed10m, i),
                    WindDirectionDeg: Get(r.Hourly.WindDirection10m, i),
                    WindGustsMs: Get(r.Hourly.WindGusts10m, i),
                    // New Radiation Fields (Mapped to Direct and Diffuse)
                    DirectRadiationWm2: Get(r.Hourly.DirectRadiation, i),
                    DiffuseRadiationWm2: Get(r.Hourly.DiffuseRadiation, i),
                    DirectNormalIrradianceWm2: Get(r.Hourly.DirectNormalIrradiance, i),
                    ShortwaveRadiationWm2: Get(r.Hourly.ShortwaveRadiation, i),
                    TerrestrialRadiationWm2: Get(r.Hourly.TerrestrialRadiation, i),
                    // Precipitation/Weather
                    PrecipitationMm: Get(r.Hourly.Precipitation, i),
                    SnowfallCm: Get(r.Hourly.Snowfall, i),
                    PrecipitationProbabilityPct: Get(r.Hourly.PrecipitationProbability, i),
                    WeatherCode: Get(r.Hourly.WeatherCode, i),
                    SnowDepthCm: Get(r.Hourly.SnowDepth, i),
                    EvapotranspirationMm: Get(r.Hourly.Evapotranspiration, i),
                    PressureHpa: Get(r.Hourly.PressureMsl, i)
                ));
            }
            return list;
        }

        private static List<NowcastPeriod> ConvertToNowcastPeriods(NowcastResponse r)
        {
            var data = r.Minutely15;
            if (data?.Time == null || data.Time.Count == 0) return new();
            var list = new List<NowcastPeriod>(data.Time.Count);
            for (int i = 0; i < data.Time.Count; i++)
            {
                var t = DateTime.Parse(data.Time[i]);
                list.Add(new NowcastPeriod(
                    Time: t,
                    TemperatureC: Get(data.Temperature2m, i),
                    // New Humidity/Clouds
                    RelativeHumidity: Get(data.RelativeHumidity2m, i),
                    DewPointC: Get(data.DewPoint2m, i),
                    CloudCoverPct: Get(data.CloudCover, i),
                    // Wind
                    WindSpeedMs: Get(data.WindSpeed10m, i),
                    WindDirectionDeg: Get(data.WindDirection10m, i),
                    WindGustsMs: Get(data.WindGusts10m, i),
                    // New Radiation Fields (Mapped to DNI and Diffuse)
                    DirectNormalIrradianceWm2: Get(data.DirectNormalIrradianceInstant, i),
                    DiffuseRadiationWm2: Get(data.DiffuseRadiationInstant, i),
                    SolarRadiationWm2: Get(data.ShortwaveRadiationInstant, i),
                    // Precipitation
                    PrecipitationMm: Get(data.Precipitation, i)
                ));
            }
            return list;
        }

        private static double? Get(List<double?>? list, int i) =>
            list is { Count: > 0 } && i >= 0 && i < list.Count ? list[i] : null;

        public void Dispose() => _httpClient.Dispose();
    }
}