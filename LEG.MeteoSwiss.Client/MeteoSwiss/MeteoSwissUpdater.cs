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
        public async Task UpdateDataForGroundStations(List<string> stationsList, string granularity = "t")
        {
            var apiClient = new MeteoSwissClient();
            var downloadEndDate = DateTime.UtcNow;

            foreach (var stationId in stationsList)
            {
                var downloadStartDate = GetUpdateStartDate(stationId, isTower: false, granularity: granularity) ?? new DateTime(2020, 1, 1);
                await apiClient.UpdatePeriodFiles(downloadStartDate.ToString("yyyy-MM-dd"), downloadEndDate.ToString("yyyy-MM-dd"), stationId, granularity);
            }
        }

        public async Task UpdateDataForTowerStations(List<string> stationsList, string granularity = "t")
        {
            var apiClient = new MeteoSwissClient();
            var downloadEndDate = DateTime.UtcNow;

            foreach (var stationId in stationsList)
            {
                var downloadStartDate = GetUpdateStartDate(stationId, isTower: true, granularity: granularity) ?? new DateTime(2020, 1, 1);
                await apiClient.UpdatePeriodFiles(downloadStartDate.ToString("yyyy-MM-dd"), downloadEndDate.ToString("yyyy-MM-dd"), stationId, granularity);
            }
        }
    }
}