using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;
using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.E3Dc.Client;
using LEG.HorizonProfiles.Client;
using LEG.MeteoSwiss.Abstractions.Models;
using LEG.MeteoSwiss.Client.Forecast;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using System.Data;
using static LEG.MeteoSwiss.Client.Forecast.ForecastBlender;
using static LEG.PV.Core.Models.DataRecords;

namespace LEG.PV.Data.Processor
{
    public record MeteoImportResult(
        List<StationMeteoData> PerStationMeteoParameters,
        List<MeteoParameters> BlendedMeteoParameters,
        string SiteId,
        List<PvRecord> DataRecords,
        List<bool> ValidRecords,
        double InstalledPower,
        int PeriodsPerHour
        );
    public class DataImporter
    {
        const double daysPerYear = 365.2422;
        const double hoursPerDay = 24.0;
        const double minutesPerHour = 60.0;
        const double minutesPerYear = minutesPerHour * hoursPerDay * daysPerYear;
        const double solarConstant = 1361.0;                                                       // [W/m²]
        const double maxGroundIrradiance = 1000.0;                                                 // [W/m²]
        const double radiationNoise = maxGroundIrradiance / 100.0;                                 // [W/m²]      Fluctuation of 1% of max irradiance
        const double radiationBaselineVariance = radiationNoise * radiationNoise;                  // [(W/m²)²]
        const double radiationVarianceMaxVariance = maxGroundIrradiance * maxGroundIrradiance / 4;        // [(W/m²)²]   Bernoulli distribution with p=0.5

        // see also file: C:\code\LEG_analysis\Data\MeteoData\StationsData\klo_sma_hoe_ueb_recent_16.11.2025.xlsx
        const int meteoDataOffset = 60;           // Timestamps are UTC values
        int meteoDataLagHistory = 10;             // Values at given timestamp represent the aggregation over previous 10 minutes
        int meteoDataLagForecast = 0;            // Forecast data lag in minutes
        const double latSma = 47.378;
        const double lonSma = 8.566;

        // Selected stations, available parameters and blending weights
        public static Dictionary<string, WeightMeteoParameters> stationDictionary = new Dictionary<string, WeightMeteoParameters>
        {
            { "SMA", new WeightMeteoParameters { 
                WeightSunshineDuration = 3.0, 
                WeightDirectRadiation = 3.0, 
                WeightDirectNormalIrradiance = 3.0, 
                WeightGlobalRadiation = 3.0,
                WeightDiffuseRadiation = 0.0, // SMA station has no diffuse radiation data
                WeightTemperature = 1.0,
                WeightWindSpeed = 1.0,
                WeightSnowDepth = 1.0
            } },
            { "KLO", new WeightMeteoParameters { 
                WeightSunshineDuration = 1.0, 
                WeightDirectRadiation = 1.0, 
                WeightDirectNormalIrradiance = 1.0,
                WeightGlobalRadiation = 1.0,
                WeightDiffuseRadiation = 1.0,
                WeightTemperature = 1.0,
                WeightWindSpeed = 1.0,
                WeightSnowDepth = 1.0
            } },
            { "HOE", new WeightMeteoParameters { 
                WeightSunshineDuration = 1.0, 
                WeightDirectRadiation = 1.0, 
                WeightDirectNormalIrradiance = 1.0, 
                WeightGlobalRadiation = 1.0, 
                WeightDiffuseRadiation = 0.0, // HOE station has no diffuse radiation data
                WeightTemperature = 0.0,
                WeightWindSpeed = 0.0,
                WeightSnowDepth = 0.0
            } },
            { "UEB", new WeightMeteoParameters {
                WeightSunshineDuration = 1.0,
                WeightDirectRadiation = 1.0,
                WeightDirectNormalIrradiance = 1.0,
                WeightGlobalRadiation = 1.0,
                WeightDiffuseRadiation = 1.0,
                WeightTemperature = 0.0,
                WeightWindSpeed = 0.0,
                WeightSnowDepth = 0.0
            } }
        };
        public static List<string> selectedStationsIdList = stationDictionary.Keys.ToList();

        // Import meteo history and merge with actual and calculated pvProduction data
        public async Task<MeteoImportResult> ImportE3DcAndMeteoHistory(int folder, bool meteoTillNow = false)
        {
            // Fetch pvProduction records
            folder = 1 + (folder - 1) % 2;
            var siteId = folder == 1 ? ListSites.Senn : ListSites.SennV;

            var pvDataRecords = E3DcLoadPeriodRecords.LoadRecords(folder);

            // Determine time range and periods per hour in local time
            var firstE3DcTimestamp = E3DcFileHelper.ParseTimestamp(pvDataRecords[0].Timestamp);
            var secondE3DcTimestamp = E3DcFileHelper.ParseTimestamp(pvDataRecords[1].Timestamp);
            var lastE3DcTimestamp = E3DcFileHelper.ParseTimestamp(pvDataRecords[^1].Timestamp);
            var minutesPerPeriod = (secondE3DcTimestamp - firstE3DcTimestamp).Minutes;
            var periodsPerHour = 60 / minutesPerPeriod;

            var firstTimestamp = firstE3DcTimestamp;
            var lastTimestamp = meteoTillNow ? DateTime.Now : DateTime.Now.AddDays(10);

            // Fetch geometry factors
            var (timeStamps, directGeometryFactors, diffuseGeometryFactor, sinSunElevations, installedPower) = await PvProduction(siteId, firstTimestamp, lastTimestamp, minutesPerPeriod, shiftSupportTimeStamps: 0);
            firstTimestamp = timeStamps[0];
            lastTimestamp = timeStamps[^1];

            // Fetch meteo data
            // Update historic weather data for selected stations
            MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetAllGroundStations();
            var updateClient = new MeteoSwissUpdater();
            await updateClient.UpdateDataForGroundStations(selectedStationsIdList, granularity: "t");

            meteoDataLagHistory = 5 * (int)Math.Round((double)meteoDataLagHistory / 5);             // Lag to be applied to historical data
            var (perStationWeatherData, blendedWeatherData) = LoadBlendedWeatherHistory(
                stationDictionary,
                timeStamps,
                shiftMeteoTimeStamps: meteoDataOffset + meteoDataLagHistory);

            // Merge data
            var countOfE3DcRecords = pvDataRecords.Count;
            var countOfMeteoRecords = blendedWeatherData.Count;
            var dataRecords = new List<PvRecord>();
            var validRecords = new List<bool>();
            for (var i = 0; i < countOfMeteoRecords; i++)
            {
                var recordIndex = i;
                var meteoParam = blendedWeatherData[i];
                var weight = 1.0 / (1E-6 + meteoParam.DirectRadiationVariance ?? (double.MaxValue - 1E-6));
                //var pvDataRecord = pvDataRecords[i];
                double? solarProduction = i < countOfE3DcRecords ? pvDataRecords[i].SolarProduction : null;
                if (!solarProduction.HasValue)
                {
                    weight = 0.0;
                }
                var age = (timeStamps[i] - firstE3DcTimestamp).TotalMinutes / minutesPerYear;
                var pvRecord = new PvRecord(
                    timeStamps[i],
                    recordIndex,                            // TODO: pvDataRecord.Index,
                    directGeometryFactors[i],
                    diffuseGeometryFactor,
                    sinSunElevations[i],
                    meteoParam.SunshineDuration ?? 0.0,
                    meteoParam.DirectRadiation ?? 0.0,
                    meteoParam.DirectNormalIrradiance ?? 0.0,
                    meteoParam.GlobalRadiation ?? 0.0,
                    meteoParam.DiffuseRadiation ?? 0.0,
                    meteoParam.Temperature ?? 0.0,
                    meteoParam.WindSpeed ?? 0.0,
                    meteoParam.SnowDepth ?? 0.0,
                    weight,
                    age,
                    solarProduction
                    );
                dataRecords.Add(pvRecord);
                var validE3Dc = solarProduction.HasValue && solarProduction.Value > 0.0;
                validRecords.Add(pvRecord.DirectGeometryFactor > 0 || validE3Dc);
            }

            return new MeteoImportResult(perStationWeatherData, blendedWeatherData, siteId, dataRecords, validRecords, installedPower, periodsPerHour);
        }

        // Import meteo forecast and merge with calculated pvProduction data
        public async Task<MeteoImportResult> ImportMeteoForecastAndCalculatedProduction(
            int folder,
            DateTime firstE3DcTimestamp,
            DateTime lastHistoryTimestamp,
            int forecastDays = 16)
        {
            folder = 1 + (folder - 1) % 2;
            var siteId = folder == 1 ? ListSites.Senn : ListSites.SennV;
            forecastDays = Math.Max(0, Math.Min(forecastDays, 16));

            const int minutesPerPeriod = 15;
            const int periodsPerHour = 60 / minutesPerPeriod;

            var firstTimestamp = lastHistoryTimestamp;
            var lastTimestamp = firstTimestamp.AddDays(forecastDays);

            // Fetch geometry factors
            var (timeStamps, directGeometryFactors, diffuseGeometryFactor, sinSunElevations, installedPower) = await PvProduction(siteId, firstTimestamp, lastTimestamp, minutesPerPeriod, shiftSupportTimeStamps: 0);
            firstTimestamp = timeStamps[0];
            lastTimestamp = timeStamps[^1];

            // Fetch meteo data
            meteoDataLagForecast = 5 * (int)Math.Round((double)meteoDataLagForecast / 5);               // Lag to be applied to forecast data
            var (perStationWeatherData, blendedWeatherData) = await LoadBlendedWeatherForecast(
                stationDictionary,
                timeStamps,
                shiftMeteoTimeStamps: meteoDataOffset + meteoDataLagForecast);

            // Merge data
            var countOfMeteoRecords = blendedWeatherData.Count;
            var dataRecords = new List<PvRecord>();
            var validRecords = new List<bool>();
            for (var i = 0; i < countOfMeteoRecords; i++)
            {
                var recordIndex = i;
                var meteoParam = blendedWeatherData[i];
                var age = (timeStamps[i] - firstE3DcTimestamp).TotalMinutes / minutesPerYear;
                var pvRecord = new PvRecord(
                    timeStamps[i],
                    recordIndex,
                    directGeometryFactors[i],
                    diffuseGeometryFactor,
                    sinSunElevations[i],
                    meteoParam.SunshineDuration ?? 0.0,
                    meteoParam.DirectRadiation ?? 0.0,
                    meteoParam.DirectNormalIrradiance ?? 0.0,
                    meteoParam.GlobalRadiation ?? 0.0,
                    meteoParam.DiffuseRadiation ?? 0.0,
                    meteoParam.Temperature ?? 0.0,
                    meteoParam.WindSpeed ?? 0.0,
                    meteoParam.SnowDepth ?? 0.0,
                    0.0,
                    age,
                    null
                    );

                dataRecords.Add(pvRecord);
                validRecords.Add(false);
            }

            return new MeteoImportResult(perStationWeatherData, blendedWeatherData, siteId, dataRecords, validRecords, installedPower, periodsPerHour);
        }

        // Import e3dc history and computed data only
        public async Task<(string siteId,
            List<PvRecord> dataRecords,
            List<bool> validRecords,
            double installedPower,
            int periodsPerHour)>
            ImportE3DcHistory(int folder, bool meteoTillNow = false)      // 0: downloaded E3Dc history, 1: meteo history till now, 2: including meteo forecast
        {
            var (_, _, siteId, dataRecords, validRecords, installedPower, periodsPerHour) = await ImportE3DcAndMeteoHistory(folder, meteoTillNow: meteoTillNow);

            return (siteId, dataRecords, validRecords, installedPower, periodsPerHour);
        }

        private void InjectDataRecords(
            PvModelParams pvModelParams,
            double installedPower,
            int periodsPerHour,
            List<PvRecord> dataRecords,
            List<bool> validDataRecord,
            List<List<double?>> filteredRadiationSeries,
            List<List<double?>> filteredTemperatureSeries,
            List<List<double?>> filteredWindSpeedSeries,
            List<PvRecordLists> listsDataRecords,
            List<bool> validListsDataRecords
            )
        {
            var countOfListsDataRecords = listsDataRecords.Count;
            var indexFirstDataRecord = 0;                                           // Start from the beginning if list is empty
            if (countOfListsDataRecords > 0)                                        // Continue after last injected record
            {
                var indexLastValidRecord = countOfListsDataRecords - 1;
                var lastInjectedTimestamp = listsDataRecords[indexLastValidRecord].Timestamp;
                // Find first forecast record after last injected timestamp
                for (var i = 0; i < dataRecords.Count; i++)
                {
                    if (dataRecords[i].Timestamp > lastInjectedTimestamp)
                    {
                        indexFirstDataRecord = i;
                        break;
                    }
                }
                // Step packwards to find last valid record
                var indexNewRecord = indexFirstDataRecord;
                while (indexNewRecord > 0 && indexLastValidRecord > 0 && !listsDataRecords[indexLastValidRecord].HasMeteoData())
                {
                    indexNewRecord--;
                    listsDataRecords.RemoveAt(indexLastValidRecord);
                    validListsDataRecords.RemoveAt(indexLastValidRecord);
                    indexLastValidRecord--;
                }
            }

            for (var index = indexFirstDataRecord; index < dataRecords.Count; index++)
            {

                if (indexFirstDataRecord > 0 && index > 368)
                {
                    var debug = 0;
                }

                var record = dataRecords[index];
                var computedPower = record.ComputedPower(pvModelParams, installedPower, periodsPerHour: periodsPerHour);

                // Build lists for the current record, including the base series and the valid reference series
                List<double?> radiationList = [];
                List<double?> temperatureList = [];
                List<double?> windSpeedList = [];
                radiationList.AddRange(filteredRadiationSeries.Select(series => series[index]));
                temperatureList.AddRange(filteredTemperatureSeries.Select(series => series[index]));
                windSpeedList.AddRange(filteredWindSpeedSeries.Select(series => series[index]));

                var listsDataRecord = new PvRecordLists(
                    record.Timestamp,
                    record.Index,
                    [record.MeasuredPower, computedPower],
                    radiationList,
                    temperatureList,
                    windSpeedList
                );

                listsDataRecords.Add(listsDataRecord);
                validListsDataRecords.Add(validDataRecord[index]);
            }

        }

        // Import e3dc history and computed data with selected meteo parameters
        public async Task<(
            string siteId,
            List<PvRecordLists> dataRecords,
            PvRecordLabels dataRecordLabels,
            List<bool> validRecords,
            double installedPower,
            int periodsPerHour)>
            ImportE3DcHistoryAndCalculated(int folder, int displayPeriod = 2)      // 0: downloaded E3Dc history, 1: meteo history till now, 2: including meteo forecast
        {
            List<PvModelParams> pvModelParamsList
                = [
                GetDefaultPriorModelParams(),
                new(
                    0.619,
                    -0.00461,
                    213.7,
                    0.173,
                    0.0139
                ),
                new(             // SennV: elevation 35° 
                    0.478,
                    -0.00096,
                    29.0,
                    0.500,
                    0.00631
                )
            ];
            var pvModelParams = pvModelParamsList[folder];

            // Fetch pvProduction and meteo data
            var (perStationWeatherHistory, 
                blendedWeatherHistory, 
                siteId, 
                dataRecordsHistory, 
                validRecordsHistory, 
                installedPower, 
                periodsPerHour) = await ImportE3DcAndMeteoHistory(folder, meteoTillNow: displayPeriod > 0);

            var (filteredRadiationHistorySeries, filteredRadiationLabels,
                filteredTemperatureHistorySeries, filteredTemperatureLabels,
                filteredWindSpeedHistorySeries, filteredWindSpeedLabels) = FilterAndLabelSeries(perStationWeatherHistory);

            var listsDataRecords = new List<PvRecordLists>();
            var validListsDataRecords = new List<bool>();
            InjectDataRecords(
                pvModelParams,
                installedPower,
                periodsPerHour,
                dataRecordsHistory,
                validRecordsHistory,
                filteredRadiationHistorySeries,
                filteredTemperatureHistorySeries,
                filteredWindSpeedHistorySeries,
                listsDataRecords,
                validListsDataRecords
                );

            // If forecast is requested, extend data with forecast values
            if (displayPeriod == 2)
            {
                var firstE3DcTimestamp = dataRecordsHistory[0].Timestamp;
                var lHistoryTimestamp = dataRecordsHistory[^3].Timestamp;
                var (perStationWeatherForecast, blendedWeatherForecasty, _, dataRecordsForecast, validRecordsForecast, _, _) = await ImportMeteoForecastAndCalculatedProduction(folder, firstE3DcTimestamp, lHistoryTimestamp);

                var (filteredRadiationForecastSeries, _,
                    filteredTemperatureForecastSeries, _,
                    filteredWindSpeedForecastSeries, _) = FilterAndLabelSeries(perStationWeatherForecast);

                InjectDataRecords(
                    pvModelParams,
                    installedPower,
                    periodsPerHour,
                    dataRecordsForecast,
                    validRecordsForecast,
                    filteredRadiationForecastSeries,
                    filteredTemperatureForecastSeries,
                    filteredWindSpeedForecastSeries,
                    listsDataRecords,
                    validListsDataRecords
                    );
            }

            var dataRecordLabels = new PvRecordLabels(
                ["MeasuredPower", "ComputedPower"],
                filteredRadiationLabels,
                filteredTemperatureLabels,
                filteredWindSpeedLabels);

            return (siteId, listsDataRecords, dataRecordLabels, validListsDataRecords, installedPower, periodsPerHour);
        }

        // ***************************************************************************************************************************************************

        // Fetch computed pv production data and geometry factors
        private async Task<(
            List<DateTime> timeStamps,
            List<double> directGeometryFactors,
            double diffuseGeometryFactor,
            List<double> sinSunElevations,
            double installedPower)>
            PvProduction(
            string siteId,
            DateTime startTime,
            DateTime endTime,
            int minutesPerPeriod,
            int shiftSupportTimeStamps = 0)
        {
            var siteModel = await PvSiteModelGetters.GetSiteDataModelAsync(siteId);

            var apiKey = Environment.GetEnvironmentVariable("GOOGLE_ELEVATION_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("Google Elevation API key is not set. Please set the 'GOOGLE_ELEVATION_API_KEY' environment variable.");
            }
            var horizonClient = new HorizonProfileClient(googleApiKey: apiKey!);
            var coordinateProvider = new SampleSiteCoordinateProvider();
            var horizonControlProvider = new SampleSiteHorizonControlProvider();

            var timeStamps = new List<DateTime>();
            var directGeometryFactors = new List<double>();
            var diffuseGeometryFactor = 0.0;
            var sinSunElevations = new List<double>();

            var installedKwP = 0.0;
            for (var year = startTime.Year; year <= endTime.Year; year++)
            {
                var results = await SolarCalculate.ComputePvSiteDetailedProductionFromSiteData(
                    siteModel,
                    horizonClient,
                    coordinateProvider,
                    horizonControlProvider,
                    evaluationYear: year,
                    evaluationStartHour: 4,
                    evaluationEndHour: 22,
                    minutesPerPeriod: minutesPerPeriod,
                    shiftTimeSupport: shiftSupportTimeStamps / 60.0,
                    print: false
                    );

                installedKwP = results.PeakPowerPerRoof.Sum();

                diffuseGeometryFactor = results.DiffuseGeometryFactor;
                for (int i = 0; i < results.TimeStamps.Length; i++)
                {
                    var ts = results.TimeStamps[i];
                    if (ts >= startTime && ts <= endTime && ts.Year == year)
                    {
                        timeStamps.Add(ts);
                        directGeometryFactors.Add(results.DirectGeometryFactors[i]);
                        sinSunElevations.Add(results.SinSunElevations[i]);
                    }
                }
            }

            return (timeStamps, directGeometryFactors, diffuseGeometryFactor, sinSunElevations, installedKwP * 1000);
        }

        // Allocate meteo data into support intervals with linear overlap blending
        private void AllocateMeteoDataContainers(int iSupport, int iMeteo, int supportCount, int meteoInterval, int supportInterval,
            DateTime supportTimeStamp, DateTime meteoTimeStamp, MeteoParameters leftRecord,
            double?[] supportSunshineDuration,
            double?[] supportDirectRadiation,
            double?[] supportDirectNormalIrradiance,
            double?[] supportGlobalRadiation,
            double?[] supportDiffuseRadiation,
            double?[] supportTemperature,
            double?[] supportWindSpeed,
            double?[] supportSnowDepth)
        {
            void AppendValue(double?[] supportArray, int index, double? value, double overLapRatio)
            {
                if (value.HasValue) supportArray[index] = (supportArray[index] ?? 0) + value.Value * overLapRatio;
            }

            var rightOverlapRatio = 1.0;
            var leftOverlapRatio = 0.0;
            var iRight = iSupport;
            var iLeft = iRight - 1;
            if (meteoTimeStamp < supportTimeStamp)
            {
                rightOverlapRatio = (double)(meteoTimeStamp.AddMinutes(meteoInterval) - supportTimeStamp).Minutes / meteoInterval;
                rightOverlapRatio = Math.Max(0.0, Math.Min(1.0, rightOverlapRatio));
                leftOverlapRatio = 1.0 - rightOverlapRatio;
            }
            else if (meteoTimeStamp > supportTimeStamp)
            {
                leftOverlapRatio = (double)(supportTimeStamp.AddMinutes(supportInterval) - meteoTimeStamp).Minutes / meteoInterval;
                leftOverlapRatio = Math.Max(0.0, Math.Min(1.0, leftOverlapRatio));
                rightOverlapRatio = 1.0 - leftOverlapRatio;
                iLeft++;
                iRight++;
            }

            var priorSunshineDuration = leftRecord.SunshineDuration;        // TODO: currently not used
            var priorDirectRadiation = leftRecord.DirectRadiation;
            var priorDirectNormalIrradiance = leftRecord.DirectNormalIrradiance;
            var priorGlobalRadiation = leftRecord.GlobalRadiation;
            var priorDiffuseRadiation = leftRecord.DiffuseRadiation;
            var priorTemperature = leftRecord.Temperature;
            var priorWindSpeed = leftRecord.WindSpeed;
            var priorSnowDepth = leftRecord.SnowDepth;                      // TODO: currently not used

            if (iLeft >= 0 && iLeft < supportCount && leftOverlapRatio > 0)
            {
                AppendValue(supportSunshineDuration, iLeft, priorSunshineDuration, leftOverlapRatio);
                AppendValue(supportDirectRadiation, iLeft, priorDirectRadiation, leftOverlapRatio);
                AppendValue(supportDirectNormalIrradiance, iLeft, priorDirectNormalIrradiance, leftOverlapRatio);
                AppendValue(supportGlobalRadiation, iLeft, priorGlobalRadiation, leftOverlapRatio);
                AppendValue(supportDiffuseRadiation, iLeft, priorDiffuseRadiation, leftOverlapRatio);
                AppendValue(supportTemperature, iLeft, priorTemperature, leftOverlapRatio);
                AppendValue(supportWindSpeed, iLeft, priorWindSpeed, leftOverlapRatio);
                AppendValue(supportSnowDepth, iLeft, priorSnowDepth, leftOverlapRatio);
            }
            if (iRight >= 0 && iRight < supportCount && rightOverlapRatio > 0)
            {
                AppendValue(supportSunshineDuration, iRight, priorSunshineDuration, rightOverlapRatio);
                AppendValue(supportDirectRadiation, iRight, priorDirectRadiation, rightOverlapRatio);
                AppendValue(supportDirectNormalIrradiance, iRight, priorDirectNormalIrradiance, rightOverlapRatio);
                AppendValue(supportGlobalRadiation, iRight, priorGlobalRadiation, rightOverlapRatio);
                AppendValue(supportDiffuseRadiation, iRight, priorDiffuseRadiation, rightOverlapRatio);
                AppendValue(supportTemperature, iRight, priorTemperature, rightOverlapRatio);
                AppendValue(supportWindSpeed, iRight, priorWindSpeed, rightOverlapRatio);
                AppendValue(supportSnowDepth, iRight, priorSnowDepth, rightOverlapRatio);
            }
        }

        // Compute normalized weights for blending
        private double[] GetWeights(List<double> weightList, int count)
        {
            double[] weights = new double[count];
            var copyCount = Math.Min(count, weightList.Count);
            for (var i = 0; i < copyCount; i++)
            {
                weights[i] = weightList[i] > 0 ? weightList[i] : 0.0;
            }
            var totalWeight = weights.Sum();
            if (totalWeight > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    weights[i] /= totalWeight;
                }
            }
            else
            {
                weights[0] = 1.0;
            }
            return weights;
        }

        // Aggregation helper for historical and forecast data: Process data from a single station and accumulate weighted sums
        private new List<MeteoParameters>
            ProcessStationData(
            int stationIndex,
            int countStations,
            int supportCount,
            int supportInterval,
            int meteoCount,
            int meteoInterval,
            DateTime firstSupportTimestamp,
            DateTime upperBound,
            List<DateTime> supportTimeStamps,
            List<DateTime> alignedMeteoTimeStamps,
            List<MeteoParameters> meteoParametersList,
            double[] weightSunshineDuration,
            double[] weightDirectRadiation,
            double[] weightDirectNormalIrradiance,
            double[] weightGlobalRadiation,
            double[] weightDiffuseRadiation,
            double[] weightTemperature,
            double[] weightWindSpeed,
            double[] weightSnowDepth,
            double[] weightedSumSupportSunshineDuration,
            double[] weightedSumSupportDirectRadiation,
            double[] weightedSumSupportDirectNormalIrradiance,
            double[] weightedSumSupportGlobalRadiation,
            double[] weightedSumSupportDiffuseRadiation,
            double[] weightedSumSupportTemperature,
            double[] weightedSumSupportWindSpeed,
            double[] weightedSumSupportSnowDepth,
            double[] sumSupportDirectRadiation,
            double[] squaredSumSupportDirectRadiation
        )
        {
            var iSupport = 0;
            var iMeteo = 0;
            var leftRecord = meteoParametersList[0];
            var supportSunshineDuration = new double?[supportCount];
            var supportDirectRadiation = new double?[supportCount];
            var supportDirectNormalIrradiance = new double?[supportCount];
            var supportGlobalRadiation = new double?[supportCount];
            var supportDiffuseRadiation = new double?[supportCount];
            var supportTemperature = new double?[supportCount];
            var supportWindSpeed = new double?[supportCount];
            var supportSnowDepth = new double?[supportCount];
            while (iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= firstSupportTimestamp)
            {
                iMeteo++;
            }
            while (iSupport < supportCount && iMeteo < meteoCount && alignedMeteoTimeStamps[iMeteo] < upperBound)
            {
                AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
                    supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], meteoParametersList[iMeteo],
                    supportSunshineDuration, supportDirectRadiation, supportDirectNormalIrradiance, supportGlobalRadiation, supportDiffuseRadiation,
                    supportTemperature, supportWindSpeed, supportSnowDepth);
                iSupport++;
                while (iSupport < supportCount && iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= supportTimeStamps[iSupport])
                {
                    iMeteo++;
                    AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
                        supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], meteoParametersList[iMeteo],
                        supportSunshineDuration, supportDirectRadiation, supportDirectNormalIrradiance, supportGlobalRadiation, supportDiffuseRadiation,
                        supportTemperature, supportWindSpeed, supportSnowDepth);
                }
                iMeteo++;
            }

            var weatherParameters =
                new List<MeteoParameters>();
            for (var i = 0; i < supportCount; i++)
            {
                weatherParameters.Add(new MeteoParameters(
                    supportTimeStamps[i],
                    TimeSpan.FromMinutes(supportInterval),
                    supportSunshineDuration[i],
                    supportDirectRadiation[i],
                    supportDirectNormalIrradiance[i],
                    supportGlobalRadiation[i],
                    supportDiffuseRadiation[i],
                    supportTemperature[i],
                    supportWindSpeed[i],
                    null,
                    supportSnowDepth[i],
                    null,
                    null));

                var sunshineDur = supportSunshineDuration[i] ?? 0.0;
                var directRad = supportDirectRadiation[i] ?? 0.0;
                var direNormIrr = supportDirectNormalIrradiance[i] ?? 0.0;
                var globalRad = supportGlobalRadiation[i] ?? 0.0;
                var diffuseRad = supportDiffuseRadiation[i] ?? 0.0;
                var ambTemp = supportTemperature[i] ?? 0.0;
                var windSpeed = supportWindSpeed[i] ?? 0.0;
                var snowDepth = supportSnowDepth[i] ?? 0.0;
                weightedSumSupportSunshineDuration[i] += sunshineDur * weightSunshineDuration[stationIndex];
                weightedSumSupportDirectRadiation[i] += directRad * weightDirectRadiation[stationIndex];                     // Historical records
                weightedSumSupportDirectNormalIrradiance[i] += direNormIrr * weightDirectNormalIrradiance[stationIndex];       // Forecast records
                weightedSumSupportGlobalRadiation[i] += globalRad * weightGlobalRadiation[stationIndex];                     // Historical records
                weightedSumSupportDiffuseRadiation[i] += diffuseRad * weightDiffuseRadiation[stationIndex];
                weightedSumSupportTemperature[i] += ambTemp * weightTemperature[stationIndex];
                weightedSumSupportWindSpeed[i] += windSpeed * weightWindSpeed[stationIndex];
                weightedSumSupportSnowDepth[i] += snowDepth * weightSnowDepth[stationIndex];

                sumSupportDirectRadiation[i] += directRad;
                squaredSumSupportDirectRadiation[i] += directRad * directRad;
            }

            return weatherParameters;
        }

        private List<MeteoParameters>
            GetBlendedMeteoParameters(
            int supportCount,
            List<DateTime> supportTimeStamps,
            double[] weightedSumSupportSunshineDuration,
            double[] weightedSumSupportDirectRadiation,
            double[] weightedSumSupportDirectNormalIrradiance,
            double[] weightedSumSupportGlobalRadiation,
            double[] weightedSumSupportDiffuseRadiation,
            double[] weightedSumSupportTemperature,
            double[] weightedSumSupportWindSpeed,
            double[] weightedSumSupportSnowDepth,
            double[] sumSupportDirectRadiation,
            double[] squaredSumSupportDirectRadiation)
        {
            var blendedWeatherData =
                    new List<MeteoParameters>();
            var timeInterval = (supportTimeStamps[1] - supportTimeStamps[0]).Minutes;

            for (var i = 0; i < supportCount; i++)
            {
                var directRadiationVariance = radiationVarianceMaxVariance;
                if (selectedStationsIdList.Count > 1)
                {
                    var E1i = sumSupportDirectRadiation[i] / selectedStationsIdList.Count; ;
                    var E2i = squaredSumSupportDirectRadiation[i] / selectedStationsIdList.Count;
                    directRadiationVariance = radiationBaselineVariance + (E2i - E1i * E1i) * selectedStationsIdList.Count / (selectedStationsIdList.Count - 1);
                }

                blendedWeatherData.Add(new MeteoParameters(
                    supportTimeStamps[i],
                    TimeSpan.FromMinutes(timeInterval),
                    weightedSumSupportSunshineDuration[i],
                    weightedSumSupportDirectRadiation[i],
                    weightedSumSupportDirectNormalIrradiance[i],
                    weightedSumSupportGlobalRadiation[i],
                    weightedSumSupportDiffuseRadiation[i],
                    weightedSumSupportTemperature[i],
                    weightedSumSupportWindSpeed[i],
                    null,
                    weightedSumSupportSnowDepth[i],
                    null,
                    null,
                    directRadiationVariance));
            }

            return blendedWeatherData;
        }

        // Load meteo history and blend data from selected stations
        private (List<StationMeteoData> stationsWeatherdata, List<MeteoParameters> blendedWeatherData) LoadBlendedWeatherHistory(
            Dictionary<string, WeightMeteoParameters> stationDictionary,
            List<DateTime> supportTimeStamps,
            int shiftMeteoTimeStamps = 60)                 // shift in minutes UTC -> local time
        {
            var now = DateTime.Now;
            var supportCount = supportTimeStamps.Count;
            var firstSupportTimestamp = supportTimeStamps[0];
            var secondSupportTimestamp = supportTimeStamps[1];
            var lastSupportTimestamp = supportTimeStamps[^1];
            var currentSupportTimestamp = new DateTime(now.Year, now.Month, now.Day, now.Hour, (now.Minute / 15) * 15, 0).AddMinutes(-15);
            var supportInterval = (secondSupportTimestamp - firstSupportTimestamp).Minutes;
            var upperBound = lastSupportTimestamp.AddMinutes(supportInterval);

            // Initialize list with valid ground stations
            MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetAllGroundStations();
            // Load station metadata
            var groundStationsMetaDict = StationMetaImporter.Import(MeteoSwissConstants.GroundStationsMetaFile);
            var firstYear = firstSupportTimestamp.AddMinutes(-shiftMeteoTimeStamps).Year;
            var lastYear = lastSupportTimestamp.AddMinutes(-shiftMeteoTimeStamps).Year;

            var (weightSunshineDuration,
                weightDirectRadiation,
                weightDirectNormalIrradiance,
                weightGlobalRadiation,
                weightDiffuseRadiation,
                weightTemperature,
                weightWindSpeed,
                weightSnowDepth) = GetStationWeightArrays(stationDictionary);

            var weightedSumSupportSunshineDuration = new double[supportCount];
            var weightedSumSupportDirectRadiation = new double[supportCount];
            var weightedSumSupportDirectNormalIrradiance = new double[supportCount];
            var weightedSumSupportGlobalRadiation = new double[supportCount];
            var weightedSumSupportDiffuseRadiation = new double[supportCount];
            var weightedSumSupportTemperature = new double[supportCount];
            var weightedSumSupportWindSpeed = new double[supportCount];
            var weightedSumSupportSnowDepth = new double[supportCount];

            var perStationWeatherParameters = new List<StationMeteoData>();
            var sumSupportDirectRadiation = new double[supportCount];
            var squaredSumSupportDirectRadiation = new double[supportCount];
            var stationIndex = -1;
            foreach (var stationId in selectedStationsIdList)
            {
                stationIndex++;

                var meteoParametersList = MeteoAggregator.GetFilteredMeteoParametersRecords(
                    stationId,
                    groundStationsMetaDict[stationId],
                    firstYear,
                    lastYear,
                    "t",
                    includeRecent: true,
                    includeNow: true
                    );

                var alignedMeteoTimeStamps = meteoParametersList.Select(r => r.Time.AddMinutes(shiftMeteoTimeStamps)).ToList();

                var meteoCount = alignedMeteoTimeStamps.Count;
                var firstMeteoTimestamp = alignedMeteoTimeStamps[0];
                var secondMeteoTimestamp = alignedMeteoTimeStamps[1];
                var lastMeteoTimestamp = alignedMeteoTimeStamps[^1];
                var meteoInterval = (secondMeteoTimestamp - firstMeteoTimestamp).Minutes;

                if (meteoInterval > supportInterval)
                {
                    throw new Exception("Meteo interval is larger than support interval.");
                }

                var weatherParameters = ProcessStationData(
                    stationIndex,
                    stationDictionary.Count,
                    supportCount,
                    supportInterval,
                    meteoCount,
                    meteoInterval,
                    firstSupportTimestamp,
                    upperBound,
                    supportTimeStamps,
                    alignedMeteoTimeStamps,
                    meteoParametersList,
                    weightSunshineDuration,
                    weightDirectRadiation,
                    weightDirectNormalIrradiance,
                    weightGlobalRadiation,
                    weightDiffuseRadiation,
                    weightTemperature,
                    weightWindSpeed,
                    weightSnowDepth,
                    weightedSumSupportSunshineDuration,
                    weightedSumSupportDirectRadiation,
                    weightedSumSupportDirectNormalIrradiance,
                    weightedSumSupportGlobalRadiation,
                    weightedSumSupportDiffuseRadiation,
                    weightedSumSupportTemperature,
                    weightedSumSupportWindSpeed,
                    weightedSumSupportSnowDepth,
                    sumSupportDirectRadiation,
                    squaredSumSupportDirectRadiation);

                perStationWeatherParameters.Add(new StationMeteoData(stationId, weatherParameters));
            }

            var blendedWeatherData = GetBlendedMeteoParameters(
                supportCount,
                supportTimeStamps,
                weightedSumSupportSunshineDuration,
                weightedSumSupportDirectRadiation,
                weightedSumSupportDirectNormalIrradiance,
                weightedSumSupportGlobalRadiation,
                weightedSumSupportDiffuseRadiation,
                weightedSumSupportTemperature,
                weightedSumSupportWindSpeed,
                weightedSumSupportSnowDepth,
                sumSupportDirectRadiation,
                squaredSumSupportDirectRadiation);

            return (perStationWeatherParameters, blendedWeatherData);
        }

        private async Task<(
            List<StationMeteoData> stationsWeatherdata,
            List<MeteoParameters> blendedWeatherData)>
            LoadBlendedWeatherForecast(
            Dictionary<string, WeightMeteoParameters> stationDictionary,
            List<DateTime> supportTimeStamps,
            int shiftMeteoTimeStamps = 60)                 // shift in minutes UTC -> local time
        {
            var now = DateTime.Now;
            var supportCount = supportTimeStamps.Count;
            var firstSupportTimestamp = supportTimeStamps[0];
            var secondSupportTimestamp = supportTimeStamps[1];
            var lastSupportTimestamp = supportTimeStamps[^1];
            var currentSupportTimestamp = new DateTime(now.Year, now.Month, now.Day, now.Hour, (now.Minute / 15) * 15, 0).AddMinutes(-15);
            var supportInterval = (secondSupportTimestamp - firstSupportTimestamp).Minutes;
            var upperBound = lastSupportTimestamp.AddMinutes(supportInterval);

            // Initialize list with valid ground stations
            MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetAllGroundStations();
            // Load station metadata
            var groundStationsMetaDict = StationMetaImporter.Import(MeteoSwissConstants.GroundStationsMetaFile);
            var firstYear = firstSupportTimestamp.AddMinutes(-shiftMeteoTimeStamps).Year;
            var lastYear = lastSupportTimestamp.AddMinutes(-shiftMeteoTimeStamps).Year;

            // Compute normalized weights
            var (weightSunshineDuration,
                weightDirectRadiation,
                weightDirectNormalIrradiance,
                weightGlobalRadiation,
                weightDiffuseRadiation,
                weightTemperature,
                weightWindSpeed,
                weightSnowDepth) = GetStationWeightArrays(stationDictionary);

            var weightedSumSupportSunshineDuration = new double[supportCount];
            var weightedSumSupportDirectRadiation = new double[supportCount];
            var weightedSumSupportDirectNormalIrradiance = new double[supportCount];
            var weightedSumSupportGlobalRadiation = new double[supportCount];
            var weightedSumSupportDiffuseRadiation = new double[supportCount];
            var weightedSumSupportTemperature = new double[supportCount];
            var weightedSumSupportWindSpeed = new double[supportCount];
            var weightedSumSupportSnowDepth = new double[supportCount];

            var sumSupportDirectRadiation = new double[supportCount];
            var squaredSumSupportDirectRadiation = new double[supportCount];

            // Fetch forecasts for all stations
            var forecastClient = new WeatherForecastClient();
            var blendedForecastPerStation = new List<List<MeteoParameters>>();
            foreach (var stationId in selectedStationsIdList)
            {
                var longCast = await forecastClient.Get16DayMeteoParametersByStationIdAsync(stationId);
                var midCast = await forecastClient.Get7DayMeteoParametersByStationIdAsync(stationId);
                var nowCast = await forecastClient.GetNowcast15MinuteMeteoParametersByStationIdAsync(stationId);
                blendedForecastPerStation.Add(CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast));
            }

            // Identify overlapping forecast periods
            var stationForecast = blendedForecastPerStation[0];
            var startTimestamp = stationForecast[0].Time;
            var endTimestamp = stationForecast[^1].Time;
            for (int i = 1; i < blendedForecastPerStation.Count; i++)
            {
                stationForecast = blendedForecastPerStation[i];
                startTimestamp = (startTimestamp >= stationForecast[0].Time) ? startTimestamp : stationForecast[0].Time;
                endTimestamp = (endTimestamp <= stationForecast[^1].Time) ? endTimestamp : stationForecast[^1].Time;
            }

            // Blend data from all stations using the algorithm for historical data
            var perStationWeatherParameters = new List<StationMeteoData>();
            var stationIndex = -1;
            foreach (var stationId in selectedStationsIdList)
            {
                stationIndex++;
                var blendedForecast = blendedForecastPerStation[stationIndex];

                var meteoParametersList = new List<MeteoParameters>();
                foreach (var record in blendedForecast)
                {
                    if (record.Time < startTimestamp || record.Time > endTimestamp)
                    {
                        continue;
                    }
                    meteoParametersList.Add(record);
                }
                var alignedMeteoTimeStamps = meteoParametersList.Select(r => r.Time.AddMinutes(shiftMeteoTimeStamps)).ToList();

                var meteoCount = alignedMeteoTimeStamps.Count;
                var firstMeteoTimestamp = alignedMeteoTimeStamps[0];
                var secondMeteoTimestamp = alignedMeteoTimeStamps[1];
                var lastMeteoTimestamp = alignedMeteoTimeStamps[^1];
                var meteoInterval = (secondMeteoTimestamp - firstMeteoTimestamp).Minutes;

                if (meteoInterval > supportInterval)
                {
                    throw new Exception("Meteo interval is larger than support interval.");
                }

                var weatherParameters = ProcessStationData(
                    stationIndex,
                    stationDictionary.Count,
                    supportCount,
                    supportInterval,
                    meteoCount,
                    meteoInterval,
                    firstSupportTimestamp,
                    upperBound,
                    supportTimeStamps,
                    alignedMeteoTimeStamps,
                    meteoParametersList,
                    weightSunshineDuration,
                    weightDirectRadiation,
                    weightDirectNormalIrradiance,
                    weightGlobalRadiation,
                    weightDiffuseRadiation,
                    weightTemperature,
                    weightWindSpeed,
                    weightSnowDepth,
                    weightedSumSupportSunshineDuration,
                    weightedSumSupportDirectRadiation,
                    weightedSumSupportDirectNormalIrradiance,
                    weightedSumSupportGlobalRadiation,
                    weightedSumSupportDiffuseRadiation,
                    weightedSumSupportTemperature,
                    weightedSumSupportWindSpeed,
                    weightedSumSupportSnowDepth,
                    sumSupportDirectRadiation,
                    squaredSumSupportDirectRadiation);

                perStationWeatherParameters.Add(new StationMeteoData(stationId, weatherParameters));
            }

            var blendedWeatherData = GetBlendedMeteoParameters(
                supportCount,
                supportTimeStamps,
                weightedSumSupportSunshineDuration,
                weightedSumSupportDirectRadiation,
                weightedSumSupportDirectNormalIrradiance,
                weightedSumSupportGlobalRadiation,
                weightedSumSupportDiffuseRadiation,
                weightedSumSupportTemperature,
                weightedSumSupportWindSpeed,
                weightedSumSupportSnowDepth,
                sumSupportDirectRadiation,
                squaredSumSupportDirectRadiation);
            return (perStationWeatherParameters, blendedWeatherData);
        }

        public static (
            List<List<double?>> RadiationSeries,
            List<string> RadiationLabels,
            List<List<double?>> TemperatureSeries,
            List<string> TemperatureLabels,
            List<List<double?>> WindSpeedSeries,
            List<string> WindSpeedLabels
        ) FilterAndLabelSeries(
            List<StationMeteoData> perStationWeatherData)
        {
            var radiationSeries = new List<List<double?>>();
            var radiationLabels = new List<string>();
            var temperatureSeries = new List<List<double?>>();
            var temperatureLabels = new List<string>();
            var windSpeedSeries = new List<List<double?>>();
            var windSpeedLabels = new List<string>();

            foreach (var stationData in perStationWeatherData)
            {
                var stationId = stationData.StationId;
                var stationDataRecords = stationData.WeatherData;
                var validParameters = stationDataRecords[0].GetValidMeteoParameters;  // Use first record to check valid parameters

                if (validParameters.HasValidGlobalRadiation)
                {
                    radiationSeries.Add(stationDataRecords.Select(d => d.GetValue(MeteoParameterType.GlobalRadiation)).ToList());
                    radiationLabels.Add($"Global_{stationId}");
                }
                if (validParameters.HasValidDiffuseRadiation)
                {
                    radiationSeries.Add(stationDataRecords.Select(d => d.GetValue(MeteoParameterType.DiffuseRadiation)).ToList());
                    radiationLabels.Add($"Diffuse_{stationId}");
                }
                if (validParameters.HasValidTemperature)
                {
                    temperatureSeries.Add(stationDataRecords.Select(d => d.GetValue(MeteoParameterType.Temperature)).ToList());
                    temperatureLabels.Add($"Temperature_{stationId}");
                }
                if (validParameters.HasValidWindSpeed)
                {
                    windSpeedSeries.Add(stationDataRecords.Select(d => d.GetValue(MeteoParameterType.WindSpeed)).ToList());
                    windSpeedLabels.Add($"WindSpeed_{stationId}");
                }
            }

            return (radiationSeries, radiationLabels, temperatureSeries, temperatureLabels, windSpeedSeries, windSpeedLabels);
        }

        // Helper method to get weight arrays for all selected stations
        private (
            double[] weightSunshineDuration,
            double[] weightDirectRadiation,
            double[] weightDirectNormalIrradiance,
            double[] weightGlobalRadiation,
            double[] weightDiffuseRadiation,
            double[] weightTemperature,
            double[] weightWindSpeed,
            double[] weightSnowDepth)
            GetStationWeightArrays(Dictionary<string, WeightMeteoParameters> stationDictionary)
        {
            // Get weight lists
            var weightSunshineDurationList = new List<double>();
            var weightDirectRadiationList = new List<double>();
            var weightDirectNormalIrradianceList = new List<double>();
            var weightGlobalRadiationList = new List<double>();
            var weightDiffuseRadiationList = new List<double>();
            var weightTemperatureList = new List<double>();
            var weightWindSpeedList = new List<double>();
            var weightSnowDepthList = new List<double>();
            foreach (var stationId in stationDictionary.Keys)
            {
                var weights = stationDictionary[stationId];
                weightSunshineDurationList.Add(weights.WeightSunshineDuration);
                weightDirectRadiationList.Add(weights.WeightDirectRadiation);
                weightDirectNormalIrradianceList.Add(weights.WeightDirectNormalIrradiance);
                weightGlobalRadiationList.Add(weights.WeightGlobalRadiation);
                weightDiffuseRadiationList.Add(weights.WeightDiffuseRadiation);
                weightTemperatureList.Add(weights.WeightTemperature);
                weightWindSpeedList.Add(weights.WeightWindSpeed);
                weightSnowDepthList.Add(weights.WeightSnowDepth);
            }

            // Compute normalized weights
            var countStations = stationDictionary.Count;
            var weightSunshineDuration = GetWeights(weightSunshineDurationList, countStations);
            var weightDirectRadiation = GetWeights(weightDirectRadiationList, countStations);
            var weightDirectNormalIrradiance = GetWeights(weightDirectNormalIrradianceList, countStations);
            var weightGlobalRadiation = GetWeights(weightGlobalRadiationList, countStations);
            var weightDiffuseRadiation = GetWeights(weightDiffuseRadiationList, countStations);
            var weightTemperature = GetWeights(weightTemperatureList, countStations);
            var weightWindSpeed = GetWeights(weightWindSpeedList, countStations);
            var weightSnowDepth = GetWeights(weightSnowDepthList, countStations);

            return (weightSunshineDuration,
                    weightDirectRadiation,
                    weightDirectNormalIrradiance,
                    weightGlobalRadiation,
                    weightDiffuseRadiation,
                    weightTemperature,
                    weightWindSpeed,
                    weightSnowDepth);
        }
    }
}
