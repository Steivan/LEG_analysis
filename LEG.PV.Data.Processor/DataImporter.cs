using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;
using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.E3Dc.Client;
using LEG.HorizonProfiles.Client;
using LEG.MeteoSwiss.Abstractions;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using System.Data;
using System.IO;
using static LEG.PV.Data.Processor.DataRecords;

namespace LEG.PV.Data.Processor
{
    public class DataImporter
    {
        const int meteoDataOffset = 60;             // Timestamps are UTC values
        int meteoDataLag = 10;                      // Values at given timestamp represent the aggregation over previous 10 minutes
        const string meteoStationId = "SMA";
        List<string> referenceStationIds = ["KLO", "HOE", "UEB"];  // ZH: "HOE", "KLO", "LAE", "PFA", "REH", "SMA", "UEB", "WAE"

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
            var (timeStamps, geometryFactors, installedPower) = await pvProduction(siteId, firstDateTime, lastDateime, minutesPerPeriod, shiftSupportTimeStamps: 0);

            // Fetch weather parameters
            meteoDataLag = 5 * (int)Math.Round((double)meteoDataLag / 5);
            var meteoParams = LoadWeatherParameters(meteoStationId, timeStamps, shiftMeteoTimeStamps: meteoDataOffset + meteoDataLag);

            // Merge data
            var dataRecords = new List<PvRecord>();
            var validRecords = new List<bool>();
            var recordIndex = 0;
            for (var i=0; i < pvDataRecords.Count; i++)
            {
                var meteoParam = meteoParams[i];
                var pvDataRecord = pvDataRecords[i];
                var directIrradiation = meteoParam.directIrradiation ?? 0.0;
                var diffuseIrradiation = meteoParam.diffuseIrradiation ?? 0.0;
                var globalIrradiation = directIrradiation + diffuseIrradiation;
                var effectiveGeometryFactor = globalIrradiation > 0 ? (directIrradiation * geometryFactors[i] + diffuseIrradiation) / globalIrradiation : geometryFactors[i];
                var pvRecord = new PvRecord (
                    timeStamps[i], 
                    recordIndex, 
                    effectiveGeometryFactor, 
                    globalIrradiation, 
                    meteoParam.temperature ?? 0.0, 
                    meteoParam.windVelocity ?? 0.0, 
                    (double)i * minutesPerPeriod / 60.0 / 24 / 365.2422, 
                    pvDataRecord.SolarProduction
                    );

                dataRecords.Add(pvRecord);
                validRecords.Add(pvRecord.GeometryFactor > 0 || pvDataRecord.SolarProduction > 0);
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

            // Fetch reference weather parameters
            var timeStamps = dataRecords.Select(r => r.Timestamp).ToList();
            await UpdateWeatherData(timeStamps[0], referenceStationIds);
            var referenceMeteoParamsList = new List<List<(double? directIrradiation, double? diffuseIrradiation, double? temperature, double? windVelocity)>>();
            foreach (var referenceStationId in referenceStationIds)
            {
                var referenceMeteoParams = LoadWeatherParameters(referenceStationId, timeStamps, shiftMeteoTimeStamps: meteoDataOffset + meteoDataLag);
                referenceMeteoParamsList.Add(referenceMeteoParams);
            }

            // --- New Filtering Logic ---
            // 1. Collect all series and their original labels
            var allIrradiationSeries = new List<List<double?>>();
            var allTemperatureSeries = new List<List<double?>>();
            var allWindVelocitySeries = new List<List<double?>>();

            for (int i = 0; i < referenceStationIds.Count; i++)
            {
                var stationData = referenceMeteoParamsList[i];
                allIrradiationSeries.Add(stationData.Select(d => d.directIrradiation.HasValue || d.diffuseIrradiation.HasValue ? (d.directIrradiation ?? 0) + (d.diffuseIrradiation ?? 0) : (double?)null).ToList());
                allTemperatureSeries.Add(stationData.Select(d => d.temperature).ToList());
                allWindVelocitySeries.Add(stationData.Select(d => d.windVelocity).ToList());
            }

            // 2. Filter out series that are entirely null and get the valid labels
            var finalIrradiationLabels = new List<string> { $"Irradiation_{meteoStationId}" };
            var finalTemperatureLabels = new List<string> { $"AmbientTemp_{meteoStationId}" };
            var finalWindVelocityLabels = new List<string> { $"WindVelocity_{ meteoStationId}" };

            var validIrradiationSeries = allIrradiationSeries.Where((series, i) => {
                if (series.Any(val => val.HasValue)) { finalIrradiationLabels.Add($"Reference_{referenceStationIds[i]}"); return true; }
                return false;
            }).ToList();

            var validTemperatureSeries = allTemperatureSeries.Where((series, i) => {
                if (series.Any(val => val.HasValue)) { finalTemperatureLabels.Add($"Reference_{referenceStationIds[i]}"); return true; }
                return false;
            }).ToList();

            var validWindVelocitySeries = allWindVelocitySeries.Where((series, i) => {
                if (series.Any(val => val.HasValue)) { finalWindVelocityLabels.Add($"Reference_{referenceStationIds[i]}"); return true; }
                return false;
            }).ToList();
            // --- End New Filtering Logic ---

            var listsDataRecords = new List<PvRecordLists>();
            for (var index = 0; index < dataRecords.Count; index++)
            {
                var record = dataRecords[index];
                var computedPower = record.ComputedPower(modelParams[folder], installedPower);

                // Build lists for the current record, including the base series and the valid reference series
                List<double?> irradiationList = [record.Irradiation];
                irradiationList.AddRange(validIrradiationSeries.Select(series => series[index]));

                List<double?> temperatureList = [record.AmbientTemp];
                temperatureList.AddRange(validTemperatureSeries.Select(series => series[index]));

                List<double?> windVelocityList = [record.WindVelocity];
                windVelocityList.AddRange(validWindVelocitySeries.Select(series => series[index]));

                var listsDataRecord = new PvRecordLists(
                    record.Timestamp,
                    record.Index,
                    [record.MeasuredPower, computedPower],
                    irradiationList,
                    temperatureList,
                    windVelocityList
                );

                listsDataRecords.Add(listsDataRecord);
            }

            var dataRecordLabels = new PvRecordLabels(
                ["MeasuredPower", "ComputedPower"],
                finalIrradiationLabels,
                finalTemperatureLabels,
                finalWindVelocityLabels);

            return (siteId, listsDataRecords, dataRecordLabels, validRecords, installedPower, periodsPerHour);
        }

        private async Task<(List<DateTime> timeStamps, List<double> geometryFactors, double installedPower)> pvProduction(
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
            var geometryFactors = new List<double>();

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

                for (int i = 0; i < results.TimeStamps.Length; i++)
                {
                    var ts = results.TimeStamps[i];
                    if (ts >= startTime && ts <= endTime && ts.Year == year)
                    {
                        timeStamps.Add(ts);
                        geometryFactors.Add(results.TheoreticalIrradiationPerRoofAndInterval[0, i]);
                    }
                }
            }

            return (timeStamps, geometryFactors, installedKwP * 1000);
        }

        private List<(double? directIrradiation, double? diffuseIrradiation, double? temperature, double? windVelocity)> LoadWeatherParameters(
            string stationId, List<DateTime> supportTimeStamps, int shiftMeteoTimeStamps = 60)                 // shift in minutes UTC -> local time
        {

            void AllocateMeteoDataContainers(int iSupport, int iMeteo, int supportCount, int meteoInterval, int supportInterval,
                DateTime supportTimeStamp, DateTime meteoTimeStamp, WeatherCsvRecord leftRecord,
                double?[] supportDirectIrradiation, double?[] supportDiffuseIrradiation, double?[] supportTemperature, double?[] supportWindSpeed)
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

                var priorDiffuseIrradiation = leftRecord.DiffuseRadiation;
                var priorDirectIrradiation = leftRecord.GlobalRadiation.HasValue && leftRecord.DiffuseRadiation.HasValue
                    ? Math.Max(0.0, leftRecord.GlobalRadiation.Value - leftRecord.DiffuseRadiation.Value)
                    : leftRecord.GlobalRadiation;
                var priorTemperature = leftRecord.Temperature2m;
                var priorWindSpeed = leftRecord.WindSpeed10min_kmh;

                if (iLeft >= 0 && iLeft < supportCount && leftOverlapRatio > 0)
                {
                    if (priorDirectIrradiation.HasValue) supportDirectIrradiation[iLeft] = (supportDirectIrradiation[iLeft] ?? 0) + priorDirectIrradiation.Value * leftOverlapRatio;
                    if (priorDiffuseIrradiation.HasValue) supportDiffuseIrradiation[iLeft] = (supportDiffuseIrradiation[iLeft] ?? 0) + priorDiffuseIrradiation.Value * leftOverlapRatio;
                    if (priorTemperature.HasValue) supportTemperature[iLeft] = (supportTemperature[iLeft] ?? 0) + priorTemperature.Value * leftOverlapRatio;
                    if (priorWindSpeed.HasValue) supportWindSpeed[iLeft] = (supportWindSpeed[iLeft] ?? 0) + priorWindSpeed.Value * leftOverlapRatio;
                }
                if (iRight >= 0 && iRight < supportCount && rightOverlapRatio > 0)
                {
                    if (priorDirectIrradiation.HasValue) supportDirectIrradiation[iRight] = (supportDirectIrradiation[iRight] ?? 0) + priorDirectIrradiation.Value * rightOverlapRatio;
                    if (priorDiffuseIrradiation.HasValue) supportDiffuseIrradiation[iRight] = (supportDiffuseIrradiation[iRight] ?? 0) + priorDiffuseIrradiation.Value * rightOverlapRatio;
                    if (priorTemperature.HasValue) supportTemperature[iRight] = (supportTemperature[iRight] ?? 0) + priorTemperature.Value * rightOverlapRatio;
                    if (priorWindSpeed.HasValue) supportWindSpeed[iRight] = (supportWindSpeed[iRight] ?? 0) + priorWindSpeed.Value * rightOverlapRatio;
                }
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
            var supportDirectIrradiation = new double?[supportCount];
            var supportDiffuseIrradiation = new double?[supportCount];
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
                    supportDirectIrradiation, supportDiffuseIrradiation, supportTemperature, supportWindSpeed);
                iSupport++;
                while (iSupport < supportCount && iMeteo < meteoCount - 1 && alignedMeteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= supportTimeStamps[iSupport])
                {
                    iMeteo++;
                    AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval, supportInterval,
                        supportTimeStamps[iSupport], alignedMeteoTimeStamps[iMeteo], weatherRecords[iMeteo],
                        supportDirectIrradiation, supportDiffuseIrradiation, supportTemperature, supportWindSpeed);
                }
                iMeteo++;
            }

            var weatherParameters = new List<(double? directIrradiation, double? diffuseIrradiation, double? temperature, double? windVelocity)>();
            for (var i=0; i < supportCount; i++)
            {
                weatherParameters.Add((supportDirectIrradiation[i], supportDiffuseIrradiation[i], supportTemperature[i], supportWindSpeed[i]));
            }

            return weatherParameters;
        }
    }
}