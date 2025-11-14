namespace LEG.CoreLib.SolarCalculations.Utilities
{
    public class BasicParametersAndConstants
    {
        public const int DefaultYear = 2025;
        public const int NrOfMonth = 12;

        public static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        public static readonly List<string> MonthList = ["na", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        public static readonly List<int> ReferenceDays = [1, 8, 15, 23];
        public static readonly int NrOfReferenceDays = NrOfMonth * ReferenceDays.Count;
        public static readonly int CurrentYear = DateTime.Now.Year;
        public static readonly int CalculationYear = CurrentYear;
        //public static readonly TimeOnly DefaultTimeSunRise = new TimeOnly(0, 1);
        //public static readonly TimeOnly DefaultTimeSunSet = new TimeOnly(23, 59);
    }
}
