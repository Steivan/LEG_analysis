using LEG.E3Dc.Abstractions;
using LEG.E3Dc.Client;
using LEG.MeteoSwiss.Client.MeteoSwiss;

namespace CalibrationApp
{
    public class Program
    {
        static async Task Main(string[] args)
        {


            // Aggregate E3DC data
            const bool e3DcAggregation = true;

            // No command line arguments are provided
            if (args.Length == 0)
            {
                var folder1 = E3DcConstants.DataFolder + E3DcConstants.SubFolder1;
                var folder2 = E3DcConstants.DataFolder + E3DcConstants.SubFolder2;

                var aggregationRecord = new E3DcAggregateArrayRecord();
                var recordsPerDay = 96;

                var arrayRecordsList1 = E3DcLoadArrayRecords.LoadE3DcArrayRecords(folder1, E3DcConstants.FirstYear1, E3DcConstants.LastYear1);
                Console.WriteLine(folder1);
                foreach (var arrayRecord in arrayRecordsList1)
                {
                    aggregationRecord.AggregatePeriodArrayRecord(arrayRecord, recordsPerDay);
                    Console.WriteLine($"Base: EvaluationYear: {arrayRecord.Year}, Records: {arrayRecord.RecordingEndIndex + 1 - arrayRecord.RecordingStartIndex}, " +
                                      $"Start: {arrayRecord.RecordingStartTime}, " +
                                      $"End: {arrayRecord.RecordingEndTime}, " +
                                      $"Complete: {arrayRecord.RecordingPeriodIsComplete()}");
 
                    Console.WriteLine($"      EvaluationYear: {aggregationRecord.Year}, Records: {aggregationRecord.RecordingEndIndex + 1 - aggregationRecord.RecordingStartIndex}, " +
                                      $"Start: {aggregationRecord.RecordingStartTime}, " +
                                      $"End: {aggregationRecord.RecordingEndTime}, " +
                                      $"Complete: {aggregationRecord.RecordingPeriodIsComplete()}");
                }

                var arrayRecordsList2 = E3DcLoadArrayRecords.LoadE3DcArrayRecords(folder2, E3DcConstants.FirstYear2, E3DcConstants.LastYear2);
                Console.WriteLine(folder2);
                foreach (var arrayRecord in arrayRecordsList2)
                {
                    aggregationRecord.AggregatePeriodArrayRecord(arrayRecord, recordsPerDay);
                    Console.WriteLine($"Base: EvaluationYear: {arrayRecord.Year}, Records: {arrayRecord.RecordingEndIndex + 1 - arrayRecord.RecordingStartIndex}, " +
                                      $"Start: {arrayRecord.RecordingStartTime}, " +
                                      $"End: {arrayRecord.RecordingEndTime}, " +
                                      $"Complete: {arrayRecord.RecordingPeriodIsComplete()}");
 
                    Console.WriteLine($"      EvaluationYear: {aggregationRecord.Year}, Records: {aggregationRecord.RecordingEndIndex + 1 - aggregationRecord.RecordingStartIndex}, " +
                                      $"Start: {aggregationRecord.RecordingStartTime}, " +
                                      $"End: {aggregationRecord.RecordingEndTime}, " +
                                      $"Complete: {aggregationRecord.RecordingPeriodIsComplete()}");
                }

                // Run E3DC aggregation
                if (e3DcAggregation)
                    E3DcAggregator.RunE3DcAggregation();

                await Task.CompletedTask;
                return;
            }

        }

    }
}