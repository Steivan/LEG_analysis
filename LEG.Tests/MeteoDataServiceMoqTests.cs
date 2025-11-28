//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using MeteoData;
//using MeteoData.Models;

//namespace MeteoData.Tests
//{
//    [TestClass]
//    public class MeteoDataServiceMoqTests
//    {
//        [TestMethod]
//        public async Task GetHistoricalWeatherAsync_ReturnsParsedWeatherData()
//        {
//            // Arrange
//            var mockClient = new Mock<MeteoSwissClient>();
//            var csvRows = new[]
//            {
//                "station;time;tre200s0;pp0qnhs0",
//                "SMA;20230101T0000;5.2;1000",
//                "SMA;20230101T0010;5.3;1001"
//            };
//            mockClient.Setup(c => c.GetHistoricalDataAsync(
//                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double[]>(), It.IsAny<string>(), It.IsAny<string>()))
//                .ReturnsAsync(csvRows);

//            var service = new MeteoDataService();
//            var field = typeof(MeteoDataService).GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//            field.SetValue(service, mockClient.Object);

//            // Act
//            var result = await service.GetHistoricalWeatherAsync(
//                "2023-01-01T00:00:00Z", "2023-01-01T01:00:00Z", new double[] { 8.55, 47.36, 8.56, 47.37 }, "SMA");

//            // Assert
//            Assert.IsNotNull(result);
//            Assert.AreEqual(2, result.Count);
//            Assert.AreEqual(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), result[0].Timestamp);
//            Assert.AreEqual(5.2, result[0].Temperature2m, 0.01);
//            Assert.AreEqual(0, result[0].GlobalHorizontalRadiation, 0.01);
//            Assert.AreEqual(new DateTimeOffset(2023, 1, 1, 0, 10, 0, TimeSpan.Zero), result[1].Timestamp);
//            Assert.AreEqual(5.3, result[1].Temperature2m, 0.01);
//            Assert.AreEqual(0, result[1].GlobalHorizontalRadiation, 0.01);
//        }
//    }
//}