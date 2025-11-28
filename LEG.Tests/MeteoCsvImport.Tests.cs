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
                "station_abbr;reference_timestamp;tre200s0;ure200s0;tde200s0;prestas0;gre000z0;sre000z0\r\n" +
                "SMA;01.01.2025 00:00;-2.8;99.1;-2.9;963.1;0;0\r\n" +
                "SMA;01.01.2025 00:10;-2.9;99.1;-3;963.1;0;0\r\n";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, csvContent);

            try
            {
                // Act
                var result = ImportCsv.ImportFromFile<WeatherCsvRecord>(tempFile, ";");

                // Assert
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("SMA", result[0].StationAbbr);
                Assert.AreEqual(new DateTime(2025, 1, 1, 0, 0, 0), result[0].ReferenceTimestamp);
                Assert.AreEqual(-2.8, result[0].Temperature2m.GetValueOrDefault(), 0.01);
                Assert.AreEqual(99.1, result[0].RelativeHumidity2m.GetValueOrDefault(), 0.01);
                Assert.AreEqual(-2.9, result[0].DewPoint2m.GetValueOrDefault(), 0.01);
                Assert.AreEqual(963.1, result[0].PressureAtStation.GetValueOrDefault(), 0.01);
                Assert.AreEqual(0, result[0].ShortWaveRadiation.GetValueOrDefault(), 0.01);
                Assert.AreEqual(0, result[0].SunshineDuration.GetValueOrDefault(), 0.01);
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
            // Assert.IsTrue(first.Temperature2m > -50 && first.Temperature2m < 60, "Temperature2m (temperature) out of plausible range.");
        }
    }
}