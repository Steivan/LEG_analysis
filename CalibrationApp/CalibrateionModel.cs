using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace CalibrationApp
{
    internal class CalibrateionModel
    {
        internal static double[] GetTimeSlotCalibrationFactors(
            List<SolarProductionAggregateResults> annualProductionList,
            SolarProductionAggregateResults referenceProduction,
            int startHour = 0,
            int endHour = 24
            )
        {
            var calibrationFactors = (new double[13]).Select(v => 1.0).ToArray();

            var ((dimCurves, dimMonth, dimHours),
                (referenceMaxPower, overallPeakPower, referenceModelPowerPerMonth),

                (_, // maximaRelativeYearMonthLists,
                _, // referenceMaximaAbsoluteMonthList,
                _, // productionMaximaAbsoluteMonthMeanList,
                _, // productionMaximaAbsoluteMonthMinList,
                _), // productionMaximaAbsoluteMonthMaxList),

                (_, // effectiveRelativeYearMonthLists,
                referenceEffectiveAbsoluteMonthList,
                productionEffectiveAbsoluteMonthMeanList,
                _, // productionEffectiveAbsoluteMonthMinList,
                _) // productionEffectiveAbsoluteMonthMaxList)
                ) = DecomposeRecords.ExtractProfileLists(annualProductionList, referenceProduction, calibrationFactors, adjustReferenceModel: false);

            // Calibration factors per month
            startHour = Math.Max(0, Math.Min(23, startHour));
            endHour = Math.Max(1, Math.Min(24, endHour));

            // Reference and effective production for the year and hours
            double annualReferenceSum = 0.0;
            double annualProductionSum = 0.0;
            for (int month = 1; month < dimMonth; month++)
            {
                // Reference and effective production for the month and hours
                double monthlyReferenceSum = 0.0;
                double monthlyProductionSum = 0.0;
                for (int hour = startHour; hour < endHour; hour++)
                {
                    monthlyReferenceSum += referenceEffectiveAbsoluteMonthList[month-1][hour];
                    monthlyProductionSum += productionEffectiveAbsoluteMonthMeanList[month-1][hour];
                }
                annualReferenceSum += monthlyReferenceSum;
                annualProductionSum += monthlyProductionSum;

                // Calculate monthly calibration factor
                if (monthlyReferenceSum > 0.0)
                {
                    calibrationFactors[month] = monthlyProductionSum / monthlyReferenceSum;
                }
                else
                {
                    calibrationFactors[month] = 1.0;
                }
            }
            // Calculate aggregate calibration factor
            if (annualReferenceSum > 0.0)
            {
                calibrationFactors[0] = annualProductionSum / annualReferenceSum;
            }
            else
            {
                calibrationFactors[0] = 1.0;
            }

            return calibrationFactors;
        }
    }
}
