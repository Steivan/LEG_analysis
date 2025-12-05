using LEG.MeteoSwiss.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace LEG.MeteoSwiss.Client.MeteoSwiss
{
    public class MeteoSwissClient : IMeteoSwissClient
    {
        private readonly HttpClient _httpClient;
        private bool _isInitialized = false;

        public MeteoSwissClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true
            };
            _httpClient = new HttpClient(handler);

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://data.geo.admin.ch/");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _httpClient.DefaultRequestHeaders.Add("DNT", "1");
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            _httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"91\", \"Google Chrome\";v=\"91\"");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        }

        private async Task InitializeSessionAsync()
        {
            if (_isInitialized) return;
            await _httpClient.GetAsync("https://www.meteoswiss.admin.ch/services-and-publications/applications/measurement-values-and-measuring-networks.html");
            _isInitialized = true;
        }

        private async Task DownloadFile(string stationId, string granularity, string period, List<string> allDataRows)
        {
            var lowerCaseStationId = stationId.ToLower();
            var filename = $"ogd-smn_{lowerCaseStationId}_{granularity}_{period}.csv";
            var collectionName = "ch.meteoschweiz.ogd-smn";
            var directUrl = $"https://data.geo.admin.ch/{collectionName}/{lowerCaseStationId}/{filename}";

            var dataFolder = @"C:\code\LEG_analysis\Data\MeteoData\StationsData\";
            var destinationPath = Path.Combine(dataFolder, lowerCaseStationId, filename);

            Console.WriteLine($"Attempting to download from correct URL: {directUrl}");

            try
            {
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));

                var responseBytes = await _httpClient.GetByteArrayAsync(directUrl);

                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(destinationPath, responseBytes);
                Console.WriteLine($"Successfully downloaded and saved to {destinationPath}");

                var csvResponse = Encoding.UTF8.GetString(responseBytes);
                allDataRows.AddRange(csvResponse.Split('\n'));
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"No data file found for station {stationId} for period '{period}' at {directUrl}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"File Error for {destinationPath}: {ex.Message}. The file may be open in another program.");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred for {directUrl}: {ex.Message}");
                throw;
            }
        }

        private HashSet<string> GetPeriods(string startDate, string endDate)
        {

            var start = DateOnly.Parse(startDate);
            var end = DateOnly.Parse(endDate);
            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentYear = now.Year;

            var periodsToFetch = new HashSet<string>();
            for (int year = start.Year; year <= end.Year; year++)
            {
                if (year >= currentYear - 1)
                {
                    if (start < now)
                    {
                        periodsToFetch.Add("recent");
                    }
                    periodsToFetch.Add("now");
                }
                else
                {
                    periodsToFetch.Add($"historical_{((year / 10) * 10)}-{((year / 10) * 10) + 9}");
                }
            }
            return periodsToFetch;
        }

        public async Task UpdatePeriodFiles(string startDate, string endDate, string stationId, string granularity)
        {
            var periodsToFetch = GetPeriods(startDate, endDate);
            var _ = new List<string>();
            foreach (var period in periodsToFetch)
            {
                await DownloadFile(stationId, granularity, period, _);
            }
        }

        public async Task<string[]> GetHistoricalDataAsync(string startDate, string endDate, string stationId, string granularity)
        {
            await InitializeSessionAsync();

            if (string.IsNullOrEmpty(stationId))
            {
                return [];
            }

            var periodsToFetch = GetPeriods(startDate, endDate);
            var allDataRows = new List<string>();
            foreach (var period in periodsToFetch)
            {
                await DownloadFile(stationId, granularity, period, allDataRows); // Download and accumulate data rows in allDataRows
            }

            Console.WriteLine($"Total Data Rows found for granularity '{granularity}': {allDataRows.Count}");

            return [.. allDataRows];
        }

        // Other methods remain NotImplemented for now
        public Task<byte[]> GetShortTermForecastAsync(double[] bbox) => throw new NotImplementedException();
        public Task ListItemsForCollectionAsync(string collectionId, int limit = 5) => throw new NotImplementedException();
        public Task<List<string>> ListAvailableCollectionsAsync() => throw new NotImplementedException();

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}