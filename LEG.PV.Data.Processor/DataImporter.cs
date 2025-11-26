using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;
using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.E3Dc.Client;
using LEG.HorizonProfiles.Client;
using LEG.MeteoSwiss.Abstractions;
using LEG.MeteoSwiss.Client.Forecast;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using System;
using System.Collections.Generic;
using System.Data;
using static LEG.MeteoSwiss.Client.Forecast.ForecastBlender;
using static LEG.PV.Data.Processor.DataRecords;

namespace LEG.PV.Data.Processor
{
    public class DataImporter
    {
        const double daysPerYear = 365.2422;
        const double hoursPerDay = 24.0;
        const double minutesPerHour = 60.0;
        const double minutesPerYear = minutesPerHour * hoursPerDay * daysPerYear;
        const double solarConstant = 1361.0;                                                       // [W/m²]
        const double maxGroundIrradiance = 1000.0;                                                 // [W/m²]
        const double irradianceNoise = maxGroundIrradiance / 100.0;                                // [W/m²]      Fluctuation of 1% of max irradiance
        const double irradianceBaselineVariance = irradianceNoise * irradianceNoise;               // [(W/m²)²]
        const double irradianceMaxVariance = maxGroundIrradiance * maxGroundIrradiance / 4;        // [(W/m²)²]   Bernoulli distribution with p=0.5

        // see also file: C:\code\LEG_analysis\Data\MeteoData\StationsData\klo_sma_hoe_ueb_recent_16.11.2025.xlsx
        const int meteoDataOffset = 60;           // Timestamps are UTC values
        int meteoDataLagHistory = 10;             // Values at given timestamp represent the aggregation over previous 10 minutes
        int meteoDataLagForecast = 0;            // Forecast data lag in minutes
        const double latSma = 47.378;
        const double lonSma =  8.566;

        // Selected stations, available parameters and blending weights
        List<string> selectedStationsIdList      = ["SMA", "KLO", "HOE", "UEB"];
        List<bool> hasGlobalIrradianceList       = [ true,  true,  true,  true];
        List<bool> hasSunshineDurationList       = [ true,  true,  true,  true];
        List<bool> hasDiffuseIrradianceList      = [false,  true, false,  true];
        List<bool> hasTemperatureList            = [ true,  true,  true, false];
        List<bool> hasWindSpeedList              = [ true,  true,  true, false];
        List<bool> hasSnowDepthList              = [ true,  true,  true, false];
        List<double> weightGlobalIrradianceList         = [  3.0,   1.0,   1.0,   1.0];
        List<double> weightSunshineDurationList         = [  3.0,   1.0,   1.0,   1.0];
        List<double> weightDirectNormalIrradianceList   = [  3.0,   1.0,   1.0,   1.0];         // Used in forecast only
        List<double> weightDiffuseIrradianceList        = [  0.0,   1.0,   0.0,   1.0];
        List<double> weightTemperatureList              = [  1.0,   1.0,   0.0,   0.0];
        List<double> weightWindSpeedList                = [  1.0,   1.0,   0.0,   0.0];
        List<double> weightSnowDepthList                = [  1.0,   1.0,   0.0,   0.0];

        public async Task UpdateWeatherData(DateTime downloadStartDate, List<string> stationsList)
        {
            var apiClient = new MeteoSwissClient();
            var meteoDataService = new MeteoDataService(apiClient);

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
            }
        }

        // Import meteo history and merge with actual and calculated pvProduction data
        public async Task<(
            List<(string stationID, List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth)> perStationWeatherData)>,
            List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth,
                double? directIrradianceVariance
                )> blendedWeatherData,
            string siteId, 
            List<PvRecord> dataRecords, 
            List<bool> validRecords,
            double installedPower, 
            int periodsPerHour)> 
            ImportE3DcAndMeteoHistory(int folder, bool meteoTillNow = false) 
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
                selectedStationsIdList,
                weightGlobalIrradianceList,
                weightSunshineDurationList,
                weightDirectNormalIrradianceList,
                weightDiffuseIrradianceList,
                weightTemperatureList,
                weightWindSpeedList,
                weightSnowDepthList,
                timeStamps, 
                shiftMeteoTimeStamps: meteoDataOffset + meteoDataLagHistory);

            // Merge data
            var countOfE3DcRecords = pvDataRecords.Count;
            var countOfMeteoRecords = blendedWeatherData.Count;
            var dataRecords = new List<PvRecord>();
            var validRecords = new List<bool>();
            var recordIndex = 0;
            for (var i=0; i < countOfMeteoRecords; i++)
            {
                var meteoParam = blendedWeatherData[i];
                var weight = 1.0 / (1E-6 + meteoParam.directIrradianceVariance ?? (double.MaxValue - 1E-6));
                //var pvDataRecord = pvDataRecords[i];
                double? solarProduction = i < countOfE3DcRecords ? pvDataRecords[i].SolarProduction : null;
                if (!solarProduction.HasValue)
                {
                    weight = 0.0;
                }
                var age = (timeStamps[i] - firstE3DcTimestamp).TotalMinutes / minutesPerYear;
                var pvRecord = new PvRecord (
                    timeStamps[i], 
                    recordIndex,                            // TODO: pvDataRecord.Index,
                    directGeometryFactors[i],
                    diffuseGeometryFactor,
                    sinSunElevations[i],
                    meteoParam.globalIrradiance ?? 0.0,
                    meteoParam.sunshineDuration ?? 0.0,
                    meteoParam.directNormalIrradiance ?? 0.0,
                    meteoParam.diffuseIrradiance ?? 0.0,
                    meteoParam.temperature ?? 0.0, 
                    meteoParam.windSpeed ?? 0.0, 
                    meteoParam.snowDepth ?? 0.0,
                    weight,
                    age, 
                    solarProduction
                    );
                dataRecords.Add(pvRecord);
                var validE3Dc = solarProduction.HasValue && solarProduction.Value > 0.0;
                validRecords.Add(pvRecord.DirectGeometryFactor > 0 || validE3Dc);
            }

            return (perStationWeatherData, blendedWeatherData, siteId, dataRecords, validRecords, installedPower, periodsPerHour);
        }

        // Import meteo forecast and merge with calculated pvProduction data
        public async Task<(
           List<(string stationID, List<(
               double? globalIrradiance,
               double? sunshineDuration,
               double? directNormalIrradiance,
               double? diffuseIrradiance,
               double? temperature,
               double? windSpeed,
               double? snowDepth)> perStationWeatherData)>,
           List<(
               double? globalIrradiance,
               double? sunshineDuration,
               double? directNormalIrradiance,
               double? diffuseIrradiance,
               double? temperature,
               double? windSpeed,
               double? snowDepth,
               double? directIrradianceVariance
               )> blendedWeatherData,
           string siteId,
           List<PvRecord> dataRecords,
           List<bool> validRecords,
           double installedPower,
           int periodsPerHour)>
            ImportMeteoForecastAndCalculatedProduction(int folder, DateTime firstE3DcTimestamp, DateTime lastHistoryTimestamp, int forecastDays = 16)
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
            //await UpdateWeatherData(firstDateTime, selectedStationsIdList);

            meteoDataLagForecast = 5 * (int)Math.Round((double)meteoDataLagForecast / 5);               // Lag to be applied to forecast data
            var (perStationWeatherData, blendedWeatherData) = await LoadBlendedWeatherForecast(
                selectedStationsIdList,
                weightGlobalIrradianceList,
                weightSunshineDurationList,
                weightDirectNormalIrradianceList,
                weightDiffuseIrradianceList,
                weightTemperatureList,
                weightWindSpeedList,
                weightSnowDepthList,
                timeStamps,
                shiftMeteoTimeStamps: meteoDataOffset + meteoDataLagForecast);

            // Merge data
            var countOfMeteoRecords = blendedWeatherData.Count;
            var dataRecords = new List<PvRecord>();
            var validRecords = new List<bool>();
            var recordIndex = 0;
            for (var i = 0; i < countOfMeteoRecords; i++)
            {
                var meteoParam = blendedWeatherData[i];
                var age = (timeStamps[i] - firstE3DcTimestamp).TotalMinutes / minutesPerYear;
                var pvRecord = new PvRecord(
                    timeStamps[i],
                    recordIndex,
                    directGeometryFactors[i],
                    diffuseGeometryFactor,
                    sinSunElevations[i],
                    meteoParam.globalIrradiance ?? 0.0,
                    meteoParam.sunshineDuration ?? 0.0,
                    meteoParam.directNormalIrradiance ?? 0.0,
                    meteoParam.diffuseIrradiance ?? 0.0,
                    meteoParam.temperature ?? 0.0,
                    meteoParam.windSpeed ?? 0.0,
                    meteoParam.snowDepth ?? 0.0,
                    0.0,
                    age,
                    null
                    );

                dataRecords.Add(pvRecord);
                validRecords.Add(false);
            }

            return (perStationWeatherData, blendedWeatherData, siteId, dataRecords, validRecords, installedPower, periodsPerHour);
        }

        // Import e3dc history and computed data only
        public async Task<(string siteId,
            List<PvRecord> dataRecords,
            List<bool> validRecords,
            double installedPower,
            int periodsPerHour)>
            ImportE3DcHistory(int folder, bool meteoTillNow=false)      // 0: downloaded E3Dc history, 1: meteo history till now, 2: including meteo forecast
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
            List<List<double?>> filteredIrradianceSeries,
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
                var lastInjectedTimestamp = listsDataRecords[^1].Timestamp;
                for (var i = 0; i < dataRecords.Count; i++)
                {
                    if (dataRecords[i].Timestamp > lastInjectedTimestamp)
                    {
                        indexFirstDataRecord = i;
                        break;
                    }
                }
            }

            for (var index = indexFirstDataRecord; index < dataRecords.Count; index++)
            {
                var record = dataRecords[index];
                var computedPower = record.ComputedPower(pvModelParams, installedPower, periodsPerHour: periodsPerHour);

                // Build lists for the current record, including the base series and the valid reference series
                List<double?> irradianceList = [];
                List<double?> temperatureList = [];
                List<double?> windSpeedList = [];
                irradianceList.AddRange(filteredIrradianceSeries.Select(series => series[index]));
                temperatureList.AddRange(filteredTemperatureSeries.Select(series => series[index]));
                windSpeedList.AddRange(filteredWindSpeedSeries.Select(series => series[index]));

                var listsDataRecord = new PvRecordLists(
                    record.Timestamp,
                    record.Index,
                    [record.MeasuredPower, computedPower],
                    irradianceList,
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
                =  [
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
            var (perStationWeatherHistory, blendedWeatherHistory, siteId, dataRecordsHistory, validRecordsHistory, installedPower, periodsPerHour) = await ImportE3DcAndMeteoHistory(folder, meteoTillNow: displayPeriod>0);

            // Filter: retain valid series and get the labels
            var filteredIrradianceLabels = new List<string>();
            var filteredTemperatureLabels = new List<string>();
            var filteredWindSpeedLabels = new List<string>();

            var filteredIrradianceHistorySeries = new List<List<double?>>();
            var filteredTemperatureHistorySeries = new List<List<double?>>();
            var filteredWindSpeedHistorySeries = new List<List<double?>>();
            for (int i = 0; i < selectedStationsIdList.Count; i++)
            {
                var (stationID, stationData) = perStationWeatherHistory[i];
                if (hasGlobalIrradianceList[i])
                {
                    filteredIrradianceLabels.Add($"Global_{stationID}");
                    filteredIrradianceHistorySeries.Add(stationData.Select(d => d.globalIrradiance).ToList());
                }
                if (hasDiffuseIrradianceList[i])
                {
                    filteredIrradianceLabels.Add($"Diffuse_{stationID}");
                    filteredIrradianceHistorySeries.Add(stationData.Select(d => d.diffuseIrradiance).ToList());
                }
                if (hasTemperatureList[i])
                {
                    filteredTemperatureLabels.Add($"Temperature_{stationID}");
                    filteredTemperatureHistorySeries.Add(stationData.Select(d => d.temperature).ToList());
                }
                if (hasWindSpeedList[i])
                {
                    filteredWindSpeedLabels.Add($"WindSpeed_{stationID}");
                    filteredWindSpeedHistorySeries.Add(stationData.Select(d => d.windSpeed).ToList());
                }
            }

            var listsDataRecords = new List<PvRecordLists>();
            var validListsDataRecords = new List<bool>();
            InjectDataRecords(
                pvModelParams,
                installedPower,
                periodsPerHour,
                dataRecordsHistory,
                validRecordsHistory,
                filteredIrradianceHistorySeries,
                filteredTemperatureHistorySeries,
                filteredWindSpeedHistorySeries,
                listsDataRecords,
                validListsDataRecords
                );

            // If forecast is requested, extend data with forecast values
            if (displayPeriod == 2)
            {
                var firstE3DcTimestamp = dataRecordsHistory[0].Timestamp;
                var lHistoryTimestamp = dataRecordsHistory[^1].Timestamp;
                var (perStationWeatherForecast, blendedWeatherForecasty, _, dataRecordsForecast, validRecordsForecast, _, _) = await ImportMeteoForecastAndCalculatedProduction(folder, firstE3DcTimestamp, lHistoryTimestamp);

                var filteredIrradianceForecastSeries = new List<List<double?>>();
                var filteredTemperatureForecastSeries = new List<List<double?>>();
                var filteredWindSpeedForecastSeries = new List<List<double?>>();
                for (int i = 0; i < selectedStationsIdList.Count; i++)
                {
                    var (stationID, stationData) = perStationWeatherForecast[i];
                    if (hasGlobalIrradianceList[i])
                    {
                        filteredIrradianceForecastSeries.Add(stationData.Select(d => d.globalIrradiance).ToList());
                    }
                    if (hasDiffuseIrradianceList[i])
                    {
                        filteredIrradianceForecastSeries.Add(stationData.Select(d => d.diffuseIrradiance).ToList());
                    }
                    if (hasTemperatureList[i])
                    {
                        filteredTemperatureForecastSeries.Add(stationData.Select(d => d.temperature).ToList());
                    }
                    if (hasWindSpeedList[i])
                    {
                        filteredWindSpeedForecastSeries.Add(stationData.Select(d => d.windSpeed).ToList());
                    }
                }
                InjectDataRecords(
                    pvModelParams,
                    installedPower,
                    periodsPerHour,
                    dataRecordsForecast,
                    validRecordsForecast,
                    filteredIrradianceForecastSeries,
                    filteredTemperatureForecastSeries,
                    filteredWindSpeedForecastSeries,
                    listsDataRecords,
                    validListsDataRecords
                    );
            }

            var dataRecordLabels = new PvRecordLabels(
                ["MeasuredPower", "ComputedPower"],
                filteredIrradianceLabels,
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
            DateTime supportTimeStamp, DateTime meteoTimeStamp, WeatherCsvRecord leftRecord,
            double?[] supportGlobalIrradiance, 
            double?[] supportSunshineDuration,
            double?[] supportDirectNormalIrradiance,
            double?[] supportDiffuseIrradiance, 
            double?[] supportTemperature, 
            double?[] supportWindSpeed, 
            double?[] supportSnowDepth)
        {
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

            var priorGlobalIrradiance = leftRecord.GlobalIrradiance;
            var priorSunshineDuration = leftRecord.SunshineDuration;        // TODO: currently not used
            var priorDirectNormalIrradiance = leftRecord.DirectNormalIrradiance;
            var priorDiffuseIrradiance = leftRecord.DiffuseIrradiance;
            var priorTemperature = leftRecord.Temperature2m;
            var priorWindSpeed = leftRecord.WindSpeed10min_kmh;
            var priorSnowDepth = leftRecord.SnowDepth;                      // TODO: currently not used

            if (iLeft >= 0 && iLeft < supportCount && leftOverlapRatio > 0)
            {
                if (priorGlobalIrradiance.HasValue) supportGlobalIrradiance[iLeft] = (supportGlobalIrradiance[iLeft] ?? 0) + priorGlobalIrradiance.Value * leftOverlapRatio;
                if (priorSunshineDuration.HasValue) supportSunshineDuration[iLeft] = (supportSunshineDuration[iLeft] ?? 0) + priorSunshineDuration.Value * leftOverlapRatio;
                if (priorDirectNormalIrradiance.HasValue) supportDirectNormalIrradiance[iLeft] = (supportDirectNormalIrradiance[iLeft] ?? 0) + priorDirectNormalIrradiance.Value * leftOverlapRatio;
                if (priorDiffuseIrradiance.HasValue) supportDiffuseIrradiance[iLeft] = (supportDiffuseIrradiance[iLeft] ?? 0) + priorDiffuseIrradiance.Value * leftOverlapRatio;
                if (priorTemperature.HasValue) supportTemperature[iLeft] = (supportTemperature[iLeft] ?? 0) + priorTemperature.Value * leftOverlapRatio;
                if (priorWindSpeed.HasValue) supportWindSpeed[iLeft] = (supportWindSpeed[iLeft] ?? 0) + priorWindSpeed.Value * leftOverlapRatio;
                if (priorSnowDepth.HasValue) supportSnowDepth[iLeft] = (supportSnowDepth[iLeft] ?? 0) + priorSnowDepth.Value * leftOverlapRatio;
            }
            if (iRight >= 0 && iRight < supportCount && rightOverlapRatio > 0)
            {
                if (priorGlobalIrradiance.HasValue) supportGlobalIrradiance[iRight] = (supportGlobalIrradiance[iRight] ?? 0) + priorGlobalIrradiance.Value * rightOverlapRatio;
                if (priorSunshineDuration.HasValue) supportSunshineDuration[iRight] = (supportSunshineDuration[iRight] ?? 0) + priorSunshineDuration.Value * rightOverlapRatio;
                if (priorDirectNormalIrradiance.HasValue) supportDirectNormalIrradiance[iRight] = (supportDirectNormalIrradiance[iRight] ?? 0) + priorDirectNormalIrradiance.Value * rightOverlapRatio;
                if (priorDiffuseIrradiance.HasValue) supportDiffuseIrradiance[iRight] = (supportDiffuseIrradiance[iRight] ?? 0) + priorDiffuseIrradiance.Value * rightOverlapRatio;
                if (priorTemperature.HasValue) supportTemperature[iRight] = (supportTemperature[iRight] ?? 0) + priorTemperature.Value * rightOverlapRatio;
                if (priorWindSpeed.HasValue) supportWindSpeed[iRight] = (supportWindSpeed[iRight] ?? 0) + priorWindSpeed.Value * rightOverlapRatio;
                if (priorSnowDepth.HasValue) supportSnowDepth[iRight] = (supportSnowDepth[iRight] ?? 0) + priorSnowDepth.Value * rightOverlapRatio;
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
        private new List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth)>
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
            List<WeatherCsvRecord> weatherRecords,
            double[] weightGlobalIrradiance,
            double[] weightSunshineDuration,
            double[] weightDirectNormalIrradiance,
            double[] weightDiffuseIrradiance,
            double[] weightTemperature,
            double[] weightWindSpeed,
            double[] weightSnowDepth,
            double[] weightedSumSupportGlobalIrradiance,
            double[] weightedSumSupportSunshineDuration,
            double[] weightedSumSupportDirectNormalIrradiance,
            double[] weightedSumSupportDiffuseIrradiance,
            double[] weightedSumSupportTemperature,
            double[] weightedSumSupportWindSpeed,
            double[] weightedSumSupportSnowDepth,
            double[] sumSupportGlobalIrradiance,
            double[] squaredSumSupportGlobalIrradiance
        )
        {
            var iSupport = 0;
            var iMeteo = 0;
            var leftRecord = weatherRecords[0];
            var supportGlobalIrradiance = new double?[supportCount];
            var supportSunshineDuration = new double?[supportCount];
            var supportDirectNormalIrradiance = new double?[supportCount];
            var supportDiffuseIrradiance = new double?[supportCount];
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
                    supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], weatherRecords[iMeteo],
                    supportGlobalIrradiance, supportSunshineDuration, supportDirectNormalIrradiance, supportDiffuseIrradiance, supportTemperature, supportWindSpeed, supportSnowDepth);
                iSupport++;
                while (iSupport < supportCount && iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= supportTimeStamps[iSupport])
                {
                    iMeteo++;
                    AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
                        supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], weatherRecords[iMeteo],
                        supportGlobalIrradiance, supportSunshineDuration, supportDirectNormalIrradiance, supportDiffuseIrradiance, supportTemperature, supportWindSpeed, supportSnowDepth);
                }
                iMeteo++;
            }

            var weatherParameters =
                new List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth)>();
            for (var i = 0; i < supportCount; i++)
            {
                weatherParameters.Add((
                    supportGlobalIrradiance[i],
                    supportSunshineDuration[i],
                    supportDirectNormalIrradiance[i],
                    supportDiffuseIrradiance[i],
                    supportTemperature[i],
                    supportWindSpeed[i],
                    supportSnowDepth[i]));

                var gG = supportGlobalIrradiance[i] ?? 0.0;
                var sD = supportSunshineDuration[i] ?? 0.0;
                var sI = supportDirectNormalIrradiance[i] ?? 0.0;
                var gD = supportDiffuseIrradiance[i] ?? 0.0;
                var aT = supportTemperature[i] ?? 0.0;
                var vW = supportWindSpeed[i] ?? 0.0;
                var sN = supportSnowDepth[i] ?? 0.0;
                weightedSumSupportGlobalIrradiance[i] += gG * weightGlobalIrradiance[stationIndex];                     // Historical records
                weightedSumSupportSunshineDuration[i] += sD * weightSunshineDuration[stationIndex];
                weightedSumSupportDirectNormalIrradiance[i] += sI * weightDirectNormalIrradiance[stationIndex];         // Forecast records
                weightedSumSupportDiffuseIrradiance[i] += gD * weightDiffuseIrradiance[stationIndex];
                weightedSumSupportTemperature[i] += aT * weightTemperature[stationIndex];
                weightedSumSupportWindSpeed[i] += vW * weightWindSpeed[stationIndex];
                weightedSumSupportSnowDepth[i] += sN * weightSnowDepth[stationIndex];

                sumSupportGlobalIrradiance[i] += gG;
                squaredSumSupportGlobalIrradiance[i] += gG * gG;
            }

            return weatherParameters;
        }

        // 

        private List<(
            double? globalIrradiance,
            double? sunshineDuration,
            double? directNormalIrradiance,
            double? diffuseIrradiance,
            double? temperature,
            double? windSpeed,
            double? snowDepth,
            double? globalIrradianceVariance)>
            GetBlendedMeteoParameters(
            int supportCount,
            double[] weightedSumSupportGlobalIrradiance,
            double[] weightedSumSupportSunshineDuration,
            double[] weightedSumSupportDirectNormalIrradiance,
            double[] weightedSumSupportDiffuseIrradiance,
            double[] weightedSumSupportTemperature,
            double[] weightedSumSupportWindSpeed,
            double[] weightedSumSupportSnowDepth,
            double[] sumSupportGlobalIrradiance,
            double[] squaredSumSupportGlobalIrradiance)
        {
            var blendedWeatherData =
                    new List<(
                    double? globalIrradiance,
                    double? sunshineDuration,
                    double? directNormalIrradiance,
                    double? diffuseIrradiance,
                    double? temperature,
                    double? windSpeed,
                    double? snowDepth,
                    double? globalIrradianceVariance)>();

            for (var i = 0; i < supportCount; i++)
            {
                var globalIrradianceVariance = irradianceMaxVariance;
                if (selectedStationsIdList.Count > 1)
                {
                    var E1i = sumSupportGlobalIrradiance[i] / selectedStationsIdList.Count; ;
                    var E2i = squaredSumSupportGlobalIrradiance[i] / selectedStationsIdList.Count;
                    globalIrradianceVariance = irradianceBaselineVariance + (E2i - E1i * E1i) * selectedStationsIdList.Count / (selectedStationsIdList.Count - 1);
                }

                blendedWeatherData.Add((
                    weightedSumSupportGlobalIrradiance[i],
                    weightedSumSupportSunshineDuration[i],
                    weightedSumSupportDirectNormalIrradiance[i],
                    weightedSumSupportDiffuseIrradiance[i],
                    weightedSumSupportTemperature[i],
                    weightedSumSupportWindSpeed[i],
                    weightedSumSupportSnowDepth[i],
                    globalIrradianceVariance));
            }

            return blendedWeatherData;
        }

        // Load meteo history and blend data from selected stations
        private (
            List<(string stationID, List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth)> perStationWeatherData)>,
            List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth,
                double? directIrradianceVariance
                )> blendedWeatherData)
            LoadBlendedWeatherHistory(
            List<string> selectedStationsIdList,
            List<double> weightGlobalIrradianceList,
            List<double> weightSunshineDurationList,
            List<double> weightDirectNormalIrradianceList,
            List<double> weightDiffuseIrradianceList,
            List<double> weightTemperatureList,
            List<double> weightWindSpeedList,
            List<double> weightSnowDepthList,
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

            var perStationWeatherParameters = new List<(string stationID, List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth)> perStationWeatherData)>();

            // Compute normalized weights
            var countStations = selectedStationsIdList.Count;
            var weightGlobalIrradiance = GetWeights(weightGlobalIrradianceList, countStations);
            var weightSunshineDuration = GetWeights(weightSunshineDurationList, countStations);
            var weightDirectNormalIrradiance = GetWeights(weightDirectNormalIrradianceList, countStations);
            var weightDiffuseIrradiance = GetWeights(weightDiffuseIrradianceList, countStations);
            var weightTemperature = GetWeights(weightTemperatureList, countStations);
            var weightWindSpeed = GetWeights(weightWindSpeedList, countStations);
            var weightSnowDepth = GetWeights(weightSnowDepthList, countStations);

            var weightedSumSupportGlobalIrradiance = new double[supportCount];
            var weightedSumSupportSunshineDuration = new double[supportCount];
            var weightedSumSupportDirectNormalIrradiance = new double[supportCount];
            var weightedSumSupportDiffuseIrradiance = new double[supportCount];
            var weightedSumSupportTemperature = new double[supportCount];
            var weightedSumSupportWindSpeed = new double[supportCount];
            var weightedSumSupportSnowDepth = new double[supportCount];

            var sumSupportGlobalIrradiance = new double[supportCount];
            var squaredSumSupportGlobalIrradiance = new double[supportCount];
            var stationIndex = -1;
            foreach (var stationId in selectedStationsIdList)
            {
                stationIndex++;

                var weatherRecords = MeteoAggregator.GetFilteredRecords(
                    stationId,
                    groundStationsMetaDict[stationId],
                    firstYear,
                    lastYear,
                    "t",
                    includeRecent: true,
                    includeNow: true
                    );

                var alignedMeteoTimeStamps = weatherRecords.Select(r => r.ReferenceTimestamp.AddMinutes(shiftMeteoTimeStamps)).ToList();

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
                    countStations,
                    supportCount,
                    supportInterval,
                    meteoCount,
                    meteoInterval,
                    firstSupportTimestamp,
                    upperBound,
                    supportTimeStamps,
                    alignedMeteoTimeStamps,
                    weatherRecords,
                    weightGlobalIrradiance,
                    weightSunshineDuration,
                    weightDirectNormalIrradiance,
                    weightDiffuseIrradiance,
                    weightTemperature,
                    weightWindSpeed,
                    weightSnowDepth,
                    weightedSumSupportGlobalIrradiance,
                    weightedSumSupportSunshineDuration,
                    weightedSumSupportDirectNormalIrradiance,
                    weightedSumSupportDiffuseIrradiance,
                    weightedSumSupportTemperature,
                    weightedSumSupportWindSpeed,
                    weightedSumSupportSnowDepth,
                    sumSupportGlobalIrradiance,
                    squaredSumSupportGlobalIrradiance);

                perStationWeatherParameters.Add((stationId, weatherParameters));
            }

            var blendedWeatherData = GetBlendedMeteoParameters(
                supportCount,
                weightedSumSupportGlobalIrradiance,
                weightedSumSupportSunshineDuration,
                weightedSumSupportDirectNormalIrradiance,
                weightedSumSupportDiffuseIrradiance,
                weightedSumSupportTemperature,
                weightedSumSupportWindSpeed,
                weightedSumSupportSnowDepth,
                sumSupportGlobalIrradiance,
                squaredSumSupportGlobalIrradiance);

            return (perStationWeatherParameters, blendedWeatherData);
        }


        // Load meteo forecast and blend data from selected stations
        private async Task<(
            List<(string stationID, List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth)> perStationWeatherData)>,
            List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth,
                double? directIrradianceVariance
                )> blendedWeatherData)>
            LoadBlendedWeatherForecast(
            List<string> selectedStationsIdList,
            List<double> weightGlobalIrradianceList,
            List<double> weightSunshineDurationList,
            List<double> weightDirectNormalIrradianceList,
            List<double> weightDiffuseIrradianceList,
            List<double> weightTemperatureList,
            List<double> weightWindSpeedList,
            List<double> weightSnowDepthList,
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

            var perStationWeatherParameters = new List<(string stationID, List<(
                double? globalIrradiance,
                double? sunshineDuration,
                double? directNormalIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? snowDepth)> perStationWeatherData)>();

            // Compute normalized weights
            var countStations = selectedStationsIdList.Count;
            var weightGlobalIrradiance = GetWeights(weightGlobalIrradianceList, countStations);
            var weightSunshineDuration = GetWeights(weightSunshineDurationList, countStations);
            var weightDirectNormalIrradiance = GetWeights(weightDirectNormalIrradianceList, countStations);
            var weightDiffuseIrradiance = GetWeights(weightDiffuseIrradianceList, countStations);
            var weightTemperature = GetWeights(weightTemperatureList, countStations);
            var weightWindSpeed = GetWeights(weightWindSpeedList, countStations);
            var weightSnowDepth = GetWeights(weightSnowDepthList, countStations);

            var weightedSumSupportGlobalIrradiance = new double[supportCount];
            var weightedSumSupportSunshineDuration = new double[supportCount];
            var weightedSumSupportDirectNormalIrradiance = new double[supportCount];
            var weightedSumSupportDiffuseIrradiance = new double[supportCount];
            var weightedSumSupportTemperature = new double[supportCount];
            var weightedSumSupportWindSpeed = new double[supportCount];
            var weightedSumSupportSnowDepth = new double[supportCount];

            var sumSupportGlobalIrradiance = new double[supportCount];
            var squaredSumSupportGlobalIrradiance = new double[supportCount];

            // Fetch forecasts for all stations
            var forecastClient = new WeatherForecastClient();
            var blendedForecastPerStation = new List<List<BlendedPeriod>>();
            foreach (var stationId in selectedStationsIdList)
            {
                var longCast = await forecastClient.Get16DayPeriodsByStationIdAsync(stationId);
                var midCast = await forecastClient.Get7DayPeriodsByStationIdAsync(stationId);
                var nowCast = await forecastClient.GetNowcast15MinuteByStationIdAsync(stationId);
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

            // Map to WeatherCsvRecord and blend data from all stations using the algorithm for historical data
            var stationIndex = -1;
            foreach (var stationId in selectedStationsIdList)
            {
                stationIndex++;
                var blendedForecast = blendedForecastPerStation[stationIndex];

                var weatherRecords = new List<WeatherCsvRecord>();
                foreach (var record in blendedForecast)
                {
                    if (record.Time < startTimestamp || record.Time > endTimestamp)
                    {
                        continue;
                    }
                    weatherRecords.Add(new WeatherCsvRecord
                    {
                        ReferenceTimestamp = record.Time,
                        GlobalIrradiance = null,
                        SunshineDuration = null,
                        DirectNormalIrradiance = record.DniWm2,             // Specific for forecast data
                        DiffuseIrradiance = record.DiffuseWm2,
                        Temperature2m = record.TempC,
                        WindSpeed10min_kmh = record.WindKmh,
                        SnowDepth = record.SnowDepthCm
                    });
                }
                var alignedMeteoTimeStamps = weatherRecords.Select(r => r.ReferenceTimestamp.AddMinutes(shiftMeteoTimeStamps)).ToList();

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
                    countStations,
                    supportCount,
                    supportInterval,
                    meteoCount,
                    meteoInterval,
                    firstSupportTimestamp,
                    upperBound,
                    supportTimeStamps,
                    alignedMeteoTimeStamps,
                    weatherRecords,
                    weightGlobalIrradiance,
                    weightSunshineDuration,
                    weightDirectNormalIrradiance,
                    weightDiffuseIrradiance,
                    weightTemperature,
                    weightWindSpeed,
                    weightSnowDepth,
                    weightedSumSupportGlobalIrradiance,
                    weightedSumSupportSunshineDuration,
                    weightedSumSupportDirectNormalIrradiance,
                    weightedSumSupportDiffuseIrradiance,
                    weightedSumSupportTemperature,
                    weightedSumSupportWindSpeed,
                    weightedSumSupportSnowDepth,
                    sumSupportGlobalIrradiance,
                    squaredSumSupportGlobalIrradiance);

            perStationWeatherParameters.Add((stationId, weatherParameters));
            }

            var blendedWeatherData = GetBlendedMeteoParameters(
                supportCount,
                weightedSumSupportGlobalIrradiance,
                weightedSumSupportSunshineDuration,
                weightedSumSupportDirectNormalIrradiance,
                weightedSumSupportDiffuseIrradiance,
                weightedSumSupportTemperature,
                weightedSumSupportWindSpeed,
                weightedSumSupportSnowDepth,
                sumSupportGlobalIrradiance,
                squaredSumSupportGlobalIrradiance);
            return (perStationWeatherParameters, blendedWeatherData);
        }
    }
}