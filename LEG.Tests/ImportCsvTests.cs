using LEG.CoreLib;
using LEG.E3Dc.Abstractions;
using LEG.E3Dc.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using LEG.Common;

namespace LEG.Tests
{
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [TestClass]
    public class ImportCsvTests
    {
        public static void CheckE3DCFile(string folderName, int year, int month)
        {
            var fileTail = E3DcFileHelper.FileTail(year, month);
            var fileName = E3DcFileHelper.FileName(year, month);
            var dataFile = folderName + fileName;
            if (!File.Exists(dataFile))
            {
                Console.WriteLine($"ERROR: {fileName} not found");
                return;
            }

            var records = ImportCsv.ImportFromFile<E3DcRecord>(dataFile, ";");
            var actual = records.Count;
            var expected = E3DcFileHelper.GetDaysInMonth(year, month) * 96;
            if (actual != expected)
                Console.WriteLine($"-> 20{fileTail} is incomplete: {actual} / {expected} records found in {fileName}");

            double batterySocSum = 0, batteryChargingSum = 0, batteryDischargingSum = 0;
            double houseConsumptionSum = 0, netInSum = 0, netOutSum = 0;
            double solarProductionTracker1Sum = 0,
                solarProductionTracker2Sum = 0,
                solarProductionTracker3Sum = 0,
                solarProductionSum = 0;
            double wallBoxId1TotalChargingPowerSum = 0,
                wallBoxId1GridReferenceSum = 0,
                wallBoxId1SolarChargingPowerSum = 0;
            double wallBoxId0TotalChargingPowerSum = 0,
                wallBoxId0GridReferenceSum = 0,
                wallBoxId0SolarChargingPowerSum = 0;
            double wallBoxTotalChargingPowerSum = 0, sigmaConsumptionSum = 0;

            foreach (var r in records)
            {
                batterySocSum += r.BatterySoc;
                batteryChargingSum += r.BatteryCharging;
                batteryDischargingSum += r.BatteryDischarging;
                houseConsumptionSum += r.HouseConsumption;
                netInSum += r.NetIn;
                netOutSum += r.NetOut;
                solarProductionTracker1Sum += r.SolarProductionTracker1;
                solarProductionTracker2Sum += r.SolarProductionTracker2;
                solarProductionTracker3Sum += r.SolarProductionTracker3;
                solarProductionSum += r.SolarProduction;
                wallBoxId1TotalChargingPowerSum += r.WallBoxId1TotalChargingPower;
                wallBoxId1GridReferenceSum += r.WallBoxId1GridReference;
                wallBoxId1SolarChargingPowerSum += r.WallBoxId1SolarChargingPower;
                wallBoxId0TotalChargingPowerSum += r.WallBoxId0TotalChargingPower;
                wallBoxId0GridReferenceSum += r.WallBoxId0GridReference;
                wallBoxId0SolarChargingPowerSum += r.WallBoxId0SolarChargingPower;
                wallBoxTotalChargingPowerSum += r.WallBoxTotalChargingPower;
                sigmaConsumptionSum += r.SigmaConsumption;
            }

            int countOfRecords = records.Count;
            var batterySoc = batterySocSum / countOfRecords;
            var batteryCharging = batteryChargingSum / countOfRecords;
            var batteryDischarging = batteryDischargingSum / countOfRecords;
            var houseConsumption = houseConsumptionSum / countOfRecords;
            var netIn = netInSum / countOfRecords;
            var netOut = netOutSum / countOfRecords;
            var solarProductionTracker1 = solarProductionTracker1Sum / countOfRecords;
            var solarProductionTracker2 = solarProductionTracker2Sum / countOfRecords;
            var solarProductionTracker3 = solarProductionTracker3Sum / countOfRecords;
            var solarProduction = solarProductionSum / countOfRecords;
            var wallBoxId1TotalChargingPower = wallBoxId1TotalChargingPowerSum / countOfRecords;
            var wallBoxId1GridReference = wallBoxId1GridReferenceSum / countOfRecords;
            var wallBoxId1SolarChargingPower = wallBoxId1SolarChargingPowerSum / countOfRecords;
            var wallBoxId0TotalChargingPower = wallBoxId0TotalChargingPowerSum / countOfRecords;
            var wallBoxId0GridReference = wallBoxId0GridReferenceSum / countOfRecords;
            var wallBoxId0SolarChargingPower = wallBoxId0SolarChargingPowerSum / countOfRecords;
            var wallBoxTotalChargingPower = wallBoxTotalChargingPowerSum / countOfRecords;
            var sigmaConsumption = sigmaConsumptionSum / countOfRecords;

            var hasWallBox = (
                wallBoxId1TotalChargingPower != 0 ||
                wallBoxId1GridReference != 0 ||
                wallBoxId1SolarChargingPower != 0 ||

                wallBoxId0TotalChargingPower != 0 ||
                wallBoxId0GridReference != 0 ||
                wallBoxId0SolarChargingPower != 0 ||

                wallBoxTotalChargingPower != 0 ||
                sigmaConsumption != 0
            );

            if (hasWallBox)
            {
                var residual1 = wallBoxId1TotalChargingPower - (wallBoxId1GridReference + wallBoxId1SolarChargingPower);
                var residual0 = wallBoxId0TotalChargingPower - (wallBoxId0GridReference + wallBoxId0SolarChargingPower);
                var residual = wallBoxTotalChargingPower -
                               (wallBoxId1TotalChargingPower + wallBoxId0TotalChargingPower);
            }

            var conversionFactor =
                (double)records.Count /
                1000; // average production [Wh] to aggregate energy [kWh] (production accumulated during 15 min intervals)
            var production = solarProduction * conversionFactor;
            var consumption = houseConsumption * conversionFactor;
            var batteryBalance = records[^1].BatterySoc - (records[0].BatterySoc + (batteryCharging - batteryDischarging) * conversionFactor);
            var flowBalance =
                (houseConsumption + wallBoxTotalChargingPower - solarProduction - batteryDischarging + batteryCharging -
                    netOut + netIn) * conversionFactor;
            var solarBalance =
                (solarProduction - solarProductionTracker1 - solarProductionTracker2 - solarProductionTracker3) *
                conversionFactor;

            Console.Write($"20{fileTail} {hasWallBox,5}:");

            Console.WriteLine(
                $"Production = {production,8:N1} [kWh/Mt], Consumption = {consumption,8:N1} [kWh/Mt], Battery balance = {batteryBalance,8:N1} [kWh/Mt], Flow balance = {flowBalance,8:N1} [kWh/Mt], Solar balance = {solarBalance,8:N1} [kWh/Mt]");

            var timeStamps = records.Select(r => E3DcFileHelper.ParseTimestamp(r.Timestamp)).ToList();
            for (var i = 1; i < timeStamps.Count; i++)
            {
                if (timeStamps[i] - timeStamps[i - 1] != TimeSpan.FromMinutes(15))
                {
                    Console.WriteLine(
                        $"Gap at index {i}: {timeStamps[i - 1]} -> {timeStamps[i]} ({(timeStamps[i] - timeStamps[i - 1]).TotalMinutes} min)");
                }
            }
        }

        [TestMethod]
        public void CheckE3DCFiles()
        {
            for (var folderNumber = 1; folderNumber <= E3DcFileHelper.NrOfFolders; folderNumber++)
            {
                var (dataFolder, subFolder) = E3DcFileHelper.GetFolder(folderNumber);
                var folder = dataFolder + subFolder;
                Console.WriteLine(subFolder);

                var (firstYear, lastYear) = E3DcFileHelper.GetYears(folderNumber);
                for (var year = firstYear; year <= lastYear; year++)
                {
                    var (firstMonth, lastMonth) = E3DcFileHelper.GetMonthsRange(folderNumber, year);
                    for (var month = firstMonth; month <= lastMonth; month++)
                    {
                        CheckE3DCFile(folder, year, month);
                    }
                }
            }
        }

        [TestMethod]
        public void ImportFromFile_ValidCsv_ReturnsCorrectData()
        {
            // Arrange
            var csvContent = "Name,Age\r\nAlice,30\r\nBob,25";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, csvContent);

            try
            {
                // Act
                var result = ImportCsv.ImportFromFile<Person>(tempFile);

                // Assert
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual("Alice", result[0].Name);
                Assert.AreEqual(30, result[0].Age);
                Assert.AreEqual("Bob", result[1].Name);
                Assert.AreEqual(25, result[1].Age);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}