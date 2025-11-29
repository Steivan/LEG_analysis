using LEG.MeteoSwiss.Abstractions.Models;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LEG.Tests
{
    [TestClass]
    public class MeteoAggregatorTests
    {
        [TestMethod]
        public void SafeVectorAverageWindDirection_SingleRecord_ReturnsCorrectDirection()
        {
            // Arrange
            var records = new List<WeatherCsvRecord>
            {
                new() { WindSpeedVectorial10min = 10, WindDirection = 90 }
            };

            // Act
            var result = MeteoAggregator.SafeVectorAverageWindDirection(records);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(90, result.Value, 1e-9);
        }

        [TestMethod]
        public void SafeVectorAverageWindDirection_TwoOpposingWinds_ReturnsNull()
        {
            // Arrange
            var records = new List<WeatherCsvRecord>
            {
                new() { WindSpeedVectorial10min = 10, WindDirection = 90 },
                new() { WindSpeedVectorial10min = 10, WindDirection = 270 }
            };

            // Act
            var result = MeteoAggregator.SafeVectorAverageWindDirection(records);

            // Assert
            Assert.IsNull(result, "Opposing winds of equal speed should result in a null/undefined direction.");
        }

        [TestMethod]
        public void SafeVectorAverageWindDirection_TwoPerpendicularWinds_ReturnsCorrectAverage()
        {
            // Arrange
            var records = new List<WeatherCsvRecord>
            {
                new() { WindSpeedVectorial10min = 10, WindDirection = 0 },  // From North
                new() { WindSpeedVectorial10min = 10, WindDirection = 90 }  // From East
            };

            // Act
            var result = MeteoAggregator.SafeVectorAverageWindDirection(records);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(45, result.Value, 1e-9, "Average should be North-East");
        }

        [TestMethod]
        public void SafeVectorAverageWindDirection_Handles360DegreeCrossover()
        {
            // Arrange
            var records = new List<WeatherCsvRecord>
            {
                new() { WindSpeedVectorial10min = 10, WindDirection = 350 },
                new() { WindSpeedVectorial10min = 10, WindDirection = 10 }
            };

            // Act
            var result = MeteoAggregator.SafeVectorAverageWindDirection(records);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Value, 1e-9);
        }

        [TestMethod]
        public void SafeVectorAverageWindDirection_NoValidRecords_ReturnsNull()
        {
            // Arrange
            var records = new List<WeatherCsvRecord>
            {
                new() { WindSpeedVectorial10min = 10, WindDirection = null },
                new() { WindSpeedVectorial10min = null, WindDirection = 90 }
            };

            // Act
            var result = MeteoAggregator.SafeVectorAverageWindDirection(records);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SafeVectorAverageWindDirection_CalmWinds_ReturnsNull()
        {
            // Arrange
            var records = new List<WeatherCsvRecord>
            {
                new() { WindSpeedVectorial10min = 0, WindDirection = 90 },
                new() { WindSpeedVectorial10min = 0, WindDirection = 270 }
            };

            // Act
            var result = MeteoAggregator.SafeVectorAverageWindDirection(records);

            // Assert
            Assert.IsNull(result, "Calm winds should result in a null/undefined direction.");
        }
    }
}