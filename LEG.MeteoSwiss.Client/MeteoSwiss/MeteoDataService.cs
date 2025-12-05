using LEG.MeteoSwiss.Abstractions;
using LEG.MeteoSwiss.Abstractions.Models;
using LEG.MeteoSwiss.Client.MeteoSwiss.Parsers;

namespace LEG.MeteoSwiss.Client.MeteoSwiss
{
    public class MeteoDataService(IMeteoSwissClient? meteoSwissClient = null, HttpClient? openMeteoHttpClient = null) : IMeteoDataService
    {
        private readonly HttpClient _openMeteoHttpClient = openMeteoHttpClient ?? new HttpClient();
        private readonly IMeteoSwissClient _meteoSwissClient = meteoSwissClient ?? new MeteoSwissClient();
        private bool _disposed;
        private readonly bool _isClientInjected = meteoSwissClient != null;

        public async Task<List<WeatherData>> GetHistoricalWeatherAsync(string startDate, string endDate, string stationId, string granularity = "t")
        {
            if (string.IsNullOrWhiteSpace(startDate))
                throw new ArgumentException("startDate must not be null or empty.", nameof(startDate));
            if (string.IsNullOrWhiteSpace(endDate))
                throw new ArgumentException("endDate must not be null or empty.", nameof(endDate));
            if (string.IsNullOrWhiteSpace(stationId))
                throw new ArgumentException("stationId must not be null or empty.", nameof(stationId));

            if (!DateTime.TryParse(startDate, out DateTime start))
                throw new ArgumentException("startDate is not a valid date/time string.", nameof(startDate));
            if (!DateTime.TryParse(endDate, out DateTime end))
                throw new ArgumentException("endDate is not a valid date/time string.", nameof(endDate));
            if (end < start)
                throw new ArgumentException("endDate must be after startDate.");

            var allWeatherData = new List<WeatherData>();

            try
            {
                foreach (char granChar in granularity.Distinct())
                {
                    string singleGranularity = granChar.ToString();
                    Console.WriteLine($"--- Fetching data for granularity: {singleGranularity} ---");

                    var csvRows = await _meteoSwissClient.GetHistoricalDataAsync(startDate, endDate, stationId, singleGranularity);

                    if (csvRows != null && csvRows.Length > 1)
                    {
                        var header = csvRows.FirstOrDefault(row => row != null && row.Trim().StartsWith("station_abbr"));
                        if (header != null)
                        {
                            var dataRows = csvRows.Where(row => row != null && !row.Trim().StartsWith("station_abbr")).ToList();
                            var cleanedCsvRows = new List<string> { header };
                            cleanedCsvRows.AddRange(dataRows);

                            var weatherData = CsvParser.ParseHistoricalCsv([.. cleanedCsvRows]);
                            allWeatherData.AddRange(weatherData);
                        }
                    }
                }

                return [.. allWeatherData.OrderBy(d => d.Timestamp)];
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve or parse historical weather data.", ex);
            }
        }
        public async Task<List<WeatherData>> GetOpenMeteoHistoricalAsync(double latitude, double longitude, string startDate, string endDate)
        {
            if (latitude < -90 || latitude > 90)
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
            if (longitude < -180 || longitude > 180)
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
            if (string.IsNullOrWhiteSpace(startDate))
                throw new ArgumentException("startDate must not be null or empty.", nameof(startDate));
            if (string.IsNullOrWhiteSpace(endDate))
                throw new ArgumentException("endDate must not be null or empty.", nameof(endDate));

            if (!DateTime.TryParse(startDate, out DateTime start))
                throw new ArgumentException("startDate is not a valid date/time string.", nameof(startDate));
            if (!DateTime.TryParse(endDate, out DateTime end))
                throw new ArgumentException("endDate is not a valid date/time string.", nameof(endDate));
            if (end < start)
                throw new ArgumentException("endDate must be after startDate.");

            try
            {
                var url = $"https://archive-api.open-meteo.com/v1/archive?latitude={latitude}&longitude={longitude}&start_date={startDate}&end_date={endDate}&hourly=temperature_2m,global_tilted_irradiance_instant";
                var response = await _openMeteoHttpClient.GetStringAsync(url);
                dynamic? jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                var weatherData = new List<WeatherData>();

                if (jsonResponse is not null &&
                    jsonResponse.hourly is not null &&
                    jsonResponse.hourly.time is not null &&
                    jsonResponse.hourly.temperature_2m is not null &&
                    jsonResponse.hourly.global_tilted_irradiance_instant is not null)
                {
                    int count = (int)jsonResponse.hourly.time.Count;
                    for (int i = 0; i < count; i++)
                    {
                        weatherData.Add(new WeatherData
                        {
                            Timestamp = DateTime.Parse((string)jsonResponse.hourly.time[i]),
                            temperature_2m = (double?)jsonResponse.hourly.temperature_2m[i],
                            global_radiation = (double?)jsonResponse.hourly.global_tilted_irradiance_instant[i]
                        });
                    }
                }
                return weatherData;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve or parse Open-Meteo historical data.", ex);
            }
        }

        public async Task<byte[]> GetShortTermForecastRawAsync(double[] bbox)
        {
            if (bbox == null || bbox.Length != 4)
                throw new ArgumentException("bbox must be an array of 4 elements: [minLon, minLat, maxLon, maxLat].", nameof(bbox));

            try
            {
                return await _meteoSwissClient.GetShortTermForecastAsync(bbox);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve short-term forecast data.", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (!_isClientInjected)
                    {
                        _meteoSwissClient?.Dispose();
                    }
                    _openMeteoHttpClient?.Dispose();
                }
                _disposed = true;
            }
        }

        ~MeteoDataService()
        {
            Dispose(false);
        }
    }
}