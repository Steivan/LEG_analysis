using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;
using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.E3Dc.Client;
using LEG.HorizonProfiles.Client;
using LEG.MeteoSwiss.Abstractions;
using LEG.MeteoSwiss.Abstractions.ReferenceData;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using System.Data;
using static LEG.PV.Data.Processor.DataRecords;

namespace LEG.PV.Data.Processor
{
    public class DataImporter
    {
        // see also file: C:\code\LEG_analysis\Data\MeteoData\StationsData\klo_sma_hoe_ueb_recent_16.11.2025.xlsx
        const int meteoDataOffset = 60;             // Timestamps are UTC values
        int meteoDataLag = 10;                      // Values at given timestamp represent the aggregation over previous 10 minutes
        // Selected stations, available parameters and blending weights
        List<string> selectedStationsIdList      = ["SMA", "KLO", "HOE", "UEB"];
        List<bool> hasGlobalIrradianceList       = [ true,  true,  true,  true];
        List<bool> hasDiffuseIrradianceList      = [false,  true, false,  true];
        List<bool> hasTemperatureList            = [ true,  true,  true, false];
        List<bool> hasWindSpeedList              = [ true,  true,  true, false];
        List<double> weightGlobalIrradianceList  = [  3.0,   1.0,   1.0,   1.0];
        List<double> weightListDiffuseIrradiance = [  0.0,   1.0,   0.0,   1.0];
        List<double> weightTemperatureList       = [  1.0,   1.0,   0.0,   0.0];
        List<double> weightWindSpeedList         = [  1.0,   1.0,   0.0,   0.0];

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

        public async Task<(string siteId, 
            List<PvRecord> dataRecords, 
            List<bool> validRecords,
            double installedPower, 
            int periodsPerHour)> 
            ImportE3DcData(int folder)
        {
            // Fetch pvProduction records
            folder = 1 + (folder - 1) % 2;
            var pvDataRecords = E3DcLoadPeriodRecords.LoadRecords(folder);

            var firstDateTime = E3DcFileHelper.ParseTimestamp(pvDataRecords[0].Timestamp);
            var secondDateTime = E3DcFileHelper.ParseTimestamp(pvDataRecords[1].Timestamp);
            var lastDateime = E3DcFileHelper.ParseTimestamp(pvDataRecords[^1].Timestamp);
            var minutesPerPeriod = (secondDateTime - firstDateTime).Minutes;
            var periodsPerHour = 60 / minutesPerPeriod;

            // Fetch geometry factors
            var siteId = folder == 1 ? ListSites.Senn : ListSites.SennV;
            var (timeStamps, directGeometryFactors, diffuseGeometryFactor, cosSunElevations, installedPower) = await pvProduction(siteId, firstDateTime, lastDateime, minutesPerPeriod, shiftSupportTimeStamps: 0);

            // Fetch weather parameters
            meteoDataLag = 5 * (int)Math.Round((double)meteoDataLag / 5);
            var (perStationWeatherData, blendedWeatherData) = LoadBlendedWeatherParameters(
                selectedStationsIdList,
                weightGlobalIrradianceList,
                weightListDiffuseIrradiance,
                weightTemperatureList,
                weightWindSpeedList,
                timeStamps, shiftMeteoTimeStamps: meteoDataOffset + meteoDataLag);

            // Merge data
            var dataRecords = new List<PvRecord>();
            var validRecords = new List<bool>();
            var recordIndex = 0;
            for (var i =0; i < pvDataRecords.Count; i++)
            {
                var meteoParam = blendedWeatherData[i];
                var pvDataRecord = pvDataRecords[i];
                var pvRecord = new PvRecord (
                    timeStamps[i], 
                    recordIndex,
                    directGeometryFactors[i],
                    diffuseGeometryFactor,
                    cosSunElevations[i],
                    meteoParam.directIrradiance ?? 0.0,
                    meteoParam.diffuseIrradiance ?? 0.0,
                    meteoParam.temperature ?? 0.0, 
                    meteoParam.windSpeed ?? 0.0, 
                    meteoParam.directIrradianceVariance ?? 0.0,
                    (double)i * minutesPerPeriod / 60.0 / 24 / 365.2422, 
                    pvDataRecord.SolarProduction
                    );

                dataRecords.Add(pvRecord);
                validRecords.Add(pvRecord.DirectGeometryFactor > 0 || pvDataRecord.SolarProduction > 0);
            }

            return (siteId, dataRecords, validRecords, installedPower, periodsPerHour);
        }

        public async Task<(string siteId,
            List<PvRecordLists> dataRecords,
            PvRecordLabels dataRecordLabels,
            List<bool> validRecords,
            double installedPower,
            int periodsPerHour)>
            ImportE3DcDataCalculated(int folder)
        {
            List<PvModelParams> modelParams =  [
                GetDefaultPriorModelParams(),
                new(
                    0.157,
                    -0.00196,
                    108.3,
                    0.0,
                    0.016
                ),
                new(             // SennV: elevation 35° 
                    0.12,
                    -0.002,
                    100,
                    0.0,
                    0.01
                ),
                new(
                    0.32,
                    -0.0028,
                    7.5,
                    0.0,
                    0.021
                ),
                new(             // SennV: elevation 35° 
                    0.254,
                    -0.0012,
                    3.2,
                    0.0,
                    0.0232
                )
            ];
            var (siteId, dataRecords, validRecords, installedPower, periodsPerHour) = await ImportE3DcData(folder);

            // Fetch weather parameters
            var timeStamps = dataRecords.Select(r => r.Timestamp).ToList();
            meteoDataLag = 5 * (int)Math.Round((double)meteoDataLag / 5);
            var (perStationWeatherData, blendedWeatherData) = LoadBlendedWeatherParameters(
                selectedStationsIdList,
                weightGlobalIrradianceList,
                weightListDiffuseIrradiance,
                weightTemperatureList,
                weightWindSpeedList,
                timeStamps, shiftMeteoTimeStamps: meteoDataOffset + meteoDataLag);

            // Filter: retain valid series and get the labels
            var filteredIrradianceSeries = new List<List<double?>>();
            var filteredTemperatureSeries = new List<List<double?>>();
            var filteredWindSpeedSeries = new List<List<double?>>();

            var filteredIrradianceLabels = new List<string>();
            var filteredTemperatureLabels = new List<string>();
            var filteredWindSpeedLabels = new List<string>();
            for (int i = 0; i < selectedStationsIdList.Count; i++)
            {
                var stationID = selectedStationsIdList[i];
                var stationData = perStationWeatherData[i];
                if (hasGlobalIrradianceList[i])
                {
                    filteredIrradianceSeries.Add(stationData.Select(d => d.directIrradiance).ToList());
                    filteredIrradianceLabels.Add($"Global_{stationID}");
                }
                if (hasDiffuseIrradianceList[i])
                {
                    filteredIrradianceSeries.Add(stationData.Select(d => d.diffuseIrradiance).ToList());
                    filteredIrradianceLabels.Add($"Diffuse_{stationID}");
                }
                if (hasTemperatureList[i])
                {
                    filteredTemperatureSeries.Add(stationData.Select(d => d.temperature).ToList());
                    filteredTemperatureLabels.Add($"Temperature_{stationID}");
                }
                if (hasWindSpeedList[i])
                {
                    filteredWindSpeedSeries.Add(stationData.Select(d => d.windSpeed).ToList());
                    filteredWindSpeedLabels.Add($"WindSpeed_{stationID}");
                }
            }

            //// Fetch reference weather parameters
            //var timeStamps = dataRecords.Select(r => r.Timestamp).ToList();
            //await UpdateWeatherData(timeStamps[0], referenceStationIds);
            //var referenceMeteoParamsList = new List<List<(double? directIrradiance, double? diffuseIrradiance, double? temperature, double? windSpeed, double weight)>>();
            //foreach (var referenceStationId in referenceStationIds)
            //{
            //    var referenceMeteoParams = LoadWeatherParameters(referenceStationId, timeStamps, shiftMeteoTimeStamps: meteoDataOffset + meteoDataLag);
            //    referenceMeteoParamsList.Add(referenceMeteoParams);
            //}

            // --- New Filtering Logic ---
            // 1. Collect all series and their original labels
            //var allIrradianceSeries = new List<List<double?>>();
            //var allTemperatureSeries = new List<List<double?>>();
            //var allWindSpeedSeries = new List<List<double?>>();
            //var allWeightSeries = new List<List<double?>>();

            //for (int i = 0; i < blendedStationsIdList.Count; i++)
            //{
            //    var stationData = referenceMeteoParamsList[i];
            //    allIrradianceSeries.Add(stationData.Select(d => d.directIrradiance.HasValue || d.diffuseIrradiance.HasValue ? (d.directIrradiance ?? 0) + (d.diffuseIrradiance ?? 0) : (double?)null).ToList());
            //    allTemperatureSeries.Add(stationData.Select(d => d.temperature).ToList());
            //    allWindSpeedSeries.Add(stationData.Select(d => d.windSpeed).ToList());
            //}

            //// 2. Filter out series that are entirely null and get the valid labels
            //var finalIrradianceLabels = new List<string> { $"Total_{meteoStationId}", $"Diffuse_{diffuseStationId}" };
            //var finalTemperatureLabels = new List<string> { $"{meteoStationId}" };
            //var finalWindSpeedLabels = new List<string> { $"{ meteoStationId}" };

            //var validIrradianceSeries = allIrradianceSeries.Where((series, i) => {
            //    if (series.Any(val => val.HasValue)) { finalIrradianceLabels.Add($"Reference_{referenceStationIds[i]}"); return true; }
            //    return false;
            //}).ToList();

            //var validTemperatureSeries = allTemperatureSeries.Where((series, i) => {
            //    if (series.Any(val => val.HasValue)) { finalTemperatureLabels.Add($"Reference_{referenceStationIds[i]}"); return true; }
            //    return false;
            //}).ToList();

            //var validWindSpeedSeries = allWindSpeedSeries.Where((series, i) => {
            //    if (series.Any(val => val.HasValue)) { finalWindSpeedLabels.Add($"Reference_{referenceStationIds[i]}"); return true; }
            //    return false;
            //}).ToList();
            // --- End New Filtering Logic ---

            var listsDataRecords = new List<PvRecordLists>();
            for (var index = 0; index < dataRecords.Count; index++)
            {
                var record = dataRecords[index];
                var computedPower = record.ComputedPower(modelParams[folder], installedPower);

                // Build lists for the current record, including the base series and the valid reference series
                List<double?> irradiationList = [record.GlobalHorizontalIrradiance, record.DiffuseHorizontalIrradiance];
                irradiationList.AddRange(filteredIrradianceSeries.Select(series => series[index]));

                List<double?> temperatureList = [record.AmbientTemp];
                temperatureList.AddRange(filteredTemperatureSeries.Select(series => series[index]));

                List<double?> windSpeedList = [record.WindSpeed];
                windSpeedList.AddRange(filteredWindSpeedSeries.Select(series => series[index]));

                var listsDataRecord = new PvRecordLists(
                    record.Timestamp,
                    record.Index,
                    [record.MeasuredPower, computedPower],
                    irradiationList,
                    temperatureList,
                    windSpeedList
                );

                listsDataRecords.Add(listsDataRecord);
            }

            var dataRecordLabels = new PvRecordLabels(
                ["MeasuredPower", "ComputedPower"],
                filteredIrradianceLabels,
                filteredTemperatureLabels,
                filteredWindSpeedLabels);

            return (siteId, listsDataRecords, dataRecordLabels, validRecords, installedPower, periodsPerHour);
        }

        private async Task<(List<DateTime> timeStamps, List<double> directGeometryFactors, double diffuseGeometryFactor, List<double> cosSunElevations, double installedPower)> pvProduction(
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
            var cosSunElevations = new List<double>();

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
                        cosSunElevations.Add(results.CosSunElevations[i]);
                    }
                }
            }

            return (timeStamps, directGeometryFactors, diffuseGeometryFactor, cosSunElevations, installedKwP * 1000);
        }

        private void AllocateMeteoDataContainers(int iSupport, int iMeteo, int supportCount, int meteoInterval, int supportInterval,
            DateTime supportTimeStamp, DateTime meteoTimeStamp, WeatherCsvRecord leftRecord,
            double?[] supportGlobalIrradiance, double?[] supportDiffuseIrradiance, double?[] supportTemperature, double?[] supportWindSpeed)
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

            var priorDiffuseIrradiance = leftRecord.DiffuseRadiation;
            var priorGlobalIrradiance = leftRecord.GlobalRadiation.HasValue && leftRecord.DiffuseRadiation.HasValue
                ? Math.Max(0.0, leftRecord.GlobalRadiation.Value - leftRecord.DiffuseRadiation.Value)
                : leftRecord.GlobalRadiation;
            var priorTemperature = leftRecord.Temperature2m;
            var priorWindSpeed = leftRecord.WindSpeed10min_kmh;

            if (iLeft >= 0 && iLeft < supportCount && leftOverlapRatio > 0)
            {
                if (priorGlobalIrradiance.HasValue) supportGlobalIrradiance[iLeft] = (supportGlobalIrradiance[iLeft] ?? 0) + priorGlobalIrradiance.Value * leftOverlapRatio;
                if (priorDiffuseIrradiance.HasValue) supportDiffuseIrradiance[iLeft] = (supportDiffuseIrradiance[iLeft] ?? 0) + priorDiffuseIrradiance.Value * leftOverlapRatio;
                if (priorTemperature.HasValue) supportTemperature[iLeft] = (supportTemperature[iLeft] ?? 0) + priorTemperature.Value * leftOverlapRatio;
                if (priorWindSpeed.HasValue) supportWindSpeed[iLeft] = (supportWindSpeed[iLeft] ?? 0) + priorWindSpeed.Value * leftOverlapRatio;
            }
            if (iRight >= 0 && iRight < supportCount && rightOverlapRatio > 0)
            {
                if (priorGlobalIrradiance.HasValue) supportGlobalIrradiance[iRight] = (supportGlobalIrradiance[iRight] ?? 0) + priorGlobalIrradiance.Value * rightOverlapRatio;
                if (priorDiffuseIrradiance.HasValue) supportDiffuseIrradiance[iRight] = (supportDiffuseIrradiance[iRight] ?? 0) + priorDiffuseIrradiance.Value * rightOverlapRatio;
                if (priorTemperature.HasValue) supportTemperature[iRight] = (supportTemperature[iRight] ?? 0) + priorTemperature.Value * rightOverlapRatio;
                if (priorWindSpeed.HasValue) supportWindSpeed[iRight] = (supportWindSpeed[iRight] ?? 0) + priorWindSpeed.Value * rightOverlapRatio;
            }
        }

        //private List<(double? directIrradiance, double? diffuseIrradiance, double? temperature, double? windSpeed, double weight)> LoadWeatherParameters(
        //    string stationId, List<DateTime> supportTimeStamps, int shiftMeteoTimeStamps = 60)                 // shift in minutes UTC -> local time
        //{
        //    var supportCount = supportTimeStamps.Count;
        //    var firstSupportTimestamp = supportTimeStamps[0];
        //    var secondSupportTimestamp = supportTimeStamps[1];
        //    var lastSupportTimestamp = supportTimeStamps[^1];
        //    var supportInterval = (secondSupportTimestamp - firstSupportTimestamp).Minutes;
        //    var upperBound = lastSupportTimestamp.AddMinutes(supportInterval);

        //    // Initialize list with valid ground stations
        //    MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetAllGroundStations();
        //    // Load station metadata
        //    var groundStationsMetaDict = StationMetaImporter.Import(MeteoSwissConstants.GroundStationsMetaFile);
        //    var firstYear = firstSupportTimestamp.AddMinutes(-shiftMeteoTimeStamps).Year;
        //    var lastYear = lastSupportTimestamp.AddMinutes(-shiftMeteoTimeStamps).Year;
        //    var weatherRecords = MeteoAggregator.GetFilteredRecords(
        //        stationId,
        //        groundStationsMetaDict[stationId],
        //        firstYear,
        //        lastYear,
        //        "t",
        //        false
        //        );

        //    var alignedMeteoTimeStamps = weatherRecords.Select(r => r.ReferenceTimestamp.AddMinutes(shiftMeteoTimeStamps)).ToList();

        //    var meteoCount = alignedMeteoTimeStamps.Count;
        //    var firstMeteoTimestamp = alignedMeteoTimeStamps[0];
        //    var secondMeteoTimestamp = alignedMeteoTimeStamps[1];
        //    var lastMeteoTimestamp = alignedMeteoTimeStamps[^1];
        //    var meteoInterval = (secondMeteoTimestamp - firstMeteoTimestamp).Minutes;

        //    if (meteoInterval > supportInterval)
        //    {
        //        throw new Exception("Meteo interval is larger than support interval.");
        //    }

        //    var iSupport = 0;
        //    var iMeteo = 0;
        //    var leftRecord = weatherRecords[0];
        //    var supportGlobalIrradiance = new double?[supportCount];
        //    var supportDiffuseIrradiance = new double?[supportCount];
        //    var supportTemperature = new double?[supportCount];
        //    var supportWindSpeed = new double?[supportCount];
        //    while (iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= firstSupportTimestamp)
        //    {
        //        iMeteo++;
        //    }
        //    while (iSupport < supportCount && iMeteo < meteoCount && alignedMeteoTimeStamps[iMeteo] < upperBound)
        //    {
        //        AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
        //            supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], weatherRecords[iMeteo],
        //            supportGlobalIrradiance, supportDiffuseIrradiance, supportTemperature, supportWindSpeed);
        //        iSupport++;
        //        while (iSupport < supportCount && iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= supportTimeStamps[iSupport])
        //        {
        //            iMeteo++;
        //            AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
        //                supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], weatherRecords[iMeteo],
        //                supportGlobalIrradiance, supportDiffuseIrradiance, supportTemperature, supportWindSpeed);
        //        }
        //        iMeteo++;
        //    }

        //    var weatherParameters = new List<(double? directIrradiance, double? diffuseIrradiance, double? temperature, double? windSpeed, double weight)>();
        //    for (var i=0; i < supportCount; i++)
        //    {
        //        weatherParameters.Add((supportGlobalIrradiance[i], supportDiffuseIrradiance[i], supportTemperature[i], supportWindSpeed[i], 1.0));
        //    }

        //    return weatherParameters;
        //}

        private (
            List<List<(
                double? directIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed)>
            > perStationWeatherData,
            List<(
                double? directIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed,
                double? directIrradianceVariance
                )> blendedWeatherData)
            LoadBlendedWeatherParameters(
            List<string> blendedStationsIdList,
            List<double> weightListGlobalIrradiance,
            List<double> weightListDiffuseIrradiance,
            List<double> weightListTemperature,
            List<double> weightListWindSpeed,
            List<DateTime> supportTimeStamps, 
            int shiftMeteoTimeStamps = 60)                 // shift in minutes UTC -> local time
        {
            double[] GetWeights(List<double> weightList, int count)
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

            var supportCount = supportTimeStamps.Count;
            var firstSupportTimestamp = supportTimeStamps[0];
            var secondSupportTimestamp = supportTimeStamps[1];
            var lastSupportTimestamp = supportTimeStamps[^1];
            var supportInterval = (secondSupportTimestamp - firstSupportTimestamp).Minutes;
            var upperBound = lastSupportTimestamp.AddMinutes(supportInterval);

            // Initialize list with valid ground stations
            MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetAllGroundStations();
            // Load station metadata
            var groundStationsMetaDict = StationMetaImporter.Import(MeteoSwissConstants.GroundStationsMetaFile);
            var firstYear = firstSupportTimestamp.AddMinutes(-shiftMeteoTimeStamps).Year;
            var lastYear = lastSupportTimestamp.AddMinutes(-shiftMeteoTimeStamps).Year;

            var perStationWeatherParameters = new List<
                List<(
                double? directIrradiance,
                double? diffuseIrradiance,
                double? temperature,
                double? windSpeed)>
                >();

            var countStations = blendedStationsIdList.Count;
            var weightGlobalIrradiance = GetWeights(weightListGlobalIrradiance, countStations);
            var weightDiffuseIrradiance = GetWeights(weightListDiffuseIrradiance, countStations);
            var weightTemperature = GetWeights(weightListTemperature, countStations);
            var weightWindSpeed = GetWeights(weightListWindSpeed, countStations);

            var weightedSumSupportGlobalIrradiance = new double[supportCount];
            var weightedSquaredSumSupportGlobalIrradiance = new double[supportCount];
            var weightedSumSupportDiffuseIrradiance = new double[supportCount];
            var weightedSumSupportTemperature = new double[supportCount];
            var weightedSumSupportWindSpeed = new double[supportCount];
            var stationIndex = -1;
            foreach (var stationId in blendedStationsIdList)
            {
                stationIndex++;

                var weatherRecords = MeteoAggregator.GetFilteredRecords(
                    stationId,
                    groundStationsMetaDict[stationId],
                    firstYear,
                    lastYear,
                    "t",
                    false
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

                var iSupport = 0;
                var iMeteo = 0;
                var leftRecord = weatherRecords[0];
                var supportGlobalIrradiance = new double?[supportCount];
                var supportDiffuseIrradiance = new double?[supportCount];
                var supportTemperature = new double?[supportCount];
                var supportWindSpeed = new double?[supportCount];
                while (iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= firstSupportTimestamp)
                {
                    iMeteo++;
                }
                while (iSupport < supportCount && iMeteo < meteoCount && alignedMeteoTimeStamps[iMeteo] < upperBound)
                {
                    AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
                        supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], weatherRecords[iMeteo],
                        supportGlobalIrradiance, supportDiffuseIrradiance, supportTemperature, supportWindSpeed);
                    iSupport++;
                    while (iSupport < supportCount && iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= supportTimeStamps[iSupport])
                    {
                        iMeteo++;
                        AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
                            supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], weatherRecords[iMeteo],
                            supportGlobalIrradiance, supportDiffuseIrradiance, supportTemperature, supportWindSpeed);
                    }
                    iMeteo++;
                }

                var weatherParameters =
                    new List<(
                    double? directIrradiance,
                    double? diffuseIrradiance,
                    double? temperature,
                    double? windSpeed)>();
                for (var i = 0; i < supportCount; i++)
                {
                    weatherParameters.Add((
                        supportGlobalIrradiance[i], 
                        supportDiffuseIrradiance[i], 
                        supportTemperature[i], 
                        supportWindSpeed[i]));

                    var gG = supportGlobalIrradiance[i] ?? 0.0;
                    var gD = supportDiffuseIrradiance[i] ?? 0.0;
                    var aT = supportTemperature[i] ?? 0.0;
                    var vW = supportWindSpeed[i] ?? 0.0;
                    weightedSumSupportGlobalIrradiance[i] += gG * weightGlobalIrradiance[stationIndex];
                    weightedSquaredSumSupportGlobalIrradiance[i] += gG * gG * weightGlobalIrradiance[stationIndex];
                    weightedSumSupportDiffuseIrradiance[i] += gD * weightDiffuseIrradiance[stationIndex];
                    weightedSumSupportTemperature[i] += aT * weightTemperature[stationIndex];
                    weightedSumSupportWindSpeed[i] += vW * weightWindSpeed[stationIndex];
                }
                perStationWeatherParameters.Add(weatherParameters);
            }

            var blendedWeatherData =
                    new List<(
                    double? directIrradiance,
                    double? diffuseIrradiance,
                    double? temperature,
                    double? windSpeed,
                    double? directIrradianceVariance)>();

            for (var i = 0; i < supportCount; i++)
            {
                var E1i = weightedSumSupportGlobalIrradiance[i];
                var E2i = weightedSquaredSumSupportGlobalIrradiance[i];
                blendedWeatherData.Add((
                    weightedSumSupportGlobalIrradiance[i],
                    weightedSumSupportDiffuseIrradiance[i],
                    weightedSumSupportTemperature[i],
                    weightedSumSupportWindSpeed[i],
                    E2i - E1i * E1i));
            }

            return (perStationWeatherParameters, blendedWeatherData);
        }
    }
}