
using LEG.Common.Utils;
using LEG.MeteoSwiss.Client.Forecast;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using static LEG.MeteoSwiss.Abstractions.ReferenceData.MeteoStations;
using static LEG.MeteoSwiss.Client.Forecast.ForecastBlender;

namespace MeteoConsoleApp
{
    class Program
    {

        static async Task Main(string[] args)
        {
            // Selected locations for forecast retrieval
            var (lat, lon) = (47.377925, 8.565742);     // SMA
            List<string> selectedZips = ["8124", "7550"];
            List<string> selectedStationsIdList = ["SMA", "KLO", "HOE", "UEB"];

            var client = new WeatherForecastClient();

            await GetForecastForLatLon(client, lat, lon);
            await GetForecastForZipList(client, selectedZips);
            await GetForecastForWeatherStations(client, selectedStationsIdList);

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
                // ARO, CHU, NAS, SCU
            };

            // Stations to be processed
            const string stationCanton = "ZH"; // "CH" for all
            const bool lonLatAsDms = true;

            var downloadGroundStationIds = groundStationsGrSelection;
            var downloadTowerStationIds = emptyList;
            const int downloadStartYear = 2000;
            const string downloadGranularity = "ht"; // "h" for hourly, "t" for 10-min
            // The 'forceDownload' flag is no longer needed as the new client always fetches the latest data.

            const string aggregationId = "SMA";
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
                    lon => GeoUtils.ToDms(lon),
                    lat => GeoUtils.ToDms(lat),
                    lonLatAsDms
                );
                StationConsolePrinter.PrintStationMetaSeparator();
                idList.AddRange(StationConsolePrinter.PrintStationMetaTable(
                    $"List of tower stations in {stationCanton}",
                    towerStationsMetaDict,
                    stationCanton,
                    lon => GeoUtils.ToDms(lon),
                    lat => GeoUtils.ToDms(lat),
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
                    lon => GeoUtils.ToDms(lon),
                    lat => GeoUtils.ToDms(lat),
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
                    lon => GeoUtils.ToDms(lon),
                    lat => GeoUtils.ToDms(lat),
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

        public static async Task GetForecastForLatLon(WeatherForecastClient client, double lat, double lon)
        {
            var longCast = await client.Get16DayPeriodsAsync(lat, lon);
            var midCast = await client.Get7DayPeriodsAsync(lat, lon);
            var nowCast = await client.GetNowcast15MinuteAsync(lat, lon);

            var blendedForecast = CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast);

            printForecastSamples($"Lat: {lat:F4}, Lon: {lon:F4}", longCast, midCast, nowCast, blendedForecast);
        }

        public static async Task GetForecastForZipList(WeatherForecastClient client, List<string> selectedZips)
        {
            foreach (var zip in selectedZips)
            {
                var longCast = await client.Get16DayPeriodsByZipCodeAsync(zip);
                var midCast = await client.Get7DayPeriodsByZipCodeAsync(zip);
                var nowCast = await client.GetNowcast15MinuteByZipCodeAsync(zip);

                var blendedForecast = CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast);

                printForecastSamples($"ZIP: {zip}", longCast, midCast, nowCast, blendedForecast);
            }
        }

        public static async Task GetForecastForWeatherStations(WeatherForecastClient client, List<string> selectedStationsIdList)
        {
            foreach (var stationId in selectedStationsIdList)
            {
                var longCast = await client.Get16DayPeriodsByStationIdAsync(stationId);
                var midCast = await client.Get7DayPeriodsByStationIdAsync(stationId);
                var nowCast = await client.GetNowcast15MinuteByStationIdAsync(stationId);

                var blendedForecast = CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast);

                printForecastSamples($"Station ID: {stationId}", longCast, midCast, nowCast, blendedForecast);
            }
        }

        public static void printForecastSamples(string location, List<ForecastPeriod> longCast, List<ForecastPeriod> midCast, List<NowcastPeriod> nowCast, List<BlendedPeriod> blendedForecast)
        {
            Console.WriteLine($"10-Day Forecast for {location}:");

            if (longCast.Count > 0)
            {
                var current = longCast[0];
                var last = longCast[^1];
                Console.WriteLine($"NOW     → {current.Time:dd.MM.yyyy} | {current.LocalTime:HH:mm} | {current.TemperatureC:F1}°C | WindSpeed: {current.WindSpeedMs:F1} km/h | " +
                                              $"Direct: {current.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {current.DiffuseRadiationWm2:F0} W/m² | Solar: {current.DirectRadiationWm2:F0} W/m²");
                Console.WriteLine($"Outlook → {last.Time:dd.MM.yyyy} | {last.LocalTime:HH:mm} | {last.TemperatureC:F1}°C | WindSpeed: {last.WindSpeedMs:F1} km/h | " +
                                              $"Direct: {last.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {last.DiffuseRadiationWm2:F0} W/m² | Solar: {last.DirectRadiationWm2:F0} W/m²");
            }

            Console.WriteLine($"7-Day Forecast for {location}:");
            if (midCast.Count > 0)
            {
                var current = midCast[0];
                var last = midCast[^1];
                Console.WriteLine($"NOW     → {current.Time:dd.MM.yyyy} | {current.LocalTime:HH:mm} | {current.TemperatureC:F1}°C | WindSpeed: {current.WindSpeedMs:F1} km/h | " +
                                              $"Direct: {current.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {current.DiffuseRadiationWm2:F0} W/m² | Solar: {current.DirectRadiationWm2:F0} W/m²");
                Console.WriteLine($"Outlook → {last.Time:dd.MM.yyyy} | {last.LocalTime:HH:mm} | {last.TemperatureC:F1}°C | WindSpeed: {last.WindSpeedMs:F1} km/h | " +
                                              $"Direct: {last.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {last.DiffuseRadiationWm2:F0} W/m² | Solar: {last.DirectRadiationWm2:F0} W/m²");
            }

            Console.WriteLine($"90-Hour Nowcast: for {location}:");
            if (nowCast.Count > 0)
            {
                var current = nowCast[0];
                var last = nowCast[^1];
                Console.WriteLine($"NOW     → {current.Time:dd.MM.yyyy} | {current.LocalTime:HH:mm} | {current.TemperatureC:F1}°C | WindSpeed: {current.WindSpeedMs:F1} km/h | " +
                                              $"Direct: {current.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {current.DiffuseRadiationWm2:F0} W/m² | Solar: {current.GlobalRadiationWm2:F0} W/m²");
                Console.WriteLine($"Outlook → {last.Time:dd.MM.yyyy} | {last.LocalTime:HH:mm} | {last.TemperatureC:F1}°C | WindSpeed: {last.WindSpeedMs:F1} km/h | " +
                                              $"Direct: {last.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {last.DiffuseRadiationWm2:F0} W/m² | Solar: {last.GlobalRadiationWm2:F0} W/m²");
            }

            Console.WriteLine($"Blended forecast: for {location}:");
            if (nowCast.Count > 0)
            {
                var current = blendedForecast[0];
                var last = blendedForecast[^1];
                Console.WriteLine($"NOW     → {current.Time:dd.MM.yyyy} | {current.Time:HH:mm} | {current.TempC:F1}°C | WindSpeed: {current.WindKmh:F1} km/h | SnowDepth: {current.SnowDepthCm:F1} cm | " +
                                              $"Direct: {current.DNIWm2:F0} W/m² | Diffuse: {current.DiffuseHRWm2:F0} W/m²");
                Console.WriteLine($"Outlook → {last.Time:dd.MM.yyyy} | {last.Time:HH:mm} | {last.TempC:F1}°C | WindSpeed: {last.WindKmh:F1} km/h | SnowDepth: {last.SnowDepthCm:F1} cm | " +
                                              $"Direct: {last.DNIWm2:F0} W/m² | Diffuse: {last.DiffuseHRWm2:F0} W/m²");
            }

            Console.WriteLine();
        }
    }
}
