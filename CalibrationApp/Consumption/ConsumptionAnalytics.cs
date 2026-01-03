
namespace CalibrationApp.Consumption
{
    public class ConsumptionAnalytics
    {
        public record ProfileKey(int Period, DayOfWeek DayOfWeek, TimeSpan TimeOfDay);

        // Calculates the 1-13 period based on the first Monday of the year
        public static int Get13x4Period(DateTime dt)
        {
            DateTime firstDayOfYear = new DateTime(dt.Year, 1, 1);
            // Find first Monday: Sun=0, Mon=1...
            int daysToFirstMonday = (8 - (int)firstDayOfYear.DayOfWeek) % 7;
            DateTime firstMonday = firstDayOfYear.AddDays(daysToFirstMonday);

            int daysSince = (dt.Date - firstMonday).Days;
            if (daysSince < 0) return 0; // Days before the first full week of the year

            int period = (daysSince / 28) + 1;
            return Math.Min(period, 13);
        }
    }

}

