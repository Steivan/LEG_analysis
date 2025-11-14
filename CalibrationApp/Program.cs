using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.E3Dc.Abstractions;
using LEG.E3Dc.Client;

using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.HorizonProfiles.Client;
using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;

namespace CalibrationApp
{
    public class Program
    {
        static async Task Main()
        {

            await ProcessE3DcData(1);
            await ProcessE3DcData(2);
       
            // Run E3DC aggregation
            E3DcAggregator.RunE3DcAggregation();

            await Task.CompletedTask;
        }

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
            var referenceModelId = modelNr == 1 ? ListSites.Senn : ListSites.SennV;

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