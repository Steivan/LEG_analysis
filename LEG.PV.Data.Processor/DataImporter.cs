using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;
using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.E3Dc.Client;
using LEG.HorizonProfiles.Client;
using LEG.MeteoSwiss.Abstractions;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using System.Data;
using static LEG.PV.Data.Processor.DataRecords;

namespace LEG.PV.Data.Processor
{
    public class DataImporter
    {

        public async Task<(string siteId, 
            List<PvRecord> dataRecords, 
            List<bool> validRecords,
            double installedPower, 
            int periodsPerHour)> 
            ImportE3DcData(int folder)
        {
            const string meteoStationId = "SMA";
            const int meteoDataOffset = 60;             // Timestamps are UTC values
            int meteoDataLag = 10;                      // Values at given timestamp represent the aggregation over previous 10 minutes

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
            List<PvRecordCalculated> dataRecords,
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
                new(
                    0.3,
                    -0.0015,
                    3.0,
                    0.0,
                    0.022
                )
            ];
            var (siteId, dataRecords, validRecords, installedPower, periodsPerHour) = await ImportE3DcData(folder);

            var calculateddataRecords = new List<PvRecordCalculated>();
            foreach (var record in dataRecords)
            {
                var computedPower = record.ComputedPower(modelParams[folder], installedPower);
                var calculatedDataRecord = new PvRecordCalculated(
                    record.Timestamp,
                    record.Index,
                    record.GeometryFactor,
                    record.Irradiation,
                    record.AmbientTemp,
                    record.WindVelocity,
                    record.Age,
                    record.MeasuredPower,
                    computedPower
                );   

                calculateddataRecords.Add(calculatedDataRecord);
            }

            return (siteId, calculateddataRecords, validRecords, installedPower, periodsPerHour);

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
                double[] supportDirectIrradiation, double[] supportDiffuseIrradiation, double[] supportTemperature, double[] supportWindSpeed)
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

                var priorDiffuseIrradiation = leftRecord.DiffuseRadiation ?? 0.0;
                var priorDirectIrradiation = Math.Max(0.0, (leftRecord.GlobalRadiation ?? 0.0) - priorDiffuseIrradiation);
                var priorTemperature = leftRecord.Temperature2m ?? 0.0;
                var priorWindSpeed = leftRecord.WindSpeed10min_kmh ?? 0.0;

                if (iLeft >= 0 && iLeft < supportCount && leftOverlapRatio > 0)
                {
                    supportDirectIrradiation[iLeft] += priorDirectIrradiation * leftOverlapRatio;
                    supportDiffuseIrradiation[iLeft] += priorDiffuseIrradiation * leftOverlapRatio;
                    supportTemperature[iLeft] += priorTemperature * leftOverlapRatio;
                    supportWindSpeed[iLeft] += priorWindSpeed * leftOverlapRatio;
                }
                if (iRight >= 0 && iRight < supportCount && rightOverlapRatio > 0)
                {
                    supportDirectIrradiation[iRight] += priorDirectIrradiation * rightOverlapRatio;
                    supportDiffuseIrradiation[iRight] += priorDiffuseIrradiation * rightOverlapRatio;
                    supportTemperature[iRight] += priorTemperature * rightOverlapRatio;
                    supportWindSpeed[iRight] += priorWindSpeed * rightOverlapRatio;
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
            var supportDirectIrradiation = new double[supportCount];
            var supportDiffuseIrradiation = new double[supportCount];
            var supportTemperature = new double[supportCount];
            var supportWindSpeed = new double[supportCount];
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