using LEG.Common;
using LEG.MeteoSwiss.Abstractions.Models;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using Microsoft.VisualStudio.TestTools.UnitTesting;


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
                "station_abbr;reference_timestamp;tre200s0;ure200s0;tde200s0;prestas0;gre000z0;sre000z0;ods000z0;fu3010z0;dkl010z0;htoauts0;ure200s0\r\n" +
                "SMA;01.01.2025 00:00;-2.8;99.1;-2.9;963.1;0;0;99;21.5;350;0.15;60\r\n" +
                "SMA;01.01.2025 00:10;-2.9;99.1;-3;963.1;0;0;9;12.0;360;0.0;45\r\n";
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

                // Assert conversion to MeteoParameters record
                var meteoParameters = result[0].ToMeteoParameters();
                Assert.AreEqual(result[0].ReferenceTimestamp, meteoParameters.Time);
                Assert.AreEqual(10, meteoParameters.Interval.Minutes);
                Assert.AreEqual(result[0].SunshineDuration.GetValueOrDefault(), meteoParameters.SunshineDuration.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].DirectRadiation.GetValueOrDefault(), meteoParameters.DirectRadiation.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].DirectNormalIrradiance.GetValueOrDefault(), meteoParameters.DirectNormalIrradiance.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].ShortWaveRadiation.GetValueOrDefault(), meteoParameters.GlobalRadiation.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].DiffuseRadiation.GetValueOrDefault(), meteoParameters.DiffuseRadiation.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].Temperature2m.GetValueOrDefault(), meteoParameters.Temperature.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].WindSpeed10min_kmh.GetValueOrDefault(), meteoParameters.WindSpeed.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].WindDirection.GetValueOrDefault(), meteoParameters.WindDirection.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].SnowDepth.GetValueOrDefault(), meteoParameters.SnowDepth.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].RelativeHumidity2m.GetValueOrDefault(), meteoParameters.RelativeHumidity.GetValueOrDefault(), 0.01);
                Assert.AreEqual(result[0].DewPoint2m.GetValueOrDefault(), meteoParameters.DewPoint.GetValueOrDefault(), 0.01);

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