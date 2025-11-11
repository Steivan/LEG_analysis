using System.Collections.Concurrent;
using Newtonsoft.Json;

// Browser url:
// https://api.open-meteo.com/v1/forecast?latitude=47.38&longitude=8.54&minutely_15=temperature_2m,precipitation,wind_speed_10m,wind_gusts_10m,shortwave_radiation_instant&forecast_minutely_15=360&timezone=Europe%2FZurich

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

        public async Task<ForecastResponse> Get7DayForecastAsync(string zipCode)
        {
            var (lat, lon) = await GetLatLonAsync(zipCode);
            return await Get7DayForecastAsync(lat, lon);
        }

        public async Task<ForecastResponse> Get7DayForecastAsync(double lat, double lon)
        {
            string key = $"7day|{lat:F4},{lon:F4}";
            if (Cache.TryGetValue(key, out var c) && DateTime.UtcNow < c.Expires)
                return (ForecastResponse)c.Response;

            var url = $"{ForecastBaseUrl}?latitude={lat:F4}&longitude={lon:F4}" +
                      "&hourly=temperature_2m,relative_humidity_2m,dew_point_2m,precipitation,snowfall," +
                      "wind_speed_10m,wind_direction_10m,wind_gusts_10m,shortwave_radiation,evapotranspiration,pressure_msl" +
                      "&timezone=Europe%2FZurich";

            var json = await _httpClient.GetStringAsync(url);
            var resp = JsonConvert.DeserializeObject<ForecastResponse>(json)!;
            Cache[key] = (DateTime.UtcNow.Add(HourlyCacheDuration), resp);
            return resp;
        }

        public async Task<List<NowcastPeriod>> GetNowcast15MinuteAsync(string zipCode)
        {
            var (lat, lon) = await GetLatLonAsync(zipCode);
            return await GetNowcast15MinuteAsync(lat, lon);
        }

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

            var url = $"{ForecastBaseUrl}?latitude={lat:F4}&longitude={lon:F4}" +
                      "&minutely_15=temperature_2m,precipitation,wind_speed_10m,wind_gusts_10m,shortwave_radiation_instant" +
                      "&forecast_minutely_15=360&timezone=Europe%2FZurich";

            var json = await client.GetStringAsync(url);
            var freshResponse = JsonConvert.DeserializeObject<NowcastResponse>(json)!;
            var result = ConvertToNowcastPeriods(freshResponse);

            if (result.Count > 0)
            {
                Cache[cacheKey] = (DateTime.UtcNow.Add(NowcastCacheDuration), freshResponse);
            }

            return result;
        }

        private async Task<(double lat, double lon)> GetLatLonAsync(string zip)
        {
            var url = $"{GeocodeBaseUrl}?name={zip}&count=1&language=en&format=json&country=CH";
            var json = await _httpClient.GetStringAsync(url);
            var geo = JsonConvert.DeserializeObject<GeocodeResponse>(json)!;
            if (geo.Results?.Count is null or 0) throw new Exception($"ZIP {zip} not found");
            return (geo.Results[0].Latitude, geo.Results[0].Longitude);
        }

        public async Task<List<ForecastPeriod>> Get7DayPeriodsAsync(string zipCode)
            => ConvertToForecastPeriods(await Get7DayForecastAsync(zipCode));

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
                    PrecipitationMm: Get(r.Hourly.Precipitation, i),
                    SnowfallCm: Get(r.Hourly.Snowfall, i),
                    WindSpeedKmh: Get(r.Hourly.WindSpeed10m, i),
                    WindDirectionDeg: Get(r.Hourly.WindDirection10m, i),
                    WindGustsKmh: Get(r.Hourly.WindGusts10m, i),
                    SolarRadiationWm2: Get(r.Hourly.ShortwaveRadiation, i),
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
                    PrecipitationMm: Get(data.Precipitation, i),
                    WindSpeedKmh: Get(data.WindSpeed10m, i),
                    WindGustsKmh: Get(data.WindGusts10m, i),
                    SolarRadiationWm2: Get(data.ShortwaveRadiation, i)
                ));
            }
            return list;
        }

        private static double? Get(List<double?>? list, int i) =>
            list is { Count: > 0 } && i >= 0 && i < list.Count ? list[i] : null;

        public void Dispose() => _httpClient.Dispose();
    }
}