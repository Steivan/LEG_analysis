using LEG.MeteoSwiss.Abstractions.Models;
using LEG.PV.Data.Processor.Helpers;
using LEG.PV.Data.Processor.Interfaces;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.Tests
{
    [TestClass]
    public class MeteoBlenderTests
    {
        [TestMethod]
        public async Task TestIntervalConverter()
        {
            // Arrange
            const int sampleMinutes = 60;
            const int targetMinutes = 12;
            const int subPeriodsCount = sampleMinutes / targetMinutes;

            var sampleInterval = TimeSpan.FromMinutes(sampleMinutes);
            var targetInterval = TimeSpan.FromMinutes(12);
            var sampleCount = 20;              // 20 hours 
            var nowYear = DateTime.UtcNow.Year;
            var sampleStartTime = new DateTime(nowYear, 5, 10, 12, 0, 0, DateTimeKind.Utc);
            var deterministicSampleSeries = MeteoDeterministicSeriesGenerator.GetMeteoSampleDictionary(sampleStartTime, sampleInterval, sampleCount);
            var randomSampleSeries = MeteoRandomSeriesGenerator.GetMeteoSampleDictionary(sampleStartTime, sampleInterval, sampleCount);

            TestIntervalConverterSeries(deterministicSampleSeries, sampleInterval, targetInterval, subPeriodsCount);
            TestIntervalConverterSeries(randomSampleSeries, sampleInterval, targetInterval, subPeriodsCount);
        }

        
        [TestMethod]
        public async Task TestBlendingSampleForecastSeries()
        {
            // Arrange
            var nearCastInterval = TimeSpan.FromMinutes(15);
            var forecastCastInterval = TimeSpan.FromMinutes(60);
            var nearCastCount = 10;             // 10 quarter hours
            var midCastCount = 10;              // 10 hours
            var farCastCount = 20;              // 20 hours 
            var nowYear = DateTime.UtcNow.Year;
            var sampleNow = new DateTime(nowYear, 5, 10, 10, 15, 0, DateTimeKind.Utc);
            var startTimeNearCast = new DateTime(nowYear, 5, 10, 9, 45, 0, DateTimeKind.Utc);
            var startTimeMidCast = new DateTime(nowYear, 5, 10, 12, 0, 0, DateTimeKind.Utc);
            var startTimeFarCast = new DateTime(nowYear, 5, 10, 10, 0, 0, DateTimeKind.Utc);
            var nearCastSample = MeteoDeterministicSeriesGenerator.GetMeteoSampleDictionary(startTimeNearCast, nearCastInterval, nearCastCount);
            var midCastSample = MeteoDeterministicSeriesGenerator.GetMeteoSampleDictionary(startTimeMidCast, forecastCastInterval, midCastCount);
            var farCastSample = MeteoDeterministicSeriesGenerator.GetMeteoSampleDictionary(startTimeFarCast, forecastCastInterval, farCastCount);

            // Act
            var blender = new MeteoForecastSeriesBlender();
            var foreCastSampleList1 = await blender.CreateBlendedForecastListFromLists(
                sampleNow,
                farCastSeriesList: MeteoSeriesConverter.MeteoDictToList(farCastSample),
                midCastSeriesList: MeteoSeriesConverter.MeteoDictToList(midCastSample),
                nearCastSeriesList: MeteoSeriesConverter.MeteoDictToList(nearCastSample),
                smoothingFilterId: 0
            );
            var foreCastSampleDict1 = MeteoSeriesConverter.MeteoListToDict(foreCastSampleList1);

            var foreCastSampleDict2 = await blender.CreateBlendedForecastDictFromDicts(
                sampleNow,
                farCastSeriesDict: farCastSample,
                midCastSeriesDict: midCastSample,
                nearCastSeriesDict: nearCastSample,
                smoothingFilterId: 0
            );
            var foreCastSampleList2 = MeteoSeriesConverter.MeteoDictToList(foreCastSampleDict2);


            // Assert
            // Assert that nearCast is preserved
            var nearCastList = MeteoSeriesConverter.MeteoDictToList(nearCastSample);
            for (var i = 0; i < nearCastCount; i++)
            {
                var data0 = nearCastList[i];
                var data1 = foreCastSampleList1[i];
                Assert.IsTrue(AreAlmostEqual(data0, data0, checkRadiationVariance: true), $"Data mismatch index {i}");
            }

            // Assert conversion Lists <-> Dictionaries
            for (var i = 0; i < foreCastSampleList1.Count; i++)
            {
                var data1 = foreCastSampleList1[i];
                var data2 = foreCastSampleList2[i];
                var timestamp = foreCastSampleList1[i].Time;
                Assert.IsTrue(AreAlmostEqual(data1, data2, checkRadiationVariance: false), $"Data mismatch index {i}");

                data1 = foreCastSampleDict1[timestamp];
                data2 = foreCastSampleDict2[timestamp];
                Assert.IsTrue(AreAlmostEqual(data1, data2, checkRadiationVariance: false), $"Data mismatch at {timestamp:u}");
            }
        }

        [TestMethod]
        public async Task TestTestStationsBlender()
        {
            // Arrange
            const int sampleMinutes = 15;
            var sampleInterval = TimeSpan.FromMinutes(sampleMinutes);
            var targetInterval = TimeSpan.FromMinutes(12);
            var sampleCount = 20;              // 20 hours 
            var nowYear = DateTime.UtcNow.Year;
            var sampleStartTime = new DateTime(nowYear, 5, 10, 12, 0, 0, DateTimeKind.Utc);

            var station1 = "station1";
            var station2 = "station2";
            var station3 = "station3";
            var station4 = "station4";
            var sampleSeries = MeteoDeterministicSeriesGenerator.GetMeteoSampleDictionary(sampleStartTime, sampleInterval, sampleCount, amplitude: 1.0);
            var stationSeriesDictionary = new Dictionary<string, Dictionary<DateTime, MeteoParameters>>()
            {
                { station1, sampleSeries },
                { station2, sampleSeries },
                { station3, sampleSeries },
                { station4, sampleSeries }
            };

            var stationsWeightDictionary = new Dictionary<string, WeightMeteoParameters>()
            {
                { station1, new WeightMeteoParameters
                    {
                        Weights = new Dictionary<MeteoParameterType, double>
                        {
                            { MeteoParameterType.SunshineDuration, 3.0 },
                            { MeteoParameterType.DirectRadiation, 3.0 },
                            { MeteoParameterType.DirectNormalIrradiance, 3.0 },
                            { MeteoParameterType.GlobalRadiation, 3.0 },
                            { MeteoParameterType.DiffuseRadiation, 3.0 },
                            { MeteoParameterType.Temperature, 1.0 },
                            { MeteoParameterType.WindSpeed, 1.0 },
                            { MeteoParameterType.WindDirection, 1.0 },
                            { MeteoParameterType.SnowDepth, 1.0 },
                            { MeteoParameterType.RelativeHumidity, 1.0 },
                            { MeteoParameterType.DewPoint, 1.0 },
                            { MeteoParameterType.RadiationVariance, 1.0 }
                        }
                    }
                },
                { station2, new WeightMeteoParameters
                    {
                        Weights = new Dictionary<MeteoParameterType, double>
                        {
                            { MeteoParameterType.SunshineDuration, 1.0 },
                            { MeteoParameterType.DirectRadiation, 1.0 },
                            { MeteoParameterType.DirectNormalIrradiance, 1.0 },
                            { MeteoParameterType.GlobalRadiation, 1.0 },
                            { MeteoParameterType.DiffuseRadiation, 1.0 },
                            { MeteoParameterType.Temperature, 1.0 },
                            { MeteoParameterType.WindSpeed, 1.0 },
                            { MeteoParameterType.WindDirection, 1.0 },
                            { MeteoParameterType.SnowDepth, 1.0 },
                            { MeteoParameterType.RelativeHumidity, 1.0 },
                            { MeteoParameterType.DewPoint, 1.0 },
                            { MeteoParameterType.RadiationVariance, 1.0 }
                        }
                    }
                },
                { station3, new WeightMeteoParameters
                    {
                        Weights = new Dictionary<MeteoParameterType, double>
                        {
                            { MeteoParameterType.SunshineDuration, 1.0 },
                            { MeteoParameterType.DirectRadiation, 1.0 },
                            { MeteoParameterType.DirectNormalIrradiance, 1.0 },
                            { MeteoParameterType.GlobalRadiation, 1.0 },
                            { MeteoParameterType.DiffuseRadiation, 1.0 },
                            { MeteoParameterType.Temperature, 0.0 },
                            { MeteoParameterType.WindSpeed, 0.0 },
                            { MeteoParameterType.WindDirection, 0.0 },
                            { MeteoParameterType.SnowDepth, 0.0 },
                            { MeteoParameterType.RelativeHumidity, 0.0 },
                            { MeteoParameterType.DewPoint, 0.0 },
                            { MeteoParameterType.RadiationVariance, 1.0 }
                        }
                    }
                },
                { station4, new WeightMeteoParameters
                    {
                        Weights = new Dictionary<MeteoParameterType, double>
                        {
                            { MeteoParameterType.SunshineDuration, 1.0 },
                            { MeteoParameterType.DirectRadiation, 1.0 },
                            { MeteoParameterType.DirectNormalIrradiance, 1.0 },
                            { MeteoParameterType.GlobalRadiation, 1.0 },
                            { MeteoParameterType.DiffuseRadiation, 1.0 },
                            { MeteoParameterType.Temperature, 0.0 },
                            { MeteoParameterType.WindSpeed, 0.0 },
                            { MeteoParameterType.WindDirection, 0.0 },
                            { MeteoParameterType.SnowDepth, 0.0 },
                            { MeteoParameterType.RelativeHumidity, 0.0 },
                            { MeteoParameterType.DewPoint, 0.0 },
                            { MeteoParameterType.RadiationVariance, 1.0 }
                        }
                    }
                }
            };

        var stationsDictionary = new Dictionary<string, (Dictionary<DateTime, MeteoParameters> stationSeries, Dictionary<MeteoParameterType, double> stationWeights)>();
            stationsDictionary[station1] = (stationSeriesDictionary[station1], stationsWeightDictionary[station1].Weights);
            stationsDictionary[station2] = (stationSeriesDictionary[station2], stationsWeightDictionary[station2].Weights);
            stationsDictionary[station3] = (stationSeriesDictionary[station3], stationsWeightDictionary[station3].Weights);
            stationsDictionary[station4] = (stationSeriesDictionary[station4], stationsWeightDictionary[station4].Weights);

            var blendedSeries = MeteoStationsBlender.BlendMeteoStationsData(stationsDictionary, addStationsRadiationVariance: false);


            // Assert conversion Lists <-> Dictionaries
            foreach (var record in sampleSeries)
            {
                var timestamp = record.Key;

                var data1 = sampleSeries[timestamp];
                var data2 = blendedSeries[timestamp];
                Assert.IsTrue(AreAlmostEqual(data1, data2, checkRadiationVariance: true), $"Data mismatch at {timestamp:u}");
            }
        }

        // ****************************************************************************************************************

        private static void TestIntervalConverterSeries(
            Dictionary<DateTime, MeteoParameters> SampleSeries,
            TimeSpan sampleInterval, TimeSpan targetInterval,
            int subPeriodsCount)
        {
            var shiftedSeries = new Dictionary<DateTime, MeteoParameters>();
            foreach (var sample in SampleSeries)
            {
                shiftedSeries[sample.Key.AddMinutes(5)] = sample.Value;
            }

            var syncedSeries = shiftedSeries;
            if (!MeteoIntervalConverter.FirstTimeStampIsSynced(shiftedSeries))
            {
                syncedSeries = MeteoIntervalConverter.SyncTimeStamps(shiftedSeries);
            }

            var slpittedSeries = MeteoIntervalConverter.MeteoIntervalSplitter(syncedSeries, subPeriodsCount);
            var aggregatedSeries = MeteoIntervalConverter.MeteoIntervalAggregator(slpittedSeries, subPeriodsCount);

            var fromToSeries = MeteoIntervalConverter.MeteoFromToConvertor(syncedSeries, targetInterval);
            var toFromSeries = MeteoIntervalConverter.MeteoFromToConvertor(fromToSeries, sampleInterval);

            // Assert conversion Lists <-> Dictionaries
            foreach (var record in SampleSeries)
            {
                var timestamp = record.Key;

                var data0 = SampleSeries[timestamp];
                var data1 = aggregatedSeries[timestamp];
                var data2 = toFromSeries[timestamp];

                Assert.IsTrue(AreAlmostEqual(data0, data1, checkRadiationVariance: true), $"Data mismatch at {timestamp:u}");
                Assert.IsTrue(AreAlmostEqual(data0, data2, checkRadiationVariance: true), $"Data mismatch at {timestamp:u}");
            }
        }
        public static bool AreAlmostEqual(MeteoParameters a, MeteoParameters b, bool checkRadiationVariance = false, double tolerance = 1e-6)
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
            if (checkRadiationVariance)
            {
                if (NotAlmostEqual(a.RadiationVariance, b.RadiationVariance, tolerance)) return false;
            }

            return true;
        }
    }
}
