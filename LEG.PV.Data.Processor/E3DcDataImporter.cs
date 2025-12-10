//using LEG.CoreLib.SampleData;
//using LEG.CoreLib.SampleData.SampleData;
//using LEG.CoreLib.SolarCalculations.Calculations;
//using LEG.E3Dc.Client;
//using LEG.HorizonProfiles.Client;
//using LEG.MeteoSwiss.Abstractions.Models;
//using LEG.MeteoSwiss.Client.Forecast;
//using LEG.MeteoSwiss.Client.MeteoSwiss;
//using System.Data;
//using static LEG.MeteoSwiss.Client.Forecast.ForecastBlender;
//using LEG.PV.Core.Models;
//using static LEG.PV.Core.Models.PvDataClass;


//namespace LEG.PV.Data.Processor
//{
//    internal class E3DcDataImporter
//    {
//        public record MeteoImportResult(
//            List<StationMeteoData> PerStationMeteoParameters,
//            List<MeteoParameters> BlendedMeteoParameters,
//            string SiteId,
//            List<PvRecord> DataRecords,
//            List<bool> ValidRecords,
//            double InstalledPower,
//            int PeriodsPerHour
//            );
//        public class DataImporter
//        {
//            const double daysPerYear = 365.2422;
//            const double hoursPerDay = 24.0;
//            const double minutesPerHour = 60.0;
//            const double minutesPerYear = minutesPerHour * hoursPerDay * daysPerYear;
//            const double maxGroundIrradiance = 1000.0;                                                 // [W/m²]
//            const double radiationNoise = maxGroundIrradiance / 100.0;                                 // [W/m²]      Fluctuation of 1% of max irradiance
//            const double radiationBaselineVariance = radiationNoise * radiationNoise;                  // [(W/m²)²]
//            const double radiationVarianceMaxVariance = maxGroundIrradiance * maxGroundIrradiance / 4;        // [(W/m²)²]   Bernoulli distribution with p=0.5

//            // see also file: C:\code\LEG_analysis\Data\MeteoData\StationsData\klo_sma_hoe_ueb_recent_16.11.2025.xlsx
//            const int meteoDataOffset = 60;           // Timestamps are UTC values
//            int meteoDataLagHistory = 10;             // Values at given timestamp represent the aggregation over previous 10 minutes
//            int meteoDataLagForecast = 0;            // Forecast data lag in minutes
//            const double latSma = 47.378;
//            const double lonSma = 8.566;

//            // Selected stations, available parameters and blending weights
//            public static Dictionary<string, WeightMeteoParameters> stationDictionary = new Dictionary<string, WeightMeteoParameters>
//        {
//            { "SMA", new WeightMeteoParameters {
//                WeightSunshineDuration = 3.0,
//                WeightDirectRadiation = 3.0,
//                WeightDirectNormalIrradiance = 3.0,
//                WeightGlobalRadiation = 3.0,
//                WeightDiffuseRadiation = 0.0, // SMA station has no diffuse radiation data
//                WeightTemperature = 1.0,
//                WeightWindSpeed = 1.0,
//                WeightWindDirection = 1.0,
//                WeightSnowDepth = 1.0,
//                WeightRelativeHumidity = 1.0,
//                WeightDewPoint = 1.0,
//                WeightDirectRadiationVariance = 1.0
//            } },
//            { "KLO", new WeightMeteoParameters {
//                WeightSunshineDuration = 1.0,
//                WeightDirectRadiation = 1.0,
//                WeightDirectNormalIrradiance = 1.0,
//                WeightGlobalRadiation = 1.0,
//                WeightDiffuseRadiation = 1.0,
//                WeightTemperature = 1.0,
//                WeightWindSpeed = 1.0,
//                WeightWindDirection = 1.0,
//                WeightSnowDepth = 1.0,
//                WeightRelativeHumidity = 1.0,
//                WeightDewPoint = 1.0,
//                WeightDirectRadiationVariance = 1.0
//            } },
//            { "HOE", new WeightMeteoParameters {
//                WeightSunshineDuration = 1.0,
//                WeightDirectRadiation = 1.0,
//                WeightDirectNormalIrradiance = 1.0,
//                WeightGlobalRadiation = 1.0,
//                WeightDiffuseRadiation = 0.0, // HOE station has no diffuse radiation data
//                WeightTemperature = 0.0,
//                WeightWindSpeed = 0.0,
//                WeightWindDirection = 0.0,
//                WeightSnowDepth = 0.0,
//                WeightRelativeHumidity = 0.0,
//                WeightDewPoint = 0.0,
//                WeightDirectRadiationVariance = 1.0
//            } },
//            { "UEB", new WeightMeteoParameters {
//                WeightSunshineDuration = 1.0,
//                WeightDirectRadiation = 1.0,
//                WeightDirectNormalIrradiance = 1.0,
//                WeightGlobalRadiation = 1.0,
//                WeightDiffuseRadiation = 1.0,
//                WeightTemperature = 0.0,
//                WeightWindSpeed = 0.0,
//                WeightWindDirection = 0.0,
//                WeightSnowDepth = 0.0,
//                WeightRelativeHumidity = 0.0,
//                WeightDewPoint = 0.0,
//                WeightDirectRadiationVariance = 1.0
//            } }
//        };
//            public static List<string> selectedStationsIdList = stationDictionary.Keys.ToList();

//            // Import meteo history and merge with actual and calculated pvProduction data
//            public async Task<MeteoImportResult> ImportE3DcAndMeteoHistory(int folder, bool meteoTillNow = false)
//            {
//                // Fetch pvProduction records
//                folder = 1 + (folder - 1) % 2;
//                var siteId = folder == 1 ? ListSites.Senn : ListSites.SennV;

//                var pvDataRecords = E3DcLoadPeriodRecords.LoadRecords(folder);

//                // Determine time range and periods per hour in local time
//                var firstE3DcTimestamp = E3DcFileHelper.ParseTimestamp(pvDataRecords[0].Timestamp);
//                var secondE3DcTimestamp = E3DcFileHelper.ParseTimestamp(pvDataRecords[1].Timestamp);
//                var lastE3DcTimestamp = E3DcFileHelper.ParseTimestamp(pvDataRecords[^1].Timestamp);
//                var minutesPerPeriod = (secondE3DcTimestamp - firstE3DcTimestamp).Minutes;
//                var periodsPerHour = 60 / minutesPerPeriod;

//                var firstTimestamp = firstE3DcTimestamp;
//                var lastTimestamp = meteoTillNow ? DateTime.Now : DateTime.Now.AddDays(10);

//                // Fetch geometry factors
//                var (timeStamps, geometryFactors, installedPower) = await PvProduction(siteId, firstTimestamp, lastTimestamp, minutesPerPeriod, shiftSupportTimeStamps: 0);
//                firstTimestamp = timeStamps[0];
//                lastTimestamp = timeStamps[^1];

//                // Fetch meteo data
//                // Update historic weather data for selected stations
//                MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetAllGroundStations();
//                var updateClient = new MeteoSwissUpdater();
//                await updateClient.UpdateDataForGroundStations(selectedStationsIdList, granularity: "t");

//                meteoDataLagHistory = 5 * (int)Math.Round((double)meteoDataLagHistory / 5);             // Lag to be applied to historical data
//                var (perStationWeatherData, blendedWeatherData) = LoadBlendedWeatherHistory(
//                    stationDictionary,
//                    timeStamps,
//                    shiftMeteoTimeStamps: meteoDataOffset + meteoDataLagHistory);

//                // Merge data
//                var countOfE3DcRecords = pvDataRecords.Count;
//                var countOfMeteoRecords = blendedWeatherData.Count;
//                var dataRecords = new List<PvRecord>();
//                var validRecords = new List<bool>();
//                for (var i = 0; i < countOfMeteoRecords; i++)
//                {
//                    var recordIndex = i;
//                    var meteoParam = blendedWeatherData[i];
//                    var weight = 1.0 / (1E-6 + meteoParam.DirectRadiationVariance ?? (double.MaxValue - 1E-6));
//                    double? solarProduction = i < countOfE3DcRecords ? pvDataRecords[i].SolarProduction : null;
//                    if (!solarProduction.HasValue)
//                    {
//                        weight = 0.0;
//                    }
//                    var age = (timeStamps[i] - firstE3DcTimestamp).TotalMinutes / minutesPerYear;
//                    var pvRecord = new PvRecord(
//                        timeStamps[i],
//                        recordIndex,                            // TODO: pvDataRecord.Index,
//                        geometryFactors[i],
//                        meteoParam,
//                        weight,
//                        age,
//                        solarProduction
//                        );
//                    dataRecords.Add(pvRecord);
//                    var validE3Dc = solarProduction.HasValue && solarProduction.Value > 0.0;
//                    validRecords.Add(pvRecord.SolarGeometry.HasIrradiance || validE3Dc);
//                }

//                return new MeteoImportResult(perStationWeatherData, blendedWeatherData, siteId, dataRecords, validRecords, installedPower, periodsPerHour);
//            }

//            // Import meteo forecast and merge with calculated pvProduction data
//            public async Task<MeteoImportResult> ImportMeteoForecastAndCalculatedProduction(
//                int folder,
//                DateTime firstE3DcTimestamp,
//                DateTime lastHistoryTimestamp,
//                int forecastDays = 16)
//            {
//                folder = 1 + (folder - 1) % 2;
//                var siteId = folder == 1 ? ListSites.Senn : ListSites.SennV;
//                forecastDays = Math.Max(0, Math.Min(forecastDays, 16));

//                const int minutesPerPeriod = 15;
//                const int periodsPerHour = 60 / minutesPerPeriod;

//                var firstTimestamp = lastHistoryTimestamp;
//                var lastTimestamp = firstTimestamp.AddDays(forecastDays);

//                // Fetch geometry factors
//                var (timeStamps, geometryFactors, installedPower) = await PvProduction(siteId, firstTimestamp, lastTimestamp, minutesPerPeriod, shiftSupportTimeStamps: 0);
//                firstTimestamp = timeStamps[0];
//                lastTimestamp = timeStamps[^1];

//                // Fetch meteo data
//                meteoDataLagForecast = 5 * (int)Math.Round((double)meteoDataLagForecast / 5);               // Lag to be applied to forecast data
//                var (perStationWeatherData, blendedWeatherData) = await LoadBlendedWeatherForecast(
//                    stationDictionary,
//                    timeStamps,
//                    shiftMeteoTimeStamps: meteoDataOffset + meteoDataLagForecast);

//                // Merge data
//                var countOfMeteoRecords = blendedWeatherData.Count;
//                var dataRecords = new List<PvRecord>();
//                var validRecords = new List<bool>();
//                for (var i = 0; i < countOfMeteoRecords; i++)
//                {
//                    var recordIndex = i;
//                    //var meteoParam = blendedWeatherData[i];
//                    var age = (timeStamps[i] - firstE3DcTimestamp).TotalMinutes / minutesPerYear;
//                    var pvRecord = new PvRecord(
//                        timeStamps[i],
//                        recordIndex,
//                        geometryFactors[i],
//                        blendedWeatherData[i],
//                        0.0,
//                        age,
//                        null
//                        );

//                    dataRecords.Add(pvRecord);
//                    validRecords.Add(false);
//                }

//                return new MeteoImportResult(perStationWeatherData, blendedWeatherData, siteId, dataRecords, validRecords, installedPower, periodsPerHour);
//            }
//        }
//    }
//}
