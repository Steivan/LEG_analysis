using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;
using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.E3Dc.Client;
using LEG.HorizonProfiles.Client;
using LEG.MeteoSwiss.Abstractions;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using static LEG.PV.Data.Processor.DataRecords;

namespace LEG.PV.Data.Processor
{
    public class DataImporter
    {

        public async Task<(List<PvRecord> dataRecords, List<bool> validRecords)> ImportE3DcData(int folder)
        {
            // Fetch pvProduction records
            folder = 1 + (folder - 1) % 2;
            var pvDataRecords = E3DcLoadPeriodRecords.LoadRecords(folder);

            var firstDate = E3DcFileHelper.ParseTimestamp(pvDataRecords[0].Timestamp);
            var secondDate = E3DcFileHelper.ParseTimestamp(pvDataRecords[1].Timestamp);
            var lastDate = E3DcFileHelper.ParseTimestamp(pvDataRecords[^1].Timestamp);
            var minutesPerPeriod = (secondDate - firstDate).Minutes;

            // Fetch geometry factors
            var siteId = folder == 1 ? ListSites.Senn : ListSites.SennV;
            var (timeStamps, geometryFactors) = await pvProduction(siteId, firstDate, lastDate, minutesPerPeriod);

            // Fetch weather parameters
            var stationId = "SMA";
            var meteoParams = LoadWeatherParameters(stationId, timeStamps);

            // Merge data
            var dataRecords = new List<PvRecord>();
            var validRecords = new List<bool>();
            var recordIndex = 0;
            for (var i=0; i < pvDataRecords.Count; i++)
            {
                var meteoParam = meteoParams[i];
                var pvDataRecord = pvDataRecords[i];
                var pvRecord = new PvRecord
                {
                    Timestamp = timeStamps[i],
                    Index = recordIndex,
                    GeometryFactor = geometryFactors[i],
                    Irradiation = meteoParam.irradiation ?? 0.0,
                    AmbientTemp = meteoParam.temperature ?? 0.0,
                    WindVelocity = meteoParam.windVelocity ?? 0.0,
                    Age = (double)i * minutesPerPeriod / 60.0 / 24 / 365.2422,
                    MeasuredPower = pvDataRecord.SolarProduction
                };

                dataRecords.Add(pvRecord);
                validRecords.Add(pvRecord.GeometryFactor > 0 || pvDataRecord.SolarProduction > 0);
            }

            return (dataRecords, validRecords);
        }

        private async Task<(List<DateTime> timeStamps, List<double> geometryFactors)> pvProduction(
            string siteId,
            DateTime startTime,
            DateTime endTime,
            int minutesPerPeriod)
        {
            var siteModel = await PvSiteModelGetters.GetSiteDataModelAsync(siteId);

            // Instantiate the HorizonProfileClient and the new data providers
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
                    shiftTimeSupport: 0,
                    print: false
                    );

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

            return (timeStamps, geometryFactors);
        }

        private List<(double? irradiation, double? temperature, double? windVelocity)> LoadWeatherParameters(
            string stationId, List<DateTime> supportTimeStamps)
        {

            void AllocateMeteoDataContainers(int iSupport, int iMeteo, int supportCount, int meteoInterval, 
                DateTime supportTimeStamp, DateTime meteoTimeStamp, WeatherCsvRecord leftRecord, 
                double[] supportIrradiation, double[] supportTemperature, double[] supportWindSpeed)
            {
                var rightOverlapRatio = (double)(meteoTimeStamp.AddMinutes(meteoInterval) - supportTimeStamp).Minutes / meteoInterval;
                rightOverlapRatio = Math.Max(0.0, Math.Min(1.0, rightOverlapRatio));
                var leftOverlapRatio = 1.0 - rightOverlapRatio;
                var priorIrradiation = leftRecord.GlobalRadiation ?? 0.0;
                var priorTemperature = leftRecord.Temperature2m ?? 0.0;
                var priorWindSpeed = leftRecord.WindSpeedScalar10min ?? 0.0;
                if (iSupport > 0 && leftOverlapRatio > 0)
                {
                    supportIrradiation[iSupport - 1] += priorIrradiation * leftOverlapRatio;
                    supportTemperature[iSupport - 1] += priorTemperature * leftOverlapRatio;
                    supportWindSpeed[iSupport - 1] += priorWindSpeed * leftOverlapRatio;
                }
                if (iSupport < supportCount && rightOverlapRatio > 0)
                {
                    supportIrradiation[iSupport] += priorIrradiation * rightOverlapRatio;
                    supportTemperature[iSupport] += priorTemperature * rightOverlapRatio;
                    supportWindSpeed[iSupport] += priorWindSpeed * rightOverlapRatio;
                }
            }

            var supportCount = supportTimeStamps.Count;
            var firstSupportTimestamp = supportTimeStamps[0];
            var secondSupportTimestamp = supportTimeStamps[1];
            var lastSupportTimestamp = supportTimeStamps[^1];
            var supportInterval = (secondSupportTimestamp - firstSupportTimestamp).Minutes;
            var upperBound = lastSupportTimestamp.AddMinutes(supportInterval);

            // Initialize list with valid ground stations
            MeteoSwissHelper.ValidGroundStations = MeteoSwissHelper.GetBaselineGroundStations();
            // Load station metadata
            var groundStationsMetaDict = StationMetaImporter.Import(MeteoSwissConstants.GroundStationsMetaFile);

            var weatherRecords = MeteoAggregator.GetFilteredRecords(
                stationId,
                groundStationsMetaDict[stationId],
                firstSupportTimestamp.Year,
                lastSupportTimestamp.Year,
                "t",
                false
                );

            var meteoTimeStamps = weatherRecords.Select(r => r.ReferenceTimestamp).ToList();

            var meteoCount = meteoTimeStamps.Count;
            var firstMeteoTimestamp = meteoTimeStamps[0];
            var secondMeteoTimestamp = meteoTimeStamps[1];
            var lastMeteoTimestamp = meteoTimeStamps[^1];
            var meteoInterval = (secondMeteoTimestamp - firstMeteoTimestamp).Minutes;

            if (meteoInterval > supportInterval)
            {
                throw new Exception("Meteo interval is larger than support interval.");
            }

            var iSupport = 0;
            var iMeteo = 0;
            var leftRecord = weatherRecords[0];
            var supportIrradiation = new double[supportCount];
            var supportTemperature = new double[supportCount];
            var supportWindSpeed = new double[supportCount];
            while (iMeteo < meteoCount - 1 && meteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= firstSupportTimestamp)
            {
                iMeteo++;
            }
            while (iSupport < supportCount && iMeteo < meteoCount && meteoTimeStamps[iMeteo] < upperBound)
            {
                AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval,
                    supportTimeStamps[iSupport], meteoTimeStamps[iMeteo], weatherRecords[iMeteo],
                    supportIrradiation, supportTemperature, supportWindSpeed);
                iSupport++;
                while (iSupport < supportCount && iMeteo < meteoCount - 1 && meteoTimeStamps[iMeteo].AddMinutes(meteoInterval) <= supportTimeStamps[iSupport])
                {
                    iMeteo++;
                    AllocateMeteoDataContainers(iSupport, iMeteo, supportCount, meteoInterval,
                        supportTimeStamps[iSupport], meteoTimeStamps[iMeteo], weatherRecords[iMeteo],
                        supportIrradiation, supportTemperature, supportWindSpeed);
                }
                iMeteo++;
            }

            var weatherParameters = new List<(double? irradiation, double? temperature, double? windVelocity)>();
            for (var i=0; i < supportCount; i++)
            {
                weatherParameters.Add((supportIrradiation[i], supportTemperature[i], supportWindSpeed[i]));
            }

            return weatherParameters;
        }
    }
}