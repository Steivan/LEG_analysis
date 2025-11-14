using LEG.MeteoSwiss.Abstractions;
using LEG.MeteoSwiss.Client.MeteoSwiss;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LEG.Tests
{
    [TestClass]
    public class MeteoDataServiceTests
    {
        private MeteoDataService? _meteoDataService;

        [TestInitialize]
        public void TestInitialize()
        {
            // ** REFACTORED: Use the newly renamed MeteoSwissClient. **
            IMeteoSwissClient apiClient = new MeteoSwissClient();
            _meteoDataService = new MeteoDataService(apiClient);
        }

        [TestMethod]
        public async Task GetHistoricalWeatherAsync_KnownGood_ReturnsData()
        {
            // Arrange
            var startDate = "2024-05-01T00:00:00Z";
            var endDate = "2024-05-02T00:00:00Z";
            var stationId = "SCU";
            var granularity = "t"; // "t" for 10-min data

            // Act
            var result = await _meteoDataService!.GetHistoricalWeatherAsync(startDate, endDate, stationId, granularity);

            // Assert
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsTrue(result.Count != 0, "Expected at least one data point for the specified station and date range.");

            var first = result.First();
            Assert.IsNotNull(first.Timestamp, "Timestamp should not be null.");
        }

        [TestMethod]
        public async Task GetOpenMeteoHistoricalAsync_ReturnsData()
        {
            // Arrange
            var latitude = 47.38; // SMA coordinates
            var longitude = 8.54;
            var startDate = "2023-06-01";
            var endDate = "2023-06-02";

            // Act
            var result = await _meteoDataService!.GetOpenMeteoHistoricalAsync(latitude, longitude, startDate, endDate);

            // Assert
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsTrue(result.Count != 0, $"Expected at least one data point, got {result.Count}.");

            var first = result.First();
            Assert.IsNotNull(first.Timestamp, "Timestamp should not be null.");
            Assert.IsTrue(first.temperature_2m >= -50 && first.temperature_2m <= 50, $"Temperature ({first.temperature_2m}) should be within reasonable range.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetHistoricalWeatherAsync_InvalidStationId_Throws()
        {
            // Arrange
            var startDate = "2023-06-01T00:00:00Z";
            var endDate = "2023-06-02T00:00:00Z";
            string? stationId = null;

            // Act
            await _meteoDataService!.GetHistoricalWeatherAsync(startDate, endDate, stationId!);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _meteoDataService?.Dispose();
        }
    }
}