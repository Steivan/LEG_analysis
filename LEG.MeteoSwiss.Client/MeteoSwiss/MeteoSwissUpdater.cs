using LEG.Common;
using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.MeteoSwiss.Client.MeteoSwiss
{
    public class MeteoSwissUpdater
    {

        private static DateTime? GetUpdateStartDate(string stationId, bool isTower = false, string granularity = "t")
        {
            DateTime? GetDateTimeLastRecord(string period)
            {
                var filePath = "";
                if (isTower)
                {
                    (_, filePath) = MeteoSwissHelper.GetTowerCsvFilename(stationId, period, granularity: granularity);
                }
                else
                {
                    (_, filePath) = MeteoSwissHelper.GetGroundCsvFilename(stationId, period, granularity: granularity);
                }
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Warning: File not found: {filePath}");
                    return null;
                }
                var records = ImportCsv.ImportFromFile<WeatherCsvRecord>(filePath, ";");
                return records[^1].ReferenceTimestamp;
            }

            if (string.IsNullOrWhiteSpace(stationId))
            {
                Console.WriteLine("stationId must not be null or empty.", nameof(stationId));
                return null;
            }

            var updateStartDate = new DateTime(2000, 1, 1);
            var now = DateTime.UtcNow;
            // Getdata from current decade file
            var decade = now.Year / 10 * 10;
            var period = MeteoSwissHelper.NormalizeAndValidatePeriod($"{decade}-{decade + 9}");
            var latestDecadeRecord = GetDateTimeLastRecord(period);
            if (latestDecadeRecord.HasValue && latestDecadeRecord.Value.Year + 1 >= now.Year)
            {
                updateStartDate = new DateTime(now.Year, 1, 1);

                // Get recent data from the "recent" file
                var latestRecentRecord = GetDateTimeLastRecord("recent");
                if (latestRecentRecord.HasValue && latestRecentRecord.Value.AddDays(1) >= now)
                {
                    updateStartDate = new DateTime(now.Year, now.Month, now.Day);
                }
            }

            return updateStartDate;
        }
        public async Task<List<ValidMeteoParameters>> UpdateWeatherData(DateTime downloadStartDate, List<string> stationsList)
        {
            var apiClient = new MeteoSwissClient();
            var meteoDataService = new MeteoDataService(apiClient);
            // Initialize list with valid ground stations
            MeteoSwissHelper.ValidGroundStations = stationsList.ToArray();
            // Load station metadata
            var selectedStationsMetaDict = StationMetaImporter.Import(MeteoSwissConstants.GroundStationsMetaFile);

            var validMeteoParametersList = new List<ValidMeteoParameters>();
            foreach (var stationId in stationsList)
            {
                var downloadStartYear = downloadStartDate.Year;
                var filePath = Path.Combine(MeteoSwissConstants.MeteoStationsDataFolder, $"stac_t_{stationId}_{DateTime.UtcNow.Year}.csv");

                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    downloadStartYear = fileInfo.CreationTime.Year;
                }

                var startDate = new DateTime(downloadStartYear, 1, 1).ToString("o");
                var endDate = new DateTime(DateTime.UtcNow.Year, 12, 31).ToString("o");

                await meteoDataService.GetHistoricalWeatherAsync(startDate, endDate, stationId, "t");

                var stationLatestRecord = MeteoAggregator.GetStationLatestMeteoParametersRecord(stationId, selectedStationsMetaDict[stationId], granularity: "t", isTower: false);
                validMeteoParametersList.Add(stationLatestRecord.GetValidMeteoParameters);
            }

            return validMeteoParametersList;
        }

        private async Task UpdateDataForStations(List<string> stationsList, bool isTower = false, string granularity = "t")
        {
            var apiClient = new MeteoSwissClient();
            var downloadEndDate = DateTime.UtcNow;

            foreach (var stationId in stationsList)
            {
                var downloadStartDate = GetUpdateStartDate(stationId, isTower: isTower, granularity: granularity) ?? new DateTime(2020, 1, 1);
                await apiClient.UpdatePeriodFiles(downloadStartDate.ToString("yyyy-MM-dd"), downloadEndDate.ToString("yyyy-MM-dd"), stationId, granularity);
            }
        }

        public async Task UpdateDataForGroundStations(List<string> stationsList, string granularity = "t")
        {
            await UpdateDataForStations(stationsList, isTower: false, granularity: granularity);
        }

        public async Task UpdateDataForTowerStations(List<string> stationsList, string granularity = "t")
        {
            await UpdateDataForStations(stationsList, isTower: true, granularity: granularity);
        }
    }
}