namespace LEG.CoreLib.SolarCalculations.Calculations
{
    public class TimeConversionHelper
    {
        private const int JulianYear0 = 2000;
        private const int JulianMonth0 = 1;
        private const int JulianDay0 = 1;
        private const int JulianHour0 = 12;
        private const int JulianMinute0 = 0;
        private const int JulianSecond0 = 0;
        private const int JulianDateTime0Base = 2451545;
        private static readonly int ExcelDayNumber0 = new DateOnly(1899, 12, 30).DayNumber;

        public static int DateSerial(int y, int m, int d) =>
            new DateOnly(y, m, d).DayNumber - ExcelDayNumber0;

        public static double TimeSerial(int hh, int mm, int ss) =>
            (((double)ss / 60 + mm) / 60 + hh) / 24;

        public static double DateTimeSerial(int y, int m, int d, int hh, int mm, int ss) =>
            DateSerial(y, m, d) + TimeSerial(hh, mm, ss);

        public static double DateTimeUtc(int y, int m, int d, int hh, int mm, int ss, int utcShift) =>
            DateTimeSerial(y, m, d, hh, mm, ss) + utcShift / 24.0;

        public static double DateTimeUtc0() =>
            DateTimeUtc(JulianYear0, JulianMonth0, JulianDay0, JulianHour0, JulianMinute0, JulianSecond0, 0);

        public static int DateUtc(int y, int m, int d, int hh, int mm, int ss, int utcShift) =>
            (int)DateTimeUtc(y, m, d, hh, mm, ss, utcShift);

        public static double TimeUtc(int y, int m, int d, int hh, int mm, int ss, int utcShift) =>
            DateTimeUtc(y, m, d, hh, mm, ss, utcShift) - DateUtc(y, m, d, hh, mm, ss, utcShift);

        public static double TimeUtcSince0(int y, int m, int d, int hh, int mm, int ss, int utcShift) =>
            DateTimeUtc(y, m, d, hh, mm, ss, utcShift) - DateTimeUtc0();

        public static double JulianTimeUtc(int y, int m, int d, int hh, int mm, int ss, int utcShift) =>
            JulianDateTime0Base + TimeUtcSince0(y, m, d, hh, mm, ss, utcShift);
    }
}