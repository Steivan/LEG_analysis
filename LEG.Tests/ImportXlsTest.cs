using LEG.PvImport.Abstractions.Fronius.Abstractions;
using LEG.PvImport.Clients.Fronius.Client;

namespace LEG.Tests
{
    [TestClass]
    public class ImportXlsTest
    {
        [TestMethod]
        public void CheckFroniusFiles()
        {
            var yearExtension = "_2011";
            var powerName = FroniusConstants.PowerTab;
            var dcName = FroniusConstants.DcTab;
            var acMeanName = FroniusConstants.AcMeanTab;
            var acL1Name = FroniusConstants.AcL1Tab;
            var acL2Name = FroniusConstants.AcL2Tab;
            var acL3Name = FroniusConstants.AcL3Tab;

            var headerRow = FroniusConstants.HeaderRow;

            for (var year = FroniusConstants.FirstYear; year <= FroniusConstants.LastYear; year++)
            {
                yearExtension = $"_{ year}";
                var filePath = FroniusConstants.DataFolder + FroniusConstants.FileBody + yearExtension + FroniusConstants.FileExtension;

                var froniusPowerRecords = FromiusLoadRecords.ImportFroniusRecords(filePath, powerName, headerRow);
                var froniusDcRecords = FromiusLoadRecords.ImportFroniusRecords(filePath, dcName, headerRow);
                var froniusAcMeanRecords = FromiusLoadRecords.ImportFroniusRecords(filePath, acMeanName, headerRow);
                var froniusAcL1Records = FromiusLoadRecords.ImportFroniusRecords(filePath, acL1Name, headerRow);
                var froniusAcL2Records = FromiusLoadRecords.ImportFroniusRecords(filePath, acL2Name, headerRow);
                var froniusAcL3Records = FromiusLoadRecords.ImportFroniusRecords(filePath, acL3Name, headerRow);
            }
        }
    }
}

