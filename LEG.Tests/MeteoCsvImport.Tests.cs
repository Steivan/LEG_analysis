using Microsoft.VisualStudio.TestTools.UnitTesting;
using LEG.Common;
using LEG.MeteoSwiss.Abstractions;
using LEG.MeteoSwiss.Client.MeteoSwiss;


namespace LEG.Tests
{
    [TestClass]
    public class MeteoCsvImportTests
    {
        [TestMethod]
        public void ImportFromFile_ValidMeteoCsv_ReturnsCorrectData()
        {
            // Arrange
            var csvContent =
                "station_abbr;reference_timestamp;ta1tows0;tdetows0;uretows0;fkltowz1;fk1towz0;dv1towz0;fu3towz0;fu3towz1;gre000z0;sre000z0\r\n" +
                "BAN;01.01.2020 00:00;5.7;-6.9;39.8;6.3;4.8;82;16.9;22.7;4;0\r\n" +
                "BAN;01.01.2020 00:10;5.8;-6.9;39.4;4.7;3.5;103;12.2;16.9;3;0\r\n";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, csvContent);

            try
            {
                // Act
                var result = ImportCsv.ImportFromFile<WeatherCsvRecord>(tempFile, ";");

                // Assert
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("BAN", result[0].StationAbbr);
                Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0), result[0].ReferenceTimestamp);
                Assert.AreEqual(5.7, result[0].Ta1Tows0.GetValueOrDefault(), 0.01);
                Assert.AreEqual(-6.9, result[0].TdeTows0.GetValueOrDefault(), 0.01);
                Assert.AreEqual(39.8, result[0].UreTows0.GetValueOrDefault(), 0.01);
                Assert.AreEqual(4, result[0].Gre000z0.GetValueOrDefault(), 0.01);
                Assert.AreEqual(0, result[0].Sre000z0.GetValueOrDefault(), 0.01);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void ImportFromFile_RealMeteoCsv_ReadsData()
        {
            var filePath = MeteoSwissConstants.OgdSmnTowerSamplePath + MeteoSwissConstants.OgdSmnTowerSampleFile;

            // Act
            var records = ImportCsv.ImportFromFile<WeatherCsvRecord>(filePath, ";");

            // Assert
            Assert.IsTrue(records.Count > 0, "No records were imported from the Meteo CSV file.");
            // Optionally, check a few fields of the first record for plausibility
            var first = records.First();
            Assert.IsFalse(string.IsNullOrWhiteSpace(first.StationAbbr), "StationAbbr should not be empty.");
            // ReferenceTimestamp is DateTime, so check for default value
            Assert.AreNotEqual(default, first.ReferenceTimestamp, "ReferenceTimestamp should not be default value.");
            // Optionally, add more asserts for plausibility
            // Assert.IsTrue(first.Ta1Tows0 > -50 && first.Ta1Tows0 < 60, "Ta1Tows0 (temperature) out of plausible range.");
        }
    }
}