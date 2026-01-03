using CalibrationApp.Helpers;

namespace CalibrationApp.Generator
{
    public class ModelValidator
    {
        public record ValidationResult(double MeanResidual, double Rmse, double BiasPercentage, double MaxResidual);

        public ValidationResult ValidateDeterministicModel(
            Dictionary<DateTime, double> data,
            FourierSeries seasonalModel,
            FourierSeries diurnalModel,
            double[] weekdayFactors,
            DateTime reference)
        {
            double sumError = 0;
            double sumSqError = 0;
            double sumActual = 0;
            double maxResid = 0;
            int count = 0;

            foreach (var kvp in data)
            {
                DateTime dt = kvp.Key;
                double actual = kvp.Value;

                // 1. Compute the Prediction
                double s = seasonalModel.Evaluate((dt - reference).TotalDays, 365.2422);
                double d = diurnalModel.Evaluate(dt.TimeOfDay.TotalHours, 24.0);
                double w = weekdayFactors[(int)dt.DayOfWeek];

                double predicted = s * d * w;
                double residual = actual - predicted;

                // 2. Accumulate Stats
                sumError += residual;
                sumSqError += residual * residual;
                sumActual += actual;
                maxResid = Math.Max(maxResid, Math.Abs(residual));
                count++;
            }

            double meanResidual = sumError / count;
            double rmse = Math.Sqrt(sumSqError / count);
            double biasPct = (sumError / sumActual) * 100;

            return new ValidationResult(meanResidual, rmse, biasPct, maxResid);
        }
    }
}
