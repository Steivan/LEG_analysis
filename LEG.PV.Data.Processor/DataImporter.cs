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
using LEG.PV.Core.Models;
using static LEG.PV.Core.Models.PvDataClass;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

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

        public List<MeteoParameterType> MeteoParameterTypeList  { get; set; } = new()
        {
            //MeteoParameterType.SunshineDuration,
            //MeteoParameterType.DirectRadiation,
            //MeteoParameterType.DirectNormalIrradiance,
            MeteoParameterType.GlobalRadiation,
            MeteoParameterType.DiffuseRadiation,
            MeteoParameterType.Temperature,
            MeteoParameterType.WindSpeed,
            //MeteoParameterType.WindDirection,
            MeteoParameterType.SnowDepth,
            MeteoParameterType.RelativeHumidity,
            MeteoParameterType.DewPoint
        };
        public Dictionary<MeteoParameterType, double?[]> MeteoValuesArrays { get; set; } = new();
        public Dictionary<MeteoParameterType, double[]> WeightMeteoArrays { get; set; } = new();
        public Dictionary<MeteoParameterType, double[]> WeightedSumMeteoValuesArrays { get; set; } = new();

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
                WeightWindDirection = 1.0,
                WeightSnowDepth = 1.0,
                WeightRelativeHumidity = 1.0,
                WeightDewPoint = 1.0,
                WeightRadiationVariance = 1.0
            } },
            { "KLO", new WeightMeteoParameters { 
                WeightSunshineDuration = 1.0, 
                WeightDirectRadiation = 1.0, 
                WeightDirectNormalIrradiance = 1.0,
                WeightGlobalRadiation = 1.0,
                WeightDiffuseRadiation = 1.0,
                WeightTemperature = 1.0,
                WeightWindSpeed = 1.0,
                WeightWindDirection = 1.0,
                WeightSnowDepth = 1.0,
                WeightRelativeHumidity = 1.0,
                WeightDewPoint = 1.0,
                WeightRadiationVariance = 1.0
            } },
            { "HOE", new WeightMeteoParameters { 
                WeightSunshineDuration = 1.0, 
                WeightDirectRadiation = 1.0, 
                WeightDirectNormalIrradiance = 1.0, 
                WeightGlobalRadiation = 1.0, 
                WeightDiffuseRadiation = 0.0, // HOE station has no diffuse radiation data
                WeightTemperature = 0.0,
                WeightWindSpeed = 0.0,
                WeightWindDirection = 0.0,
                WeightSnowDepth = 0.0,
                WeightRelativeHumidity = 0.0,
                WeightDewPoint = 0.0,
                WeightRadiationVariance = 1.0
            } },
            { "UEB", new WeightMeteoParameters {
                WeightSunshineDuration = 1.0,
                WeightDirectRadiation = 1.0,
                WeightDirectNormalIrradiance = 1.0,
                WeightGlobalRadiation = 1.0,
                WeightDiffuseRadiation = 1.0,
                WeightTemperature = 0.0,
                WeightWindSpeed = 0.0,
                WeightWindDirection = 0.0,
                WeightSnowDepth = 0.0,
                WeightRelativeHumidity = 0.0,
                WeightDewPoint = 0.0,
                WeightRadiationVariance = 1.0
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
            var (timeStamps, geometryFactors, installedPower) = await PvProduction(siteId, firstTimestamp, lastTimestamp, minutesPerPeriod, shiftSupportTimeStamps: 0);
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
                var weight = 1.0 / (1E-6 + meteoParam.RadiationVariance ?? (double.MaxValue - 1E-6));
                double? solarProduction = i < countOfE3DcRecords ? pvDataRecords[i].SolarProduction : null;
                if (!solarProduction.HasValue)
                {
                    weight = 0.0;
                }
                var age = (timeStamps[i] - firstE3DcTimestamp).TotalMinutes / minutesPerYear;
                var pvRecord = new PvRecord(
                    timeStamps[i],
                    recordIndex,                            // TODO: pvDataRecord.Index,
                    geometryFactors[i],
                    meteoParam,
                    weight,
                    age,
                    solarProduction
                    );
                dataRecords.Add(pvRecord);
                var validE3Dc = solarProduction.HasValue && solarProduction.Value > 0.0;
                validRecords.Add(pvRecord.SolarGeometry.HasIrradiance || validE3Dc);
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
            var (timeStamps, geometryFactors, installedPower) = await PvProduction(siteId, firstTimestamp, lastTimestamp, minutesPerPeriod, shiftSupportTimeStamps: 0);
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
                //var meteoParam = blendedWeatherData[i];
                var age = (timeStamps[i] - firstE3DcTimestamp).TotalMinutes / minutesPerYear;
                var pvRecord = new PvRecord(
                    timeStamps[i],
                    recordIndex,
                    geometryFactors[i],
                    blendedWeatherData[i],
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
                var pvDataRecord = record.GetPvResidualsRecord(pvModelParams, installedPower, periodsPerHour: periodsPerHour);
                var computedPower = pvDataRecord.ComputedPower;
                var residualsRecord = pvDataRecord.UnexplainedFractionLossRecord;
                var referenceResidual = computedPower.PowerG / (installedPower / periodsPerHour);
                var hasCalculated = pvDataRecord.HasCalculated;

                // Build lists for the current record, including the base series and the valid reference series
                List<double?> radiationList = [];
                List<double?> residualsList = [];
                List<double?> temperatureList = [];
                List<double?> windSpeedList = [];
                radiationList.AddRange(filteredRadiationSeries.Select(series => series[index]));
                residualsList.AddRange(filteredRadiationSeries.Select(series => series[index]));
                temperatureList.AddRange(filteredTemperatureSeries.Select(series => series[index]));
                windSpeedList.AddRange(filteredWindSpeedSeries.Select(series => series[index]));

                var listsDataRecord = new PvRecordLists(
                    record.Timestamp,
                    record.Index,
                    [record.MeasuredPower, computedPower.PowerGR, computedPower.PowerGRTW, computedPower.PowerGRTWSF],
                    [referenceResidual, residualsRecord.PowerGR, residualsRecord.PowerGRTW, residualsRecord.PowerGRTWSF],
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
                PvPriorConfig.GetAllPriorsMeans(),      // Default priors used for index=0
                new(
                    etha: 0.571,
                    gamma: -0.0056,
                    u0: 200,
                    u1: 0.001,
                    lDegr: 0.0119,
                    dSnow: 18.0,
                    lambdaAFog: -0.468,
                    bFog: 0.956,
                    lambdaKFog: 1.92
                ),
                new(             // SennV: elevation 35° 
                    etha: 0.464,
                    gamma: -0.0003,
                    u0: 200,
                    u1: 0.001,
                    lDegr: 0.0085,
                    dSnow: 18.0,
                    lambdaAFog: -0.108,
                    bFog: 1.092,
                    lambdaKFog: 1.98
                ),
                new(                                    // initial calibration without Snow/Fog
                    etha: 0.619,
                    gamma: -0.00461,
                    u0: 213.7,
                    u1: 0.173,
                    lDegr: 0.0139,
                    dSnow: 15.0,
                    lambdaAFog: 2.0,
                    bFog: 1.0,
                    lambdaKFog: 2.0
                ),
                new(             // SennV: elevation 35° 
                    etha: 0.478,
                    gamma: -0.00096,
                    u0: 29.0,
                    u1: 0.500,
                    lDegr: 0.00631,
                    dSnow: 2.0,
                    lambdaAFog: 2.0,
                    bFog: 1.0,
                    lambdaKFog: 2.0
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
                ["MeasuredPower", "PowerGR", "PowerGRTW", "PowerGRTWSF"],
                ["Reference", "UflGR", "UflGRTW", "UflGRTWSF"],
                filteredRadiationLabels,
                filteredTemperatureLabels,
                filteredWindSpeedLabels);

            return (siteId, listsDataRecords, dataRecordLabels, validListsDataRecords, installedPower, periodsPerHour);
        }

        // ***************************************************************************************************************************************************

        // Fetch computed pv production data and geometry factors
        private async Task<(
            List<DateTime> timeStamps,
            List<PvSolarGeometry> geometryFactors,
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
            var geometryFactors = new List<PvSolarGeometry>(); 

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

                //diffuseGeometryFactor = results.DiffuseGeometryFactor;
                for (int i = 0; i < results.TimeStamps.Length; i++)
                {
                    var ts = results.TimeStamps[i];
                    if (ts >= startTime && ts <= endTime && ts.Year == year)
                    {
                        timeStamps.Add(ts);
                        geometryFactors.Add(new PvSolarGeometry(
                            results.DirectGeometryFactors[i],
                            results.DiffuseGeometryFactor,
                            results.SinSunElevations[i]
                            ));
                    }
                }
            }

            return (timeStamps, geometryFactors, installedKwP * 1000);
        }

        // Allocate meteo data into support intervals with linear overlap blending
        private void AllocateMeteoDataContainers(int iSupport, int iMeteo, int supportCount, int meteoInterval, int supportInterval,
            DateTime supportTimeStamp, DateTime meteoTimeStamp, MeteoParameters leftRecord)
        {
            void AppendValue(MeteoParameterType meteoParameter, int index, double? value, double overLapRatio)
            {
                if (value.HasValue)
                    MeteoValuesArrays[meteoParameter][index] = (MeteoValuesArrays[meteoParameter][index] ?? 0) + value * overLapRatio;
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

            if (iLeft >= 0 && iLeft < supportCount && leftOverlapRatio > 0)
            {
                foreach (var meteoParameterType in MeteoParameterTypeList)
                {
                    AppendValue(meteoParameterType, iLeft, leftRecord.ValueFromType(meteoParameterType), leftOverlapRatio);
                }
            }
            if (iRight >= 0 && iRight < supportCount && rightOverlapRatio > 0)
            {
                foreach (var meteoParameterType in MeteoParameterTypeList)
                {
                    AppendValue(meteoParameterType, iRight, leftRecord.ValueFromType(meteoParameterType), rightOverlapRatio);
                }
            }
        }

        // Compute normalized weights for blending
        private void NormalizeMeteoWeightArray(MeteoParameterType parameterType, int count)
        {
            var weights = WeightMeteoArrays[parameterType];
            var copyCount = Math.Min(count, weights.Length);
            var normalizedWeights = new double[count];
            for (var i = 0; i < copyCount; i++)
            {
                normalizedWeights[i] = weights[i] > 0 ? weights[i] : 0.0;
            }
            var totalWeight = normalizedWeights.Sum();
            if (totalWeight > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    normalizedWeights[i] /= totalWeight;
                }
            }
            else
            {
                normalizedWeights[0] = 1.0;
            }

            WeightMeteoArrays[parameterType] = normalizedWeights;
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
            double[] sumSupportGlobalRadiation,
            double[] squaredSumSupportGlobalRadiation
        )
        {
            var iSupport = 0;
            var iMeteo = 0;
            var leftRecord = meteoParametersList[0];

            foreach (var meteoParameterType in MeteoParameterTypeList)
            {
                MeteoValuesArrays[meteoParameterType] = new double?[supportCount];
            }

            while (iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= firstSupportTimestamp)
            {
                iMeteo++;
            }
            while (iSupport < supportCount && iMeteo < meteoCount && alignedMeteoTimeStamps[iMeteo] < upperBound)
            {
                AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
                    supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], meteoParametersList[iMeteo]);
                iSupport++;
                while (iSupport < supportCount && iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= supportTimeStamps[iSupport])
                {
                    iMeteo++;
                    AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
                        supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], meteoParametersList[iMeteo]);
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
                    MeteoParameterTypeList.Contains(MeteoParameterType.SunshineDuration) ? MeteoValuesArrays[MeteoParameterType.SunshineDuration][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.DirectRadiation) ? MeteoValuesArrays[MeteoParameterType.DirectRadiation][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.DirectNormalIrradiance) ? MeteoValuesArrays[MeteoParameterType.DirectNormalIrradiance][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.GlobalRadiation) ? MeteoValuesArrays[MeteoParameterType.GlobalRadiation][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.DiffuseRadiation) ? MeteoValuesArrays[MeteoParameterType.DiffuseRadiation][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.Temperature) ? MeteoValuesArrays[MeteoParameterType.Temperature][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.WindSpeed) ? MeteoValuesArrays[MeteoParameterType.WindSpeed][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.WindDirection) ? MeteoValuesArrays[MeteoParameterType.WindDirection][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.SnowDepth) ? MeteoValuesArrays[MeteoParameterType.SnowDepth][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.RelativeHumidity) ? MeteoValuesArrays[MeteoParameterType.RelativeHumidity][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.DewPoint) ? MeteoValuesArrays[MeteoParameterType.DewPoint][i] : null
                    )
                    );

                foreach (var meteoParameterType in MeteoParameterTypeList)
                {
                    //if (double.IsNaN(MeteoValuesArrays[meteoParameterType][i].Value * WeightMeteoArrays[meteoParameterType][stationIndex]))
                    //{
                    //    // Handle NaN case if needed
                    //}

                    WeightedSumMeteoValuesArrays[meteoParameterType][i] += 
                        (MeteoValuesArrays[meteoParameterType][i] ?? 0.0) * WeightMeteoArrays[meteoParameterType][stationIndex];
                }

                var globalRad = MeteoValuesArrays[MeteoParameterType.GlobalRadiation][i] ?? 0.0;
                sumSupportGlobalRadiation[i] += globalRad;
                squaredSumSupportGlobalRadiation[i] += globalRad * globalRad;
            }

            return weatherParameters;
        }

        private List<MeteoParameters>
            GetBlendedMeteoParameters(
            int supportCount,
            List<DateTime> supportTimeStamps,
            double[] sumSupportGlobalRadiation,
            double[] squaredSumSupportGlobalRadiation)
        {
            var blendedWeatherData =
                    new List<MeteoParameters>();
            var timeInterval = (supportTimeStamps[1] - supportTimeStamps[0]).Minutes;

            for (var i = 0; i < supportCount; i++)
            {
                var directRadiationVariance = radiationVarianceMaxVariance;
                if (selectedStationsIdList.Count > 1)
                {
                    var E1i = sumSupportGlobalRadiation[i] / selectedStationsIdList.Count; ;
                    var E2i = squaredSumSupportGlobalRadiation[i] / selectedStationsIdList.Count;
                    directRadiationVariance = radiationBaselineVariance + (E2i - E1i * E1i) * selectedStationsIdList.Count / (selectedStationsIdList.Count - 1);
                }

                blendedWeatherData.Add(new MeteoParameters(
                    supportTimeStamps[i],
                    TimeSpan.FromMinutes(timeInterval),
                    MeteoParameterTypeList.Contains(MeteoParameterType.SunshineDuration) ? WeightedSumMeteoValuesArrays[MeteoParameterType.SunshineDuration][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.DirectRadiation) ? WeightedSumMeteoValuesArrays[MeteoParameterType.DirectRadiation][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.DirectNormalIrradiance) ? WeightedSumMeteoValuesArrays[MeteoParameterType.DirectNormalIrradiance][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.GlobalRadiation) ? WeightedSumMeteoValuesArrays[MeteoParameterType.GlobalRadiation][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.DiffuseRadiation) ? WeightedSumMeteoValuesArrays[MeteoParameterType.DiffuseRadiation][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.Temperature) ? WeightedSumMeteoValuesArrays[MeteoParameterType.Temperature][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.WindSpeed) ? WeightedSumMeteoValuesArrays[MeteoParameterType.WindSpeed][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.WindDirection) ? WeightedSumMeteoValuesArrays[MeteoParameterType.WindDirection][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.SnowDepth) ? WeightedSumMeteoValuesArrays[MeteoParameterType.SnowDepth][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.RelativeHumidity) ? WeightedSumMeteoValuesArrays[MeteoParameterType.RelativeHumidity][i] : null,
                    MeteoParameterTypeList.Contains(MeteoParameterType.DewPoint) ? WeightedSumMeteoValuesArrays[MeteoParameterType.DewPoint][i] : null,
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

            SetStationsWeightArrays(stationDictionary);

            foreach (var meteoParameterType in MeteoParameterTypeList)
            {
                WeightedSumMeteoValuesArrays[meteoParameterType] = new double[supportCount];
            }

            var perStationWeatherParameters = new List<StationMeteoData>();
            var sumSupportGlobalRadiation = new double[supportCount];
            var squaredSumSupportGlobalRadiation = new double[supportCount];
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
                    sumSupportGlobalRadiation,
                    squaredSumSupportGlobalRadiation);

                perStationWeatherParameters.Add(new StationMeteoData(stationId, weatherParameters));
            }

            var blendedWeatherData = GetBlendedMeteoParameters(
                supportCount,
                supportTimeStamps,
                sumSupportGlobalRadiation,
                squaredSumSupportGlobalRadiation);

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
            SetStationsWeightArrays(stationDictionary);

            foreach (var meteoParameterType in MeteoParameterTypeList)
            {
                WeightedSumMeteoValuesArrays[meteoParameterType] = new double[supportCount];
            }

            var sumSupportGlobalRadiation = new double[supportCount];
            var squaredSumSupportGlobalRadiation = new double[supportCount];

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
                    sumSupportGlobalRadiation,
                    squaredSumSupportGlobalRadiation);

                perStationWeatherParameters.Add(new StationMeteoData(stationId, weatherParameters));
            }

            var blendedWeatherData = GetBlendedMeteoParameters(
                supportCount,
                supportTimeStamps,
                sumSupportGlobalRadiation,
                squaredSumSupportGlobalRadiation);
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
                var validParameters = stationDataRecords[0].GetValidMeteoParameters();  // Use first record to check valid parameters

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
        private void SetStationsWeightArrays(Dictionary<string, WeightMeteoParameters> stationDictionary)
        {
            var stationsCount = stationDictionary.Count;

            foreach (var meteoParameterType in MeteoParameterTypeList)
            {
                WeightMeteoArrays[meteoParameterType] = new double[stationsCount];
            }

            var stationIndex = 0;
            foreach (var stationId in stationDictionary.Keys)
            {
                var weights = stationDictionary[stationId];
                foreach (var meteoParameterType in MeteoParameterTypeList)
                {
                    WeightMeteoArrays[meteoParameterType][stationIndex] = weights.WeightSunshineDuration;
                }

                stationIndex++;
            }

            // Compute normalized weights
            foreach (var meteoParameterType in MeteoParameterTypeList)
            {
                NormalizeMeteoWeightArray(meteoParameterType, stationsCount);
            }
        }
    }
}
