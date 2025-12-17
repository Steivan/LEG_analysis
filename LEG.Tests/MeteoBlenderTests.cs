using LEG.PV.Data.Processor.Helpers;
using LEG.PV.Data.Processor.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.Tests
{
    [TestClass]
    public class MeteoBlenderTests
    {
        [TestMethod]
        public async Task TestBlendingSampleForecastSeries()
        {
            // Arrange
            var nearCastInterval = TimeSpan.FromMinutes(10);
            var forecastCastInterval = TimeSpan.FromMinutes(60);
            var nearCastCount = 10;
            var midCastCount = 10;
            var farCastCount = 10;
            var nowYear = DateTime.UtcNow.Year;
            var sampleNow = new DateTime(nowYear, 5, 10, 10, 15, 0, DateTimeKind.Utc);
            var startTimeNearCast = new DateTime(nowYear, 5, 10, 9, 45, 0, DateTimeKind.Utc);
            var startTimeMidCast = new DateTime(nowYear, 5, 10, 12, 0, 0, DateTimeKind.Utc);
            var startTimeFarCast = new DateTime(nowYear, 5, 10, 10, 0, 0, DateTimeKind.Utc);
            var nearCastSample = MeteoSampleRecords.GetMeteoSamples(startTimeNearCast, nearCastInterval, nearCastCount);
            var midCastSample = MeteoSampleRecords.GetMeteoSamples(startTimeMidCast, forecastCastInterval, midCastCount);
            var farCastSample = MeteoSampleRecords.GetMeteoSamples(startTimeFarCast, forecastCastInterval, farCastCount);

            // Act
            var blender = new MeteoForecastSeriesBlender();
            var foreCastSampleList1 = await blender.CreateBlendedForecastListFromLists(
                sampleNow,
                farCastSeries: MeteoSeriesConverter.MeteoDictToList(farCastSample),
                midCastSeries: MeteoSeriesConverter.MeteoDictToList(midCastSample),
                nearCastSeries: MeteoSeriesConverter.MeteoDictToList(nearCastSample),
                smoothingFilterId: 0
            );
            var foreCastSampleDict1 = MeteoSeriesConverter.MeteoListToDict(foreCastSampleList1);

            var foreCastSampleDict2 = await blender.CreateBlendedForecastDictFromDicts(
                sampleNow,
                farCastSeries: farCastSample,
                midCastSeries: midCastSample,
                nearCastSeries: nearCastSample,
                smoothingFilterId: 0
            );
            var foreCastSampleList2 = MeteoSeriesConverter.MeteoDictToList(foreCastSampleDict2);


            //var result = await _meteoDataService!.GetHistoricalWeatherAsync(startDate, endDate, stationId, granularity);

            // Assert
            for (var i=0; i< foreCastSampleList1.Count; i++)
            {
                var data1 = foreCastSampleList1[i];
                var data2 = foreCastSampleList2[i];
                var timestamp = foreCastSampleList1[i].Time;
                Assert.IsTrue(AreAlmostEqual(data1, data2), $"Data mismatch index {i}");

                data1 = foreCastSampleDict1[timestamp];
                data2 = foreCastSampleDict2[timestamp];
                Assert.IsTrue(AreAlmostEqual(data1, data2), $"Data mismatch at {timestamp:u}");
            }
        }

        public static bool AreAlmostEqual(MeteoParameters a, MeteoParameters b, double tolerance = 1e-6)
        {
            bool NotAlmostEqual(double? x, double? y, double tol)
            {
                if ((x == null) != (y == null)) return true;
                if ((x == null) || (y == null)) return false;
                return Math.Abs(x.Value - y.Value) > tol;
            }
            if (a == null || b == null) return false;
            // Compare non-nullable properties
            if (!a.Time.Equals(b.Time)) return false;
            if (!a.Interval.Equals(b.Interval)) return false;
            if (!a.Anchor.Equals(b.Anchor)) return false;
            // Compare all relevant properties, e.g.:
            if (NotAlmostEqual(a.SunshineDuration, b.SunshineDuration, tolerance)) return false;
            if (NotAlmostEqual(a.DirectRadiation, b.DirectRadiation, tolerance)) return false;
            if (NotAlmostEqual(a.DirectNormalIrradiance, b.DirectNormalIrradiance, tolerance)) return false;
            if (NotAlmostEqual(a.GlobalRadiation, b.GlobalRadiation, tolerance)) return false;
            if (NotAlmostEqual(a.DiffuseRadiation, b.DiffuseRadiation, tolerance)) return false;
            if (NotAlmostEqual(a.Temperature, b.Temperature, tolerance)) return false;
            if (NotAlmostEqual(a.WindSpeed, b.WindSpeed, tolerance)) return false;
            if (NotAlmostEqual(a.WindDirection, b.WindDirection, tolerance)) return false;
            if (NotAlmostEqual(a.SnowDepth, b.SnowDepth, tolerance)) return false;
            if (NotAlmostEqual(a.RelativeHumidity, b.RelativeHumidity, tolerance)) return false;
            if (NotAlmostEqual(a.DewPoint, b.DewPoint, tolerance)) return false;
            if (NotAlmostEqual(a.RadiationVariance, b.RadiationVariance, tolerance)) return false;

            return true;
        }
    }
}
