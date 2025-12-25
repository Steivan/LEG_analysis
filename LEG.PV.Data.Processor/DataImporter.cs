using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;
using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.HorizonProfiles.Client;
using LEG.MeteoSwiss.Abstractions.Models;
using LEG.MeteoSwiss.Client.Forecast;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using LEG.PV.Core.Models;
using LEG.PV.Data.Processor.Helpers;
using LEG.PV.Data.Processor.Interfaces;
using LEG.PvImport.Abstractions;
using LEG.PvImport.Clients.E3Dc.Client;
using LEG.PvImport.Clients.Fronius.Client;
using System.Data;
using static LEG.CoreLib.SampleData.ReferenceData.MeteoStationProfile;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;
using static LEG.PV.Core.Models.PvDataClass;
using static LEG.PV.Data.Processor.Simulator.SimulatorParameters;
using static LEG.PV.Core.Models.MeteoCalibrationParameters.MeteoCalibrationParameters;

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

        public Dictionary<MeteoParameterType, double?[]> MeteoValuesArrays { get; set; } = new();
        public Dictionary<MeteoParameterType, double[]> WeightMeteoArrays { get; set; } = new();
        public Dictionary<MeteoParameterType, double[]> WeightedSumMeteoValuesArrays { get; set; } = new();

        public static List<string> AvailableSitesIdList = PvModelParamsDictionary.Keys.ToList();

        // Import synthetic meteo and PV production data
        public static MeteoImportResult GenerateSyntheticData(
            PvModelParams modelParameters,
            double installedPower = 10000.0,    // [W]
            double simulationsPeriod = 5.0)
        {
            bool applyRandomNoise = true;
            bool applySnowDays = true;
            bool applyFoggyDays = true;
            bool applyOutliers = true;
   
            var siteId = "SyntheticSite";
            var minutesPerPeriod = 15;
            var periodsPerHour = 60 / minutesPerPeriod;
            var now = DateTime.UtcNow;
            var (pvRecords, modelValidRecords) = PvRandomRecordGenerator.GetPvSimulatedRecordsList(
                now.AddYears(-(int)simulationsPeriod),
                now,
                minutesPerPeriod: minutesPerPeriod,
                pvParams: modelParameters,
                siteLatitude: 46,
                siteLongitude: 10,
                installedPower: installedPower,
                roofAzimuth: -30,
                roofElevation: 20,
                applyRandomNoise: applyRandomNoise,
                applySnowDays: applySnowDays,
                applyFoggyDays: applyFoggyDays,
                applyOutliers: applyOutliers
                );

            var blendedWeatherData = pvRecords.Select(record => record.MeteoDataRecord).ToList();
            var perStationWeatherData = new List<StationMeteoData>() { new StationMeteoData("MC", blendedWeatherData) };

            return new MeteoImportResult(perStationWeatherData, blendedWeatherData, siteId, pvRecords, modelValidRecords, installedPower, periodsPerHour);
        }

        // Import meteo history and merge with actual and calculated pvProduction data
        public async Task<MeteoImportResult> ImportProductionAndMeteoHistory(int folder, int displayPeriod = 0)
        {
            // Compute normalized weights
            SetSelectedStationsWeightArrays(folder); // StationDictionary);

            // Fetch pvProduction records
            folder = 1 + (folder - 1) % 3;
            var siteId = folder == 1 ? ListSites.Senn : ListSites.SennV;
            var pvDataRecords = new List<IPowerRecord>();
            switch (folder)
            {
                case 1:
                case 2:
                    siteId = folder == 1 ? ListSites.Senn : ListSites.SennV;
                    pvDataRecords = E3DcLoadPeriodRecords.LoadPowerRecords(folder);
                    break;
                case 3:
                    siteId = ListSites.Studenrain;
                    pvDataRecords = FroniusLoadPeriodRecords.LoadPowerRecords();
                    break;
                default:
                    break;
            }

            var firstImportTimestamp = pvDataRecords[0].Timestamp;
            var secondImportTimestamp = pvDataRecords[1].Timestamp;
            var lastImportTimestamp = pvDataRecords[^1].Timestamp;

            var minutesPerPeriod = (secondImportTimestamp - firstImportTimestamp).Minutes;
            var periodsPerHour = 60 / minutesPerPeriod;

            var firstTimestamp = firstImportTimestamp;
            var lastTimestamp = displayPeriod==1 ? DateTime.Now : displayPeriod == 2 ? DateTime.Now.AddDays(10) : lastImportTimestamp;

            // Fetch geometry factors
            var (timeStamps, geometryFactors, installedPower) = await PvProduction(siteId, firstTimestamp, lastTimestamp, minutesPerPeriod, shiftSupportTimeStamps: 0);
            firstTimestamp = timeStamps[0];
            lastTimestamp = timeStamps[^1];

            // Fetch meteo data
            // Update historic weather data for selected stations
            MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetAllGroundStations();
            var updateClient = new MeteoSwissUpdater();
            await updateClient.UpdateDataForGroundStations(SelectedStationsIdList, granularity: "t");

            meteoDataLagHistory = 5 * (int)Math.Round((double)meteoDataLagHistory / 5);             // Lag to be applied to historical data
            var (perStationWeatherData, blendedWeatherData) = LoadBlendedWeatherHistory(
                timeStamps,
                shiftMeteoTimeStamps: meteoDataOffset + meteoDataLagHistory);

            // Merge data
            var countOfImportRecords = pvDataRecords.Count;
            var countOfMeteoRecords = blendedWeatherData.Count;
            var dataRecords = new List<PvRecord>();
            var validRecords = new List<bool>();
            for (var i = 0; i < countOfMeteoRecords; i++)
            {
                var recordIndex = i;
                var meteoParam = blendedWeatherData[i];
                var weight = 1.0 / (1E-6 + meteoParam.RadiationVariance ?? (double.MaxValue - 1E-6));
                double? solarProduction = i < countOfImportRecords ? pvDataRecords[i].SolarProduction : null;
                if (!solarProduction.HasValue)
                {
                    weight = 0.0;
                }
                var age = (timeStamps[i] - firstImportTimestamp).TotalMinutes / minutesPerYear;
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
                var validImport = solarProduction.HasValue && solarProduction.Value > 0.0;
                validRecords.Add(pvRecord.SolarGeometry.HasIrradiance || validImport);
            }

            perStationWeatherData.Add(new StationMeteoData("Blended", blendedWeatherData));

            return new MeteoImportResult(perStationWeatherData, blendedWeatherData, siteId, dataRecords, validRecords, installedPower, periodsPerHour);
        }

        // Import meteo forecast and merge with calculated pvProduction data
        public async Task<MeteoImportResult> ImportMeteoForecastAndCalculatedProduction(
            int folder,
            DateTime firstImportTimestamp,
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
                timeStamps,
                shiftMeteoTimeStamps: meteoDataOffset + meteoDataLagForecast);

            // Merge data
            var countOfMeteoRecords = blendedWeatherData.Count;
            var dataRecords = new List<PvRecord>();
            var validRecords = new List<bool>();
            for (var i = 0; i < countOfMeteoRecords; i++)
            {
                var recordIndex = i;
                var age = (timeStamps[i] - firstImportTimestamp).TotalMinutes / minutesPerYear;
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

            perStationWeatherData.Add(new StationMeteoData("Blended", blendedWeatherData));

            return new MeteoImportResult(perStationWeatherData, blendedWeatherData, siteId, dataRecords, validRecords, installedPower, periodsPerHour);
        }

        // Import history and computed data only
        public async Task<(string siteId,
            List<PvRecord> dataRecords,
            List<bool> validRecords,
            double installedPower,
            int periodsPerHour)>
            ImportProductionHistory(int folder, int displayPeriod = 0)      // 0: downloaded history, 1: meteo history till now, 2: including meteo forecast
        {
            var (_, _, siteId, dataRecords, validRecords, installedPower, periodsPerHour) = await ImportProductionAndMeteoHistory(folder, displayPeriod: displayPeriod);

            return (siteId, dataRecords, validRecords, installedPower, periodsPerHour);
        }

        private void InjectDataRecords(
            PvModelParams pvModelParams,
            double installedPower,
            int periodsPerHour,
            List<PvRecord> dataRecords,
            List<bool> validDataRecord,
            Dictionary<string, List<double?>> filteredRadiationSeries,
            Dictionary<string, List<double?>> filteredTemperatureSeries,
            Dictionary<string, List<double?>> filteredWindSpeedSeries,
            Dictionary<string, List<double?>> filteredSnowDepthSeries,
            Dictionary<string, List<double?>> filteredRelativeHumiditySeries,
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
                var record = dataRecords[index];
                var pvDataRecord = record.GetPvResidualsRecord(pvModelParams, installedPower, periodsPerHour: periodsPerHour);
                var computedPower = pvDataRecord.ComputedPower;
                var residualsRecord = pvDataRecord.UnexplainedFractionLossRecord;
                var referenceResidual = computedPower.PowerG / (installedPower / periodsPerHour);
                var hasCalculated = pvDataRecord.HasCalculated;

                // Build dictionaries for the current record, including the base series and the valid reference series
                var powerDict = new Dictionary<string, double?>
                {
                    { PvConstants.MeasuredPower, record.MeasuredPower },
                    { PvConstants.PowerGR, computedPower.PowerGR },
                    { PvConstants.PowerGRTW, computedPower.PowerGRTW },
                    { PvConstants.PowerGRTWSF, computedPower.PowerGRTWSF }
                };

                var residualsDict = new Dictionary<string, double?>
                {
                    { PvConstants.Reference, referenceResidual },
                    { PvConstants.UflGR, residualsRecord.PowerGR },
                    { PvConstants.UflGRTW, residualsRecord.PowerGRTW },
                    { PvConstants.UflGRTWSF, residualsRecord.PowerGRTWSF }
                };

                var radiationDict = new Dictionary<string, double?>();
                foreach (var label in filteredRadiationSeries.Keys)
                {
                    radiationDict[label] = filteredRadiationSeries[label][index];
                }
                var temperatureDict = new Dictionary<string, double?>();
                foreach (var label in filteredTemperatureSeries.Keys)
                {
                    temperatureDict[label] = filteredTemperatureSeries[label][index];
                }
                var windSpeedDict = new Dictionary<string, double?>();
                foreach (var label in filteredWindSpeedSeries.Keys)
                {
                    windSpeedDict[label] = filteredWindSpeedSeries[label][index];
                }
                var snowDepthDict = new Dictionary<string, double?>();
                foreach (var label in filteredSnowDepthSeries.Keys)
                {
                    snowDepthDict[label] = filteredSnowDepthSeries[label][index];
                }
                var relativeHumidityDict = new Dictionary<string, double?>();
                foreach (var label in filteredRelativeHumiditySeries.Keys)
                {
                    relativeHumidityDict[label] = filteredRelativeHumiditySeries[label][index];
                }

                var listsDataRecord = new PvRecordLists(
                    record.Timestamp,
                    record.Index,
                    powerDict,
                    residualsDict,
                    radiationDict,
                    temperatureDict,
                    windSpeedDict,
                    snowDepthDict,
                    relativeHumidityDict
                );

                listsDataRecords.Add(listsDataRecord);
                validListsDataRecords.Add(validDataRecord[index]);
            }

        }

        // Import history and computed data with selected meteo parameters
        public async Task<(
            string siteId,
            List<PvRecordLists> dataRecords,
            PvRecordLabels dataRecordLabels,
            List<bool> validRecords,
            double installedPower,
            int periodsPerHour)>
            ImportHistoryAndCalculated(int folder, int displayPeriod = 2)      // 0: downloaded meteo for PV history, 1: meteo PV history till now, 2: including meteo forecast
        {
            var siteModelId = AvailableSitesIdList[folder];
            var pvModelParams = PvModelParamsDictionary[siteModelId];

            MeteoImportResult meteoImportResult = null;
            switch (folder)
            {
                case 0:
                    displayPeriod = 0;   // synthetic meteo data is only available for the simulated period
                    meteoImportResult = GenerateSyntheticData(pvModelParams, simulationsPeriod: 5);
                    break;
                case 1:
                case 2:
                    // 1: Senn and 2: SennV site with E3Dc data
                    meteoImportResult = await ImportProductionAndMeteoHistory(folder, displayPeriod: displayPeriod);
                    break;
                case 3:
                    // Studenrain site with Fronius data
                    displayPeriod = 0; // data from 2011 till 2015 only
                    meteoImportResult = await ImportProductionAndMeteoHistory(3, displayPeriod);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(folder), "Folder index out of range");
            }

            // Extract pvProduction and meteo data
            var (perStationWeatherHistory, 
                blendedWeatherHistory, 
                siteId, 
                dataRecordsHistory, 
                validRecordsHistory, 
                installedPower, 
                periodsPerHour) = meteoImportResult;

            var (filteredRadiationHistorySeries, filteredRadiationLabels,
                filteredTemperatureHistorySeries, filteredTemperatureLabels,
                filteredWindSpeedHistorySeries, filteredWindSpeedLabels,
                filteredSnowDepthSeries, filteredSnowDepthLabels,
                filteredRelativeHumiditySeries, filteredRelativeHumidityLabels) = FilterAndLabelSeries(perStationWeatherHistory);

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
                filteredSnowDepthSeries,
                filteredRelativeHumiditySeries,
                listsDataRecords,
                validListsDataRecords
                );

            // If forecast is requested, extend data with forecast values
            if (displayPeriod == 2)
            {
                var firstImportTimestamp = dataRecordsHistory[0].Timestamp;
                var lHistoryTimestamp = dataRecordsHistory[^3].Timestamp;
                var (perStationWeatherForecast, 
                    blendedWeatherForecasty, 
                    _, 
                    dataRecordsForecast, 
                    validRecordsForecast, 
                    _, 
                    _) = await ImportMeteoForecastAndCalculatedProduction(folder, firstImportTimestamp, lHistoryTimestamp);

                var (filteredRadiationForecastSeries, _,
                    filteredTemperatureForecastSeries, _,
                    filteredWindSpeedForecastSeries, _,
                    filteredSnowDepthForecastSeries, _,
                    filteredRelativeHumidityForecastSeries, _) = FilterAndLabelSeries(perStationWeatherForecast);

                InjectDataRecords(
                    pvModelParams,
                    installedPower,
                    periodsPerHour,
                    dataRecordsForecast,
                    validRecordsForecast,
                    filteredRadiationForecastSeries,
                    filteredTemperatureForecastSeries,
                    filteredWindSpeedForecastSeries,
                    filteredSnowDepthForecastSeries,
                    filteredRelativeHumidityForecastSeries,
                    listsDataRecords,
                    validListsDataRecords
                    );
            }

            var dataRecordLabels = new PvRecordLabels(
                [PvConstants.MeasuredPower, PvConstants.PowerGR, PvConstants.PowerGRTW, PvConstants.PowerGRTWSF],
                [PvConstants.Reference, PvConstants.UflGR, PvConstants.UflGRTW, PvConstants.UflGRTWSF],
                filteredRadiationLabels.Select(kv => kv.Key).ToList(),
                filteredTemperatureLabels.Select(kv => kv.Key).ToList(),
                filteredWindSpeedLabels.Select(kv => kv.Key).ToList(),
                filteredSnowDepthLabels.Select(kv => kv.Key).ToList(),
                filteredRelativeHumidityLabels.Select(kv => kv.Key).ToList());

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
                var adjustmentFactor = ParameterIsAdditive[meteoParameter] ? 1.0 : (double)meteoInterval / supportInterval;
                if (value.HasValue)
                    MeteoValuesArrays[meteoParameter][index] = (MeteoValuesArrays[meteoParameter][index] ?? 0) + value * overLapRatio * adjustmentFactor;
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
        private void NormalizeMeteoWeightArray(MeteoParameterType parameterType)
        {
            var weights = WeightMeteoArrays[parameterType].Select(w => w > 0.0 ? w : 0.0).ToArray();
            var totalWeight = weights.Sum();
            totalWeight = totalWeight > 0 ? totalWeight : 1.0;
            WeightMeteoArrays[parameterType] = weights.Select(w => w / totalWeight).ToArray();
        }

        // Aggregation helper for historical and forecast data: Process data from a single station and accumulate weighted sums
        private List<MeteoParameters>
            ProcessStationData(
            int stationIndex,
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
            var blendedWeatherData = new List<MeteoParameters>();
            var timeInterval = (supportTimeStamps[1] - supportTimeStamps[0]).Minutes;

            for (var i = 0; i < supportCount; i++)
            {
                var directRadiationVariance = radiationVarianceMaxVariance;
                if (SelectedStationsIdList.Count > 1)
                {
                    var E1i = sumSupportGlobalRadiation[i] / SelectedStationsIdList.Count; ;
                    var E2i = squaredSumSupportGlobalRadiation[i] / SelectedStationsIdList.Count;
                    directRadiationVariance = radiationBaselineVariance + (E2i - E1i * E1i) * SelectedStationsIdList.Count / (SelectedStationsIdList.Count - 1);
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

            //SetStationsWeightArrays(StationDictionary);

            foreach (var meteoParameterType in MeteoParameterTypeList)
            {
                WeightedSumMeteoValuesArrays[meteoParameterType] = new double[supportCount];
            }

            var perStationWeatherParameters = new List<StationMeteoData>();
            var sumSupportGlobalRadiation = new double[supportCount];
            var squaredSumSupportGlobalRadiation = new double[supportCount];
            var stationIndex = -1;
            foreach (var stationId in SelectedStationsIdList)
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
            List<DateTime> supportTimeStamps,
            int shiftMeteoTimeStamps = 60)                 // shift in minutes UTC -> local time
        {
            // Compute normalized weights
            //SetSelectedStationsWeightArrays();

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
            //SetStationsWeightArrays(StationDictionary);

            foreach (var meteoParameterType in MeteoParameterTypeList)
            {
                WeightedSumMeteoValuesArrays[meteoParameterType] = new double[supportCount];
            }

            var sumSupportGlobalRadiation = new double[supportCount];
            var squaredSumSupportGlobalRadiation = new double[supportCount];

            // Fetch forecasts for all stations
            var forecastClient = new WeatherForecastClient();
            var blender = new MeteoForecastSeriesBlender();
            var blendedForecastPerStation = new List<List<MeteoParameters>>();
            foreach (var stationId in SelectedStationsIdList)
            {
                var longCast = await forecastClient.Get16DayMeteoParametersByStationIdAsync(stationId);
                var midCast = await forecastClient.Get7DayMeteoParametersByStationIdAsync(stationId);
                var nowCast = await forecastClient.GetNowcast15MinuteMeteoParametersByStationIdAsync(stationId);
                var blendedForecast = await blender.CreateBlendedForecastListFromLists(DateTime.UtcNow, longCast, midCast, nowCast);
                blendedForecastPerStation.Add(blendedForecast);
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
            foreach (var stationId in SelectedStationsIdList)
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
            Dictionary<string, List<double?>> RadiationSeries, Dictionary<string, string> RadiationLabels,
            Dictionary<string, List<double?>> TemperatureSeries, Dictionary<string, string> TemperatureLabels,
            Dictionary<string, List<double?>> WindSpeedSeries, Dictionary<string, string> WindSpeedLabels,
            Dictionary<string, List<double?>> SnowDepthSeries, Dictionary<string, string> SnowDepthLabels,
            Dictionary<string, List<double?>> RelativeHumiditySeries, Dictionary<string, string> RelativeHumidityLabels
        ) FilterAndLabelSeries(List<StationMeteoData> perStationWeatherData)
        {
            var radiationSeries = new Dictionary<string, List<double?>>();
            var radiationLabels = new Dictionary<string, string>();

            var temperatureSeries = new Dictionary<string, List<double?>>();
            var temperatureLabels = new Dictionary<string, string>();

            var windSpeedSeries = new Dictionary<string, List<double?>>();
            var windSpeedLabels = new Dictionary<string, string>();

            var snowDepthSeries = new Dictionary<string, List<double?>>();
            var snowDepthLabels = new Dictionary<string, string>();

            var relativeHumiditySeries = new Dictionary<string, List<double?>>();
            var relativeHumidityLabels = new Dictionary<string, string>();

            foreach (var stationData in perStationWeatherData)
            {
                var stationId = stationData.StationId;
                var stationDataRecords = stationData.WeatherData;
                var validParameters = stationDataRecords[0].GetValidMeteoParameters();

                // Radiation
                if (validParameters.HasValidGlobalRadiation)
                {
                    var label = $"Global_{stationId}";
                    radiationSeries[label] = stationDataRecords.Select(d => d.GetValue(MeteoParameterType.GlobalRadiation)).ToList();
                    radiationLabels[label] = label;
                }
                if (validParameters.HasValidGlobalRadiation)
                {
                    var label = $"Diffuse_{stationId}";
                    radiationSeries[label] = stationDataRecords.Select(d => d.GetValue(MeteoParameterType.DiffuseRadiation)).ToList();
                    radiationLabels[label] = label;
                }
                // Temperature
                if (validParameters.HasValidTemperature)
                {
                    var label = $"Temperature_{stationId}";
                    temperatureSeries[label] = stationDataRecords.Select(d => d.GetValue(MeteoParameterType.Temperature)).ToList();
                    temperatureLabels[label] = label;
                }
                if (validParameters.HasValidDewPoint)
                {
                    var label = $"DewPoint_{stationId}";
                    temperatureSeries[label] = stationDataRecords.Select(d => d.GetValue(MeteoParameterType.DewPoint)).ToList();
                    temperatureLabels[label] = label;
                }
                //WindSpeed
                if (validParameters.HasValidWindSpeed)
                {
                    var label = $"WindSpeed_{stationId}";
                    windSpeedSeries[label] = stationDataRecords.Select(d => d.GetValue(MeteoParameterType.WindSpeed)).ToList();
                    windSpeedLabels[label] = label;
                }
                // SnowDepth
                if (validParameters.HasValidSnowDepth)
                {
                    var label = $"SnowDepth_{stationId}";
                    snowDepthSeries[label] = stationDataRecords.Select(d => d.GetValue(MeteoParameterType.SnowDepth)).ToList();
                    snowDepthLabels[label] = label;
                }
                // RelativeHumidity
                if (validParameters.HasValidRelativeHumidity)
                {
                    var label = $"Humidity_{stationId}";
                    relativeHumiditySeries[label] = stationDataRecords.Select(d => d.GetValue(MeteoParameterType.RelativeHumidity)).ToList();
                    relativeHumidityLabels[label] = label;
                }
            }

            return (
                radiationSeries, radiationLabels,
                temperatureSeries, temperatureLabels,
                windSpeedSeries, windSpeedLabels,
                snowDepthSeries, snowDepthLabels,
                relativeHumiditySeries, relativeHumidityLabels
                );
        }

        // Helper method to get weight arrays for all selected stations
        private void SetSelectedStationsWeightArrays(int folder) //Dictionary<string, WeightMeteoParameters> stationDictionary)
        {
            var folderList = SiteToProfilesDictionary.Keys.ToList();
            if (!folderList.Contains(folder))
            {
                throw new Exception($"Folder {folder} not found in SiteToProfilesDictionary.");
            }

            var stationDictionary = ProfileToStationDictionary[SiteToProfilesDictionary[folder]];
            SelectedStationsIdList = stationDictionary.Keys.ToList();
            var stationsCount = SelectedStationsIdList.Count;

            foreach (var meteoParameterType in MeteoParameterTypeList)
            {
                WeightMeteoArrays[meteoParameterType] = new double[stationsCount];
            }

            var stationIndex = 0;
            foreach (var stationId in SelectedStationsIdList)
            {
                var weights = stationDictionary[stationId];
                foreach (var meteoParameterType in MeteoParameterTypeList)
                {
                    WeightMeteoArrays[meteoParameterType][stationIndex] = weights.GetWeight(meteoParameterType);
                }

                stationIndex++;
            }

            // Compute normalized weights
            foreach (var meteoParameterType in MeteoParameterTypeList)
            {
                NormalizeMeteoWeightArray(meteoParameterType);
            }
        }
    }
}
