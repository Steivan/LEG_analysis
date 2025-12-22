using LEG.PvImport.Abstractions.Fronius.Abstractions;

namespace LEG.PvImport.Clients.Fronius.Client
{
    public class FroniusFileHelper
    {
        public static string FileBody => FroniusConstants.FileBody;
        public static string FileExtension => FroniusConstants.FileExtension;
        public static string FileName(int year) => $"{FileBody}_{year}{FileExtension}";
        public static string GetFolder => FroniusConstants.DataFolder;
        public static string GetFilePath(int year) => GetFolder + FileName(year);
        public static string GetPowerTab => FroniusConstants.PowerTab;
        public static (int firstYear, int lastYear) GetYears => (FroniusConstants.FirstYear, FroniusConstants.LastYear);
        public static TimeSpan Interval => TimeSpan.FromMinutes(FroniusConstants.MinutesPerPeriod);
    }
}
