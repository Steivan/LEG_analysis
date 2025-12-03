
using LEG.Common.Utils;
using LEG.MeteoSwiss.Abstractions.Models;
using LEG.MeteoSwiss.Abstractions.Models;
using LEG.MeteoSwiss.Client.Forecast;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using static LEG.MeteoSwiss.Abstractions.ReferenceData.MeteoStations;
using static LEG.MeteoSwiss.Client.Forecast.ForecastBlender;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetCantoGroundStations("ZH");

            // Load station metadata
            var selectedStationsMetaDict = StationMetaImporter.Import(MeteoSwissConstants.GroundStationsMetaFile);

            // Fetch latest record for selected stations
            foreach (var stationId in selectedStationsIdList)
            {
                var stationLatestRecord = MeteoAggregator.GetStationLatestRecord(stationId, selectedStationsMetaDict[stationId], granularity: "t", isTower: false);
            }

            return;

            // ******************************************   


            // Initialize list with valid ground stations
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
            var longCast = await client.Get16DayMeteoParametersAsync(lat, lon);
            var midCast = await client.Get7DayMeteoParametersAsync(lat, lon);
            var nowCast = await client.GetNowcast15MinuteMeteoParametersAsync(lat, lon);

            var blendedForecast = CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast);

            printForecastSamples($"Lat: {lat:F4}, Lon: {lon:F4}", longCast, midCast, nowCast, blendedForecast);
        }

        public static async Task GetForecastForZipList(WeatherForecastClient client, List<string> selectedZips)
        {
            foreach (var zip in selectedZips)
            {
                var longCast = await client.Get16DayMeteoParametersByZipCodeAsync(zip);
                var midCast = await client.Get7DayMeteoParametersByZipCodeAsync(zip);
                var nowCast = await client.GetNowcast15MinuteMeteoParametersByZipCodeAsync(zip);

                var blendedForecast = CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast);

                printForecastSamples($"ZIP: {zip}", longCast, midCast, nowCast, blendedForecast);
            }
        }

        public static async Task GetForecastForWeatherStations(WeatherForecastClient client, List<string> selectedStationsIdList)
        {
            foreach (var stationId in selectedStationsIdList)
            {
                var longCast = await client.Get16DayMeteoParametersByStationIdAsync(stationId);
                var midCast = await client.Get7DayMeteoParametersByStationIdAsync(stationId);
                var nowCast = await client.GetNowcast15MinuteMeteoParametersByStationIdAsync(stationId);

                var blendedForecast = CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast);

                printForecastSamples($"Station ID: {stationId}", longCast, midCast, nowCast, blendedForecast);
            }
        }

        private static void PrintMeteoParametersDataRecord(string label, MeteoParameters data)
        {
            Console.WriteLine($"{label,12} : {data.Time:dd.MM.yyyy} | {data.Interval.Minutes} m | {data.Temperature:F1}°C | WindSpeed: {data.WindSpeed:F1} km/h | " +
                $"DNI: {data.DirectNormalIrradiance:F0} W/m² | Diffuse: {data.DiffuseRadiation:F0} W/m² | Direct: {data.DirectRadiation:F0} W/m²");
        }

        public static void printForecastSamples(string location, List<MeteoParameters> longCast, List<MeteoParameters> midCast, List<MeteoParameters> nowCast, List<MeteoParameters> blendedForecast)
        {
            Console.WriteLine($"10-Day Forecast for {location}:");

            if (longCast.Count > 0)
            {
                // Convert to MeteoParameters for uniform output
                PrintMeteoParametersDataRecord("NOW", longCast[0]);
                PrintMeteoParametersDataRecord("Outlook", longCast[^1]);
            }

            Console.WriteLine($"7-Day Forecast for {location}:");
            if (midCast.Count > 0)
            {
                PrintMeteoParametersDataRecord("NOW", midCast[0]);
                PrintMeteoParametersDataRecord("Outlook", midCast[^1]);
            }

            Console.WriteLine($"90-Hour Nowcast: for {location}:");
            if (nowCast.Count > 0)
            {
                PrintMeteoParametersDataRecord("NOW", nowCast[0]);
                PrintMeteoParametersDataRecord("Outlook", nowCast[^1]);
            }

            Console.WriteLine($"Blended forecast: for {location}:");
            if (blendedForecast.Count > 0)
            {
                PrintMeteoParametersDataRecord("NOW", blendedForecast[0]);
                PrintMeteoParametersDataRecord("Outlook", blendedForecast[^1]);
            }

            Console.WriteLine();
        }
    }
}
