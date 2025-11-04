using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace CalibrationApp
{
    public class DecomposeRecords
    {
        public static (
            (int dimCurves, int dimMonth, int dimHours) dimensions, 
            (double referenceMaxPower, double overallPeakPower, double[] referenceModelPowerPerMonth) power,
            (List<List<double[]>> relativeYearMonthLists,
            List<double[]> referenceAbsoluteMonthList,
            List<double[]> productionAbsoluteMonthMeanList,
            List<double[]> productionAbsoluteMonthMinList,
            List<double[]> productionAbsoluteMonthMaxList) maxima,
            (List<List<double[]>> relativeYearMonthLists,
            List<double[]> referenceAbsoluteMonthList,
            List<double[]> productionAbsoluteMonthMeanList,
            List<double[]> productionAbsoluteMonthMinList,
            List<double[]> productionAbsoluteMonthMaxList) effective
            ) ExtractProfileLists(
            List<SolarProductionAggregateResults> annualProductionList,
            SolarProductionAggregateResults referenceModel,
            double[] calibrationFactors,
            bool reCalibrateReferenceModel = false)
        {
            var countYears = annualProductionList.Count;
            var dimCurves = 1 + countYears;
            const int dimMonth = 13;
            const int dimHours = 24;

            if (!reCalibrateReferenceModel)
            {
                calibrationFactors = (new double[13]).Select(v => 1.0).ToArray();
            }

            // Lists to hold reference and pruduction relative data
            var maximaRelativeYearMonthLists = new List<List<double[]>>();
            var effectiveRelativeYearMonthLists = new List<List<double[]>>();

            // Lists to hold reference data
            var referenceMaximaAbsoluteMonthList = new List<double[]>();
            var referenceEffectiveAbsoluteMonthList = new List<double[]>();

            var productionMaximaAbsoluteMonthMeanList = new List<double[]>();
            var productionMaximaAbsoluteMonthMinList = new List<double[]>();
            var productionMaximaAbsoluteMonthMaxList = new List<double[]>();

            var productionEffectiveAbsoluteMonthMeanList = new List<double[]>();
            var productionEffectiveAbsoluteMonthMinList = new List<double[]>();
            var productionEffectiveAbsoluteMonthMaxList = new List<double[]>();

            // Temporary arrays for aggregating production data
            var productionMaximaAbsoluteMeanArray = new double[dimMonth, dimHours];
            var productionMaximaAbsoluteMinArray = new double[dimMonth, dimHours];
            var productionMaximaAbsoluteMaxArray = new double[dimMonth, dimHours];

            var productionEffectiveAbsoluteMeanArray = new double[dimMonth, dimHours];
            var productionEffectiveAbsoluteMinArray = new double[dimMonth, dimHours];
            var productionEffectiveAbsoluteMaxArray = new double[dimMonth, dimHours];

            // Get overall peak power and renormaliyation factors
            var referenceModelPowerPerMonth = referenceModel.EffectiveMonth[0].Select(v => v).ToArray();
            var overallPeakPower = referenceModel.PeakPowerPerRoof[0];
            foreach (var annualProduction in annualProductionList)
            {
                overallPeakPower = Math.Max(overallPeakPower, annualProduction.PeakPowerPerRoof[0]);
            }
            if (overallPeakPower <= 0.0) overallPeakPower = 1.0;

            var annualReNormalizeFactors = new double[dimCurves];
            annualReNormalizeFactors[0] = referenceModel.PeakPowerPerRoof[0] / overallPeakPower;
            for (var annualIndex = 1; annualIndex < dimCurves; annualIndex++)
            {
                annualReNormalizeFactors[annualIndex] = annualProductionList[annualIndex - 1].PeakPowerPerRoof[0] / overallPeakPower;
            }

            // Process Reference (annualIndex 0) and Production (annualIndex > 0) profiles
            var referenceMaxPower = 0.0;
            var productionCountPerMonth = new double[dimMonth];
            for (var annualIndex = 0; annualIndex < dimCurves; annualIndex++)
            {
                var isReference = annualIndex == 0;
                var annualDataRecord = isReference ? referenceModel : annualProductionList[annualIndex - 1];

                var annualPeakPowerPerSite = annualDataRecord.PeakPowerPerRoof[0];
                var annualMaximaAggregation = annualDataRecord.TheoreticalAggregation;
                var annualEffectiveAggregation = annualDataRecord.EffectiveAggregation;

                var annualMaximaRelativeYearMean = new List<double[]>();
                var annualEffectiveRelativeYearMean = new List<double[]>();

                for (var month = 1; month < dimMonth; month++)
                {
                    var annualMaximaAggregationMonth = 0.0;
                    for (var hour = 0; hour < dimHours; hour++)
                    {
                        annualMaximaAggregationMonth += annualMaximaAggregation[1, month, 1 + hour]; // kW
                    }
                    if (!isReference && annualMaximaAggregationMonth > 0)
                    {
                        productionCountPerMonth[month] += 1.0;
                    }
                    var maximaRelativeMonth = new double[dimHours];
                    var effectiveRelativeMonth = new double[dimHours];

                    var maximaAbsoluteMean = new double[dimHours];
                    var effectiveAbsoluteMean = new double[dimHours];

                    for (var hour = 0; hour < dimHours; hour++)
                    {
                        var hourIndex = 1 + hour;
                        maximaRelativeMonth[hour] = annualMaximaAggregation[1, month, hourIndex] * annualReNormalizeFactors[annualIndex];
                        effectiveRelativeMonth[hour] = annualEffectiveAggregation[1, month, hourIndex] * annualReNormalizeFactors[annualIndex];

                        if (isReference)
                        {
                            maximaAbsoluteMean[hour] = annualMaximaAggregation[1, month, hourIndex] * annualPeakPowerPerSite; // kW
                            effectiveAbsoluteMean[hour] = annualEffectiveAggregation[1, month, hourIndex] * annualPeakPowerPerSite; // kW
                        }
                        else
                        {
                            if (annualMaximaAggregationMonth > 0)  // There is production data for the month
                            {
                                var maximaValue = annualMaximaAggregation[1, month, hourIndex] * annualPeakPowerPerSite; // kW
                                var effectiveValue = annualEffectiveAggregation[1, month, hourIndex] * annualPeakPowerPerSite; // kW

                                productionMaximaAbsoluteMeanArray[month, hour] += maximaValue;
                                productionEffectiveAbsoluteMeanArray[month, hour] += effectiveValue;

                                if (productionCountPerMonth[month] == 1)  // Initialize with first value
                                {
                                    productionMaximaAbsoluteMinArray[month, hour] = maximaValue;
                                    productionEffectiveAbsoluteMinArray[month, hour] = effectiveValue;
                                }
                                else
                                {
                                    productionMaximaAbsoluteMinArray[month, hour] = Math.Min(productionMaximaAbsoluteMinArray[month, hour], maximaValue);
                                    productionEffectiveAbsoluteMinArray[month, hour] = Math.Min(productionEffectiveAbsoluteMinArray[month, hour], effectiveValue);
                                }

                                productionMaximaAbsoluteMaxArray[month, hour] = Math.Max(productionMaximaAbsoluteMaxArray[month, hour], maximaValue);
                                productionEffectiveAbsoluteMaxArray[month, hour] = Math.Max(productionEffectiveAbsoluteMaxArray[month, hour], effectiveValue);
                            }
                        }
                    }

                    if (isReference)
                    {
                        if (reCalibrateReferenceModel)
                        {
                            effectiveRelativeMonth = effectiveRelativeMonth.Select(v => v * calibrationFactors[month]).ToArray();
                            effectiveAbsoluteMean = effectiveAbsoluteMean.Select(v => v * calibrationFactors[month]).ToArray();

                            referenceModelPowerPerMonth[month] *= calibrationFactors[month];
                        }
                        annualMaximaRelativeYearMean.Add(maximaRelativeMonth);
                        annualEffectiveRelativeYearMean.Add(effectiveRelativeMonth);

                        referenceMaximaAbsoluteMonthList.Add(maximaAbsoluteMean);
                        referenceEffectiveAbsoluteMonthList.Add(effectiveAbsoluteMean);

                        referenceMaxPower = Math.Max(referenceMaxPower, maximaAbsoluteMean.Max());
                    }
                    else
                    {
                        annualMaximaRelativeYearMean.Add(maximaRelativeMonth);
                        annualEffectiveRelativeYearMean.Add(effectiveRelativeMonth);
                    }
                }
                maximaRelativeYearMonthLists.Add(annualMaximaRelativeYearMean);
                effectiveRelativeYearMonthLists.Add(annualEffectiveRelativeYearMean);
            }
            for (var month = 1; month < dimMonth; month++)
            {
                var theoreticalMean = new double[dimHours];
                var theoreticalMin = new double[dimHours];
                var theoreticalMax = new double[dimHours];

                var effectiveMean = new double[dimHours];
                var effectiveMin = new double[dimHours];
                var effectiveMax = new double[dimHours];
                for (var hour = 0; hour < dimHours; hour++)
                {
                    theoreticalMean[hour] = productionMaximaAbsoluteMeanArray[month, hour] / productionCountPerMonth[month];
                    theoreticalMin[hour] = productionMaximaAbsoluteMinArray[month, hour];
                    theoreticalMax[hour] = productionMaximaAbsoluteMaxArray[month, hour];

                    effectiveMean[hour] = productionEffectiveAbsoluteMeanArray[month, hour] / productionCountPerMonth[month];
                    effectiveMin[hour] = productionEffectiveAbsoluteMinArray[month, hour];
                    effectiveMax[hour] = productionEffectiveAbsoluteMaxArray[month, hour];
                }
                productionMaximaAbsoluteMonthMeanList.Add(theoreticalMean);
                productionMaximaAbsoluteMonthMinList.Add(theoreticalMin);
                productionMaximaAbsoluteMonthMaxList.Add(theoreticalMax);

                productionEffectiveAbsoluteMonthMeanList.Add(effectiveMean);
                productionEffectiveAbsoluteMonthMinList.Add(effectiveMin);
                productionEffectiveAbsoluteMonthMaxList.Add(effectiveMax);
            }
            referenceModelPowerPerMonth[0] = referenceModelPowerPerMonth[1..].Sum();

            return ((dimCurves, dimMonth, dimHours), 
                (referenceMaxPower, overallPeakPower, referenceModelPowerPerMonth),
                (
                maximaRelativeYearMonthLists,
                referenceMaximaAbsoluteMonthList,
                productionMaximaAbsoluteMonthMeanList,
                productionMaximaAbsoluteMonthMinList,
                productionMaximaAbsoluteMonthMaxList
                ), 
                (
                effectiveRelativeYearMonthLists,
                referenceEffectiveAbsoluteMonthList,
                productionEffectiveAbsoluteMonthMeanList,
                productionEffectiveAbsoluteMonthMinList,
                productionEffectiveAbsoluteMonthMaxList
                )
                );
        }
    }
}
