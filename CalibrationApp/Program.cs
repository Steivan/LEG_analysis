using CalibrationApp.Consumption;
using CalibrationApp.Helpers;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;
using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.HorizonProfiles.Client;
using LEG.PvImport.Abstractions.E3Dc.Abstractions;
using LEG.PvImport.Clients.E3Dc.Client;

namespace CalibrationApp
{
    public class Program
    {
        static async Task Main()
        {
            //AnalyzeConfinedGaussianVariance(0.1);

            //await ProcessE3Dc();

            // Analyze E3DC consumption data
            var siteId = "Senn";
            var consumptionDictionary = E3DcLoadPeriodRecords.LoadConsumptionDictionary(siteId);       
            
            var diurnalStats = DiurnalSeasonalAnalysis.AnalyzeSeasonalConsistency(consumptionDictionary);
            //PlotDiurnalConsumptionProfiles.Plot13x4DiurnalProfiles(siteId, diurnalStats);

            //var weekdayStats = WeekdaySeasonalAnalysis.AnalyzeWeekdaySeasonality(consumptionDictionary);
            //PlotWeeklyConsumptionProfiles.Plot13x4WeeklyProfiles(siteId, weekdayStats);

            var lagSinus = 3.0;
            var lagPeaks = new double[] { 8.0, 14.0, 20.0};
            var variancePeaks = new double[] { 5.0, 5.0, 5.0};

            var p90List = new List<double[]>();
            var p75List = new List<double[]>();
            var p50List = new List<double[]>();
            var p25List = new List<double[]>();
            var meanList = new List<double[]>();
            for (var i = 0; i < 13; i++)
            { Console.WriteLine($"Processing period {i + 1}/13");
                var p90Data = new double[96];
                var p75Data = new double[96];
                var p50Data = new double[96];
                var p25Data = new double[96];
                var meanData = new double[96];
                for (var j = 0; j < 96; j++)
                {
                    var index = i * 96 + j;
                    p90Data[j] = diurnalStats[index].P90;
                    p75Data[j] = diurnalStats[index].P75;
                    p50Data[j] = diurnalStats[index].P50;
                    p25Data[j] = diurnalStats[index].P25;
                    meanData[j] = diurnalStats[index].Mean;
                }
                var (smoothedP90, p90PeaksList) = PeakDetector.ExtractAllSpikes("P90", p90Data, minAmplitudeRatio: 0.25, maxSigma: 5.0, thresholdRatio: 0.1);
                p90List.Add(smoothedP90);
                var (smoothedP75, p75PeaksList) = PeakDetector.ExtractAllSpikes("P75", p75Data, minAmplitudeRatio: 0.25, maxSigma: 5.0, thresholdRatio: 0.1);
                p75List.Add(smoothedP75);
                var (smoothedP50, p50PeaksList) = PeakDetector.ExtractAllSpikes("P50", p50Data, minAmplitudeRatio: 0.25, maxSigma: 5.0, thresholdRatio: 0.1);
                p50List.Add(smoothedP50);
                var (smoothedP25, p25PeaksList) = PeakDetector.ExtractAllSpikes("P25", p25Data, minAmplitudeRatio: 0.25, maxSigma: 5.0, thresholdRatio: 0.1);
                p25List.Add(smoothedP25);
                var (smoothedMean, meanPeaksList) = PeakDetector.ExtractAllSpikes("Mean", meanData, minAmplitudeRatio: 0.25, maxSigma: 5.0, thresholdRatio: 0.1);
                meanList.Add(smoothedMean);

                BaselineDecomposer.DecomposeSeries(smoothedMean, lagSinus, lagPeaks, variancePeaks);
            }


            var p90Aggregate = new double[96];
            var p75Aggregate = new double[96];
            var p50Aggregate = new double[96];
            var p25Aggregate = new double[96];
            var meanAggregate = new double[96];
            for (var i = 0; i < 13; i++)
            {
                var smoothedP90 = p90List[i];
                var smoothedP75 = p75List[i];
                var smoothedP50 = p50List[i];
                var smoothedP25 = p25List[i];
                var smoothedMean = meanList[i];
                for (var j = 0; j < 96; j++)
                {
                    var index = i * 96 + j;
                    diurnalStats[index].P90 = smoothedP90[j];
                    diurnalStats[index].P75 = smoothedP75[j];
                    diurnalStats[index].P50 = smoothedP50[j];
                    diurnalStats[index].P25 = smoothedP25[j];
                    diurnalStats[index].Mean = smoothedMean[j];

                    p90Aggregate[j] += smoothedP90[j] / 13;
                    p75Aggregate[j] += smoothedP75[j] / 13;
                    p50Aggregate[j] += smoothedP50[j] / 13;
                    p25Aggregate[j] += smoothedP25[j] / 13;
                    meanAggregate[j] += smoothedMean[j] / 13;
                }
            }
            PlotDiurnalConsumptionProfiles.Plot13x4DiurnalProfiles(siteId, diurnalStats);

            PlotAggregateConsumptionProfile.PlotDiurnalProfiles(siteId, p90Aggregate, p75Aggregate, p50Aggregate, p25Aggregate, meanAggregate);
        }

        public static void AnalyzeConfinedGaussianVariance(double threshold, int steps=20)
        {

            var sigmaList = new List<double> { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0 };
            var count = 0;
            var sumRatioAll = 0.0;
            var sumRatioConfined = 0.0;
            foreach (var sigma in sigmaList)
            {
                var (ratioAll, ratioConfined) = GaussianFitter.DiscreteVarianceRatios(steps, sigma, threshold: threshold);
                count++;
                sumRatioAll += ratioAll;
                sumRatioConfined += ratioConfined;
                Console.WriteLine($"Sigma: {sigma:F1}, ErrorAll: {ratioAll - 1.0:E4}, RatioConfined: {ratioConfined:F4}");
            }
            Console.WriteLine($"Averages:   ErrorAll: {sumRatioAll / count - 1.0:E4}, RatioConfined: {sumRatioConfined / count:F4}");
        }

        public static async Task ProcessE3Dc()
        {

            await ProcessE3DcData(1);
            await ProcessE3DcData(2);

            // Run E3DC aggregation
            E3DcAggregator.RunE3DcAggregation();

            await Task.CompletedTask;        }

        public static async Task ProcessE3DcData(int modelNr)
        {
            modelNr = 1 + (modelNr -1) % 2;

            // E3DC data parameters
            string dataFolder = E3DcConstants.DataFolder;
            string subFolder = modelNr == 1 ? E3DcConstants.SubFolder1 : E3DcConstants.SubFolder2;
            int firstYear = modelNr == 1 ? E3DcConstants.FirstYear1 : E3DcConstants.FirstYear2;
            int lastYear = modelNr == 1 ? E3DcConstants.LastYear1 : E3DcConstants.LastYear2;
            int recordsPerDay = 96;

            // PV Reference model
            var referenceModelId = modelNr == 1 ? SiteNamesList.Senn : SiteNamesList.SennV;

            var folder = dataFolder + subFolder;
            var aggregationRecord = new E3DcAggregateArrayRecord();

            var arrayRecordsList = E3DcLoadArrayRecords.LoadE3DcArrayRecords(folder, firstYear, lastYear);
            var solarProductionList = new List<SolarProductionAggregateResults>();
            Console.WriteLine(folder);
            foreach (var arrayRecord in arrayRecordsList)
            {
                aggregationRecord.AggregatePeriodArrayRecord(arrayRecord, recordsPerDay);

                Console.WriteLine($"Base: EvaluationYear: {arrayRecord.Year}, Records: {arrayRecord.RecordingEndIndex + 1 - arrayRecord.RecordingStartIndex}, " +
                                    $"Start: {arrayRecord.RecordingStartTime}, " +
                                    $"End: {arrayRecord.RecordingEndTime}, " +
                                    $"Complete: {arrayRecord.RecordingPeriodIsComplete()}");

                solarProductionList.Add(E3DcAggregator.MapToSolarProductionAggregateResults(
                    aggregationRecord,
                    siteId: $"{subFolder}_{arrayRecord.Year}",
                    town: "Maur",
                    nrOfRoofs: 1
                    )
                );

                Console.WriteLine($"      EvaluationYear: {aggregationRecord.Year}, Records: {aggregationRecord.RecordingEndIndex + 1 - aggregationRecord.RecordingStartIndex}, " +
                                    $"Start: {aggregationRecord.RecordingStartTime}, " +
                                    $"End: {aggregationRecord.RecordingEndTime}, " +
                                    $"Complete: {aggregationRecord.RecordingPeriodIsComplete()}");
            }

            var mergedSolarProduction = MergeSolarProduction.MergeSolarProductionAggregateResults(solarProductionList);

            SolarProductionAggregateResults? referenceModel = await GetReferenceModel(referenceModelId, siteAggregate: true);

            //await PlotE3DcProfiles.ProductionProfilePlot(referenceModel);

            //await PlotE3DcProfiles.ProductionProfilePlot(solarProductionList[0]);
            //await PlotE3DcProfiles.ProductionProfilePlot(solarProductionList[1]);
            //await PlotE3DcProfiles.ProductionProfilePlot(solarProductionList[^2]);
            //await PlotE3DcProfiles.ProductionProfilePlot(solarProductionList[^1]);

            //await PlotE3DcProfiles.ProductionProfilePlot(mergedSolarProduction, countYears: solarProductionList.Count);

            var referenceModelAdjustmentFactors = CalibrateionModel.GetTimeSlotCalibrationFactors(
                solarProductionList,
                referenceModel!,
                startHour: 12,
                endHour: 18
                );

            bool adjustReferenceModel = true;
            await PlotCombinedProfiles.ProductionProfilePlot(solarProductionList, referenceModel!, referenceModelAdjustmentFactors, adjustReferenceModel, 2000 + firstYear);
        }

        public static async Task<SolarProductionAggregateResults?> GetReferenceModel(
            string sampleId, 
            int evaluationYear = 2025, 
            int minutesPerPeriod = 10,
            int shiftTimeSupport = 0,           // E3Dc: new data download on 13.11.2025 -> no shift
            bool siteAggregate = false)
        {
            var siteModel = await PvSiteModelGetters.GetSiteDataModelAsync(sampleId);

            // Instantiate the HorizonProfileClient and the new data providers
            var apiKey = Environment.GetEnvironmentVariable("GOOGLE_ELEVATION_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("Google Elevation API key is not set. Please set the 'GOOGLE_ELEVATION_API_KEY' environment variable.");
                return null;
            }
            var horizonClient = new HorizonProfileClient(googleApiKey: apiKey!);
            var coordinateProvider = new SampleSiteCoordinateProvider();
            var horizonControlProvider = new SampleSiteHorizonControlProvider();

            if (siteAggregate)
            {
                // Return production for all roofs aggregated into a single "notional" roof
                return await siteModel.ComputePvSiteAggregateProductionPerSite(
                horizonClient,
                coordinateProvider,
                horizonControlProvider,
                evaluationYear: evaluationYear,
                evaluationStartHour: 4,
                evaluationEndHour: 22,
                minutesPerPeriod: minutesPerPeriod,
                shiftTimeSupport: shiftTimeSupport,
                print: false
                );
            }
            else
            {
                // Return production for individual roofs
                return await siteModel.ComputePvSiteAggregateProductionPerRoof(
                    horizonClient,
                    coordinateProvider,
                    horizonControlProvider,
                    evaluationYear: evaluationYear,
                    evaluationStartHour: 4,
                    evaluationEndHour: 22,
                    minutesPerPeriod: minutesPerPeriod,
                    shiftTimeSupport: shiftTimeSupport,
                    print: false
                    );
            }
        }
    }
}