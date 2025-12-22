
namespace LEG.PvImport.Abstractions.Fronius.Abstractions
{
    public class FroniusConstants
    {
        public const string DataFolder = @"C:\Users\steiv\OneDrive\Dokumente\Excel\Haus Studenrain\PV-Anlage - Fronius\";
        public const string FileBody = "Bernegger";
        public const string FileExtension = ".xls";
        public const int FirstYear = 2011;
        public const int LastYear = 2015;
        public const string PowerTab = "Leistung";
        public const string DcTab = "DC Spannung";
        public const string AcMeanTab = "AC Spannung Mean";
        public const string AcL1Tab = "AC Spannung L1";
        public const string AcL2Tab = "AC Spannung L2";
        public const string AcL3Tab = "AC Spannung L3";
        public const int HeaderRow = 6;
        public const int MinutesPerPeriod = 10;
    }
}
