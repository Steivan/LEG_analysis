
namespace CalibrationApp.Helpers
{
    internal class FitDiurnal
    {
        public static (double[] A, double[] B) FitDiurnalFullyNormalized(
            Dictionary<DateTime, double> data,
            FourierSeries seasonalModel,
            double[] weekdayFactors,
            DateTime reference,
            int harmonics)
        {
            var slots = new List<double>[96];
            for (int i = 0; i < 96; i++) slots[i] = new List<double>();

            foreach (var kvp in data)
            {
                // 1. Get Seasonal Scale
                double daysSinceRef = (kvp.Key - reference).TotalDays;
                double seasonalScale = seasonalModel.Evaluate(daysSinceRef, 365.2422);

                // 2. Get Weekday Factor
                double weekdayFactor = weekdayFactors[(int)kvp.Key.DayOfWeek];

                // 3. Fully Normalize: Remove Season and Weekday influence
                // This leaves only the "Pure" diurnal shape
                double normalizedPower = kvp.Value / (seasonalScale * weekdayFactor);

                int slotIndex = (int)(kvp.Key.TimeOfDay.TotalMinutes / 15);
                slots[slotIndex].Add(normalizedPower);
            }

            double[] masterProfile = slots.Select(s => s.Average()).ToArray();
            return FourierAnalysis.ComputeCoefficients(masterProfile, harmonics);
        }
    }
}
