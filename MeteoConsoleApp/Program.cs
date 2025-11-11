
using LEG.Common.Utils;
using LEG.MeteoSwiss.Abstractions;
using LEG.MeteoSwiss.Client.Forecast;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using static LEG.MeteoSwiss.Abstractions.ReferenceData.MeteoStations;

namespace MeteoConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new WeatherForecastClient();

            var forecast = await client.Get7DayPeriodsAsync("8124");
            Console.WriteLine("7-Day Forecast:");
            if (forecast.Count > 0)
            {
                var current = forecast[0];
                var last = forecast[^1];
                Console.WriteLine($"NOW     → {current.LocalTime:HH:mm} | {current.TemperatureC:F1}°C | " +
                                  $"Solar: {current.SolarRadiationWm2:F0} W/m² | Gust: {current.WindGustsKmh:F1} km/h");
                Console.WriteLine($"Outlook → {last.LocalTime:HH:mm} | {last.TemperatureC:F1}°C | " +
                                  $"Solar: {last.SolarRadiationWm2:F0} W/m² | Gust: {last.WindGustsKmh:F1} km/h");
            }

            var nowcast = await client.GetNowcast15MinuteAsync("8124");
            Console.WriteLine("90-Hour Nowcast:");
            if (nowcast.Count > 0)
            {
                var current = nowcast[0];
                var last = nowcast[^1];
                Console.WriteLine($"NOW     → {current.LocalTime:HH:mm} | {current.TemperatureC:F1}°C | " +
                                  $"Solar: {current.SolarRadiationWm2:F0} W/m² | Gust: {current.WindGustsKmh:F1} km/h");
                Console.WriteLine($"Outlook → {last.LocalTime:HH:mm} | {last.TemperatureC:F1}°C | " +
                                  $"Solar: {last.SolarRadiationWm2:F0} W/m² | Gust: {last.WindGustsKmh:F1} km/h");
            }

            return;

            // ******************************************   

            // 1. Instantiate our new, consolidated service layer.
            var apiClient = new MeteoSwissClient();
            var meteoDataService = new MeteoDataService(apiClient);

            // Initialize list with valid ground stations
            MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetBaselineGroundStations();

            // Load station metadata
            var groundStationsMetaDict =
                StationMetaImporter.Import(MeteoSwissConstants.GroundStationsMetaFile);
            var towerStationsMetaDict =
                StationMetaImporter.Import(MeteoSwissConstants.TowerStationsMetaFile);
            var longestPerStationDict =
                LongestPerStationInfoImporter.Import(MeteoSwissConstants.LongestPerStationInfoFile);
            var standardPerStationDict =
                StandardPerStationInfoImporter.Import(MeteoSwissConstants.StandardPerStationInfoFile);

            // Update list with valid ground stations
            var stationIds = new List<string>(groundStationsMetaDict.Keys);
            MeteoSwissHelper.UpdateValidGroundStations([.. stationIds]);

            // Define station groups (ALL PRESERVED AS REQUESTED)
            var emptyList = new List<string>();
            var allGroundStations = new List<string>(groundStationsMetaDict.Keys);
            var allTowerStations = new List<string>(towerStationsMetaDict.Keys);

            var groundStationsZh = new List<string>()
            {
                HOE, KLO, LAE, PFA, REH, SMA, UEB, WAE
            };
            var groundStationsGr = new List<string>()
            {
                AND, ARO, BEH, BIV, BUF, CHU, CMA, COV, DAV, DIS, GRO, ILZ, LAT,
                NAS, PMA, ROB, SAM, SBE, SCU, SIA, SMM, SRS, VAB, VIO, VLS, WFJ
            };
            var groundStationsGrSelection = new List<string>()
            {
                ARO, CHU, NAS, SCU
            };

            // Stations to be processed
            const string stationCanton = "GR"; // "CH" for all
            const bool lonLatAsDms = true;

            var downloadGroundStationIds = groundStationsGrSelection;
            var downloadTowerStationIds = emptyList;
            const int downloadStartYear = 2000;
            const string downloadGranularity = "ht"; // "h" for hourly, "t" for 10-min
            // The 'forceDownload' flag is no longer needed as the new client always fetches the latest data.

            const string aggregationId = "SCU";
            const bool isTower = false;

            // No command line arguments are provided
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments provided, running default aggregation...");
                Console.WriteLine();

                // Print station metadata tables
                StationConsolePrinter.PrintStationMetaSeparator();
                var idList = StationConsolePrinter.PrintStationMetaTable(
                    $"List of ground stations in {stationCanton}",
                    groundStationsMetaDict,
                    stationCanton,
                    GeoUtils.ToDms,
                    lonLatAsDms
                );
                StationConsolePrinter.PrintStationMetaSeparator();
                idList.AddRange(StationConsolePrinter.PrintStationMetaTable(
                    $"List of tower stations in {stationCanton}",
                    towerStationsMetaDict,
                    stationCanton,
                    GeoUtils.ToDms,
                    lonLatAsDms
                ));
                StationConsolePrinter.PrintStationMetaSeparator();

                // Print station info tables
                StationConsolePrinter.PrintStationInfoTable(
                    "Longest Period Station Info:",
                    longestPerStationDict,
                    idList,
                    [
                        info => info.Name,
                        info => info.Height ?? 0,
                        info => info.Lon ?? 0,
                        info => info.Lat ?? 0,
                        info => info.FirstYearDailyObs ?? 0,
                        info => info.LastYearDailyObs ?? 0,
                        info => info.TotNrYearsDailyObs ?? 0,
                        info => info.NrYearsDailyObsInRefPer ?? 0,
                        info => info.HasHourlyData
                    ],
                    GeoUtils.ToDms,
                    lonLatAsDms
                );

                StationConsolePrinter.PrintStationInfoTable(
                    "Standard Period Station Info:",
                    standardPerStationDict,
                    idList, 
                    [
                        info => info.Name,
                        info => info.Height ?? 0,
                        info => info.Lon ?? 0,
                        info => info.Lat ?? 0,
                        info => info.FirstYearDailyObs ?? 0,
                        info => info.LastYearDailyObs ?? 0,
                        info => info.NrYearsDailyObsInStandardPer ?? 0,
                        info => info.NrNaYearsDailyObsInStandardPer ?? 0,
                        info => info.HasHourlyData
                    ],
                    GeoUtils.ToDms,
                    lonLatAsDms
                );

                Console.WriteLine();
                Console.WriteLine($"Stations to process: {string.Join(", ", idList.Select(id => $"\"{id.ToUpper()}\""))}");

                // ** REFACTORED: Use the new MeteoDataService for all downloads. **
                var startDate = new DateTime(downloadStartYear, 1, 1).ToString("o");
                var endDate = new DateTime(DateTime.UtcNow.Year, 12, 31).ToString("o");

                Console.WriteLine();
                Console.WriteLine($"Downloading data for ground stations {string.Join(", ", downloadGroundStationIds.Select(id => $"\"{id.ToUpper()}\""))}");
                foreach (var id in downloadGroundStationIds)
                {
                    await meteoDataService.GetHistoricalWeatherAsync(startDate, endDate, id, downloadGranularity);
                }

                Console.WriteLine();
                Console.WriteLine($"Downloading data for tower stations {string.Join(", ", downloadTowerStationIds.Select(id => $"\"{id.ToUpper()}\""))}");
                foreach (var id in downloadTowerStationIds)
                {
                    await meteoDataService.GetHistoricalWeatherAsync(startDate, endDate, id, downloadGranularity);
                }

                // Run aggregation for the specified station
                Console.WriteLine();
                Console.WriteLine($"Aggregating data for {(isTower ? "tower" : "ground")} station {aggregationId}");
                if (stationIds.Contains(aggregationId.ToUpper()))
                {
                    MeteoAggregator.RunMeteoAggregationForPeriod(aggregationId, groundStationsMetaDict[aggregationId],
                        downloadStartYear, DateTime.UtcNow.Year,
                        isTower: isTower);
                }

                return;
            }
        }
    }
}