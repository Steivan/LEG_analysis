using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.E3Dc.Abstractions;
using LEG.E3Dc.Client;
using System.Reflection.Metadata.Ecma335;
using System.Security.Policy;

namespace CalibrationApp
{
    public class Program
    {
        static async Task Main()
        {

            ProcessE3DcData(E3DcConstants.DataFolder, E3DcConstants.SubFolder1, E3DcConstants.FirstYear1, E3DcConstants.LastYear1, 96);
            ProcessE3DcData(E3DcConstants.DataFolder, E3DcConstants.SubFolder2, E3DcConstants.FirstYear2, E3DcConstants.LastYear2, 96);
       
            // Run E3DC aggregation
            E3DcAggregator.RunE3DcAggregation();

            await Task.CompletedTask;
        }

        public static void ProcessE3DcData(
            string dataFolder, string subFolder,
            int firstYear, int lastYear,
            int recordsPerDay)
        {
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

            var mergedSolarProduction = MergeSolarProductionAggregateResults(solarProductionList);

            //PlotE3DcProfiles.ProductionProfilePlot(solarProductionList[0]);
            //PlotE3DcProfiles.ProductionProfilePlot(solarProductionList[1]);
            //PlotE3DcProfiles.ProductionProfilePlot(solarProductionList[^2]);
            //PlotE3DcProfiles.ProductionProfilePlot(solarProductionList[^1]);

            PlotE3DcProfiles.ProductionProfilePlot(mergedSolarProduction, countYears: solarProductionList.Count);
        }

        public static SolarProductionAggregateResults MergeSolarProductionAggregateResults(
            List<SolarProductionAggregateResults> solarProductionList,
            string siteId = "",
            string town = "",
            int utcShift = -1,
            int nrOfRoofs = 1
            )
        {
            if (solarProductionList.Count == 0)
                throw new ArgumentException("The solar production list is empty.");

            // Source data
            var observationYears = solarProductionList.Count;
            const int dimensionYear = 13;
            const int dimensionDay = 25;

            // Target data
            var hullProductionRatio = new double[1 + observationYears, dimensionYear, dimensionDay];
            var effectiveProductionRatio = new double[1 + observationYears, dimensionYear, dimensionDay];
            //var sumProductionHours = new double[1 + observationYears, dimensionYear, dimensionDay];
            var reNormalizeFactors = new double[1 + observationYears];
            var countPerMonth = new int[dimensionYear];
            var hullYear = new double[1 + observationYears, dimensionYear];
            var effectiveYear = new double[1 + observationYears, dimensionYear];

            // Renormalization factors
            for (var yearIndex = 0; yearIndex < observationYears; yearIndex++)
            {
                reNormalizeFactors[1 + yearIndex] = solarProductionList[yearIndex].PeakPowerPerRoof[0];
            }
            var maxPeakPower = reNormalizeFactors.Max();
            for (var yearIndex = 0; yearIndex < observationYears; yearIndex++)
            {
                reNormalizeFactors[1 + yearIndex] /= maxPeakPower;
            }

            for (var yearIndex = 0; yearIndex < observationYears; yearIndex++)
            {
                var production = solarProductionList[yearIndex];

                // Renormalize theoretical and effective aggregation
                for (var month = 0; month < dimensionYear; month++)
                {
                    for (var hour = 0; hour < dimensionDay; hour++)
                    {
                        hullProductionRatio[1 + yearIndex, month, hour] = production.TheoreticalAggregation[1, month, hour] * reNormalizeFactors[1 + yearIndex];
                        effectiveProductionRatio[1 + yearIndex, month, hour] = production.EffectiveAggregation[1, month, hour] * reNormalizeFactors[1 + yearIndex];
                    }
                }
            }

            // Fill gaps with weighted average curves
            for (var month = 0; month < dimensionYear; month++)
            {
                var weightSum = 0.0;
                var avgHullCurve = new double[dimensionDay];
                var avgEffectiveCurve = new double[dimensionDay];

                var yearsCount = 0;
                var hullProductionMean = 0.0;
                var effectiveProductionMean = 0.0;
                // Aggregate valid curves for the month
                for (var yearIndex = 0; yearIndex < observationYears; yearIndex++)
                {
                    var production = solarProductionList[yearIndex];
                    var weight = production.CountPerMonth[month];
                    if (weight > 0)
                    {
                        weightSum += weight;
                        for (var hour = 0; hour < dimensionDay; hour++)
                        {
                            avgHullCurve[hour] += hullProductionRatio[1 + yearIndex, month, hour] * weight;
                            avgEffectiveCurve[hour] += effectiveProductionRatio[1 + yearIndex, month, hour] * weight;
                        }

                        var hullProduction = production.TheoreticalMonth[0][month];
                        var effectiveProduction = production.EffectiveMonth[0][month];

                        hullYear[1 + yearIndex, month] = hullProduction;
                        hullYear[1 + yearIndex, 0] += hullProduction;

                        effectiveYear[1 + yearIndex, month] = effectiveProduction;
                        effectiveYear[1 + yearIndex, 0] += effectiveProduction;

                        yearsCount++;
                        hullProductionMean += hullProduction;
                        effectiveProductionMean += effectiveProduction;
                    }
                }
                // Compute weighted average curve and production for the month
                if (weightSum > 0)
                {
                    for (var hour = 0; hour < dimensionDay; hour++)
                    {
                        avgHullCurve[hour] /= weightSum;
                        avgEffectiveCurve[hour] /= weightSum;
                    }
                }
                if (yearsCount > 0)
                {
                    hullProductionMean /= yearsCount;
                    effectiveProductionMean /= yearsCount;
                }
                // Replace missing monthly curve for with average curve
                for (var yearIndex = 0; yearIndex < observationYears; yearIndex++)
                {
                    if (solarProductionList[yearIndex].CountPerMonth[month] <= 0)
                    {
                        for (var hour = 0; hour < dimensionDay; hour++)
                        {
                            hullProductionRatio[1 + yearIndex, month, hour] = avgHullCurve[hour];
                            effectiveProductionRatio[1 + yearIndex, month, hour] = avgEffectiveCurve[hour];
                        }
                        hullYear[1 + yearIndex, month] = hullProductionMean;
                        hullYear[1 + yearIndex, 0] += hullProductionMean;

                        effectiveYear[1 + yearIndex, month] = effectiveProductionMean;
                        effectiveYear[1 + yearIndex, 0] += effectiveProductionMean;
                    }
                }

                // Capture production per month and year
                for (var yearIndex = 0; yearIndex < observationYears; yearIndex++)
                {
                    countPerMonth[month] = yearsCount <= 0 ? 0 : (int)(weightSum / yearsCount);
                }
            }

            var hullMonthList = new List<double[]>();
            var hullYearList = new List<double>();

            var effectiveMonthList = new List<double[]>();
            var effectiveYearList = new List<double>();

            for (var yearIndex = 0; yearIndex < observationYears; yearIndex++)
            {
                var monthlyHullPower = new double[dimensionYear];
                for (var month = 0; month < dimensionYear; month++)
                    monthlyHullPower[month] = hullYear[1 + yearIndex, month];
                hullMonthList.Add(monthlyHullPower);
                hullYearList.Add(hullYear[1 + yearIndex, 0]);

                var monthlyEffectivePower = new double[dimensionYear];
                for (var month = 0; month < dimensionYear; month++)
                    monthlyEffectivePower[month] = effectiveYear[1 + yearIndex, month];
                effectiveMonthList.Add(monthlyEffectivePower);
                effectiveYearList.Add(effectiveYear[1 + yearIndex, 0]);
            }

            return new SolarProductionAggregateResults(
                SiteId: siteId,
                Town: town,
                EvaluationYear: DateTime.Now.Year,
                UtcShift: utcShift,
                DimensionRoofs: observationYears,
                PeakPowerPerRoof: [.. reNormalizeFactors[1..].Select(v => maxPeakPower)],
                TheoreticalAggregation: hullProductionRatio,
                EffectiveAggregation: effectiveProductionRatio,
                CountPerMonth: countPerMonth,
                TheoreticalMonth: hullMonthList,
                EffectiveMonth: effectiveMonthList,
                TheoreticalYear: hullYearList,
                EffectiveYear: effectiveYearList
            );
        }
    }
}