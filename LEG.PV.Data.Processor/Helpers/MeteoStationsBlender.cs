using LEG.MeteoSwiss.Abstractions.Models;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.PV.Data.Processor.Helpers
{
    public class MeteoStationsBlender
    {
        public static Dictionary<DateTime, MeteoParameters> BlendMeteoStationsData(
            Dictionary<string, (Dictionary<DateTime, MeteoParameters> stationSeries, Dictionary<MeteoParameterType, double> stationWeights)> stationsDictionary,
            bool addStationsRadiationVariance = true)
        {
            var stationKeys = stationsDictionary.Keys.ToList();
            var stationsCount = stationKeys.Count;
            var firstSeries = stationsDictionary.First().Value.stationSeries;

            var interval = firstSeries.First().Value.Interval;
            var anchor = firstSeries.First().Value.Anchor;

            var firstCommonTimeStamp = firstSeries.Keys.Min();
            var lastCommonTimeStamp = firstSeries.Keys.Max();

            var weightsSunshineDuration = new double[stationsCount];
            var weightsDirectRadiation = new double[stationsCount];
            var weightsDirectNormalIrradiance = new double[stationsCount];
            var weightsGlobalRadiation = new double[stationsCount];
            var weightsDiffuseRadiation = new double[stationsCount];
            var weightsTemperature = new double[stationsCount];
            var weightsWindSpeed = new double[stationsCount];
            var weightsWindDirection = new double[stationsCount];
            var weightsSnowDepth = new double[stationsCount];
            var weightsRelativeHumidity = new double[stationsCount];
            var weightsDewPoint = new double[stationsCount];
            var weightsRadiationVariance = new double[stationsCount];

            for (int stationIndex = 0; stationIndex < stationsCount; stationIndex++)
            {
                var stationKey = stationKeys[stationIndex];
                var (stationSeries, weightsDictionary) = stationsDictionary[stationKey];

                var stationFirstTimeStamp = stationSeries.Keys.Min();
                var stationLastTimeStamp = stationSeries.Keys.Max();
                firstCommonTimeStamp = DateTime.Compare(firstCommonTimeStamp, stationFirstTimeStamp) < 0 ? stationFirstTimeStamp : firstCommonTimeStamp;
                lastCommonTimeStamp = DateTime.Compare(lastCommonTimeStamp, stationLastTimeStamp) > 0 ? stationLastTimeStamp : lastCommonTimeStamp;

                weightsSunshineDuration[stationIndex] = weightsDictionary[MeteoParameterType.SunshineDuration];
                weightsDirectRadiation[stationIndex] = weightsDictionary[MeteoParameterType.DirectRadiation];
                weightsDirectNormalIrradiance[stationIndex] = weightsDictionary[MeteoParameterType.DirectNormalIrradiance];
                weightsGlobalRadiation[stationIndex] = weightsDictionary[MeteoParameterType.GlobalRadiation];
                weightsDiffuseRadiation[stationIndex] = weightsDictionary[MeteoParameterType.DiffuseRadiation];
                weightsTemperature[stationIndex] = weightsDictionary[MeteoParameterType.Temperature];
                weightsWindSpeed[stationIndex] = weightsDictionary[MeteoParameterType.WindSpeed];
                weightsWindDirection[stationIndex] = weightsDictionary[MeteoParameterType.WindDirection];
                weightsSnowDepth[stationIndex] = weightsDictionary[MeteoParameterType.SnowDepth];
                weightsRelativeHumidity[stationIndex] = weightsDictionary[MeteoParameterType.RelativeHumidity];
                weightsDewPoint[stationIndex] = weightsDictionary[MeteoParameterType.DewPoint];
                weightsRadiationVariance[stationIndex] = weightsDictionary[MeteoParameterType.RadiationVariance];
            }

            weightsSunshineDuration = NormalizeWeights(weightsSunshineDuration);
            weightsDirectRadiation = NormalizeWeights(weightsDirectRadiation);
            weightsDirectNormalIrradiance = NormalizeWeights(weightsDirectNormalIrradiance);
            weightsGlobalRadiation = NormalizeWeights(weightsGlobalRadiation);
            weightsDiffuseRadiation = NormalizeWeights(weightsDiffuseRadiation);
            weightsTemperature = NormalizeWeights(weightsTemperature);
            weightsWindSpeed = NormalizeWeights(weightsWindSpeed);
            weightsWindDirection = NormalizeWeights(weightsWindDirection);
            weightsSnowDepth = NormalizeWeights(weightsSnowDepth);
            weightsRelativeHumidity = NormalizeWeights(weightsRelativeHumidity);
            weightsDewPoint = NormalizeWeights(weightsDewPoint);
            weightsRadiationVariance = NormalizeWeights(weightsRadiationVariance);

            var nullRecord = new MeteoParameters(
                time: DateTime.MinValue,
                interval: interval,
                anchor: anchor,
                sunshineDuration: null,
                directRadiation: null,
                directNormalIrradiance: null,
                globalRadiation: null,
                diffuseRadiation: null,
                temperature: null,
                windSpeed: null,
                windDirection: null,
                snowDepth: null,
                relativeHumidity: null,
                dewPoint: null,
                radiationVariance: null);

            var blendedSeries = new Dictionary<DateTime, MeteoParameters>();
            for (var timeStamp = firstCommonTimeStamp; timeStamp <= lastCommonTimeStamp; timeStamp += interval)
            {
                double? blendedSunshineDuration = null;
                double? blendedDirectRadiation = null;
                double? blendedDirectNormalIrradiance = null;
                double? blendedGlobalRadiation = null;
                double? blendedDiffuseRadiation = null;
                double? blendedTemperature = null;
                double? blendedWindSpeed = null;
                double? blendedWindDirection = null;
                double? blendedSnowDepth = null;
                double? blendedRelativeHumidity = null;
                double? blendedDewPoint = null;
                double? blendedRadiationVariance = null;
                var countDirectRadiation = 0;
                var sumDirectNormal = 0.0;
                var sumDirectRadiatioSquared = 0.0;
                for (int stationIndex = 0; stationIndex < stationsCount; stationIndex++)
                {
                    var stationKey = stationKeys[stationIndex];
                    var (stationSeries, _) = stationsDictionary[stationKey];
                    var newRecord = stationSeries.ContainsKey(timeStamp) ? stationSeries[timeStamp] : nullRecord;
                    var newWeight = weightsSunshineDuration[stationIndex];

                    blendedSunshineDuration = updateValue(blendedSunshineDuration, newRecord.SunshineDuration, newWeight);
                    blendedDirectRadiation = updateValue(blendedDirectRadiation, newRecord.DirectRadiation, newWeight);
                    blendedDirectNormalIrradiance = updateValue(blendedDirectNormalIrradiance, newRecord.DirectNormalIrradiance, newWeight);
                    blendedGlobalRadiation = updateValue(blendedGlobalRadiation, newRecord.GlobalRadiation, newWeight);
                    blendedDiffuseRadiation = updateValue(blendedDiffuseRadiation, newRecord.DiffuseRadiation, newWeight);
                    blendedTemperature = updateValue(blendedTemperature, newRecord.Temperature, newWeight);
                    blendedWindSpeed = updateValue(blendedWindSpeed, newRecord.WindSpeed, newWeight);
                    blendedWindDirection = updateValue(blendedWindDirection, newRecord.WindDirection, newWeight);
                    blendedSnowDepth = updateValue(blendedSnowDepth, newRecord.SnowDepth, newWeight);
                    blendedRelativeHumidity = updateValue(blendedRelativeHumidity, newRecord.RelativeHumidity, newWeight);
                    blendedDewPoint = updateValue(blendedDewPoint, newRecord.DewPoint, newWeight);
                    blendedRadiationVariance = updateValue(blendedRadiationVariance, newRecord.RadiationVariance, newWeight);
                    if (newRecord.DirectRadiation.HasValue)
                    {
                        countDirectRadiation++;
                        sumDirectNormal += newRecord.DirectRadiation.Value;
                        sumDirectRadiatioSquared += newRecord.DirectRadiation.Value * newRecord.DirectRadiation.Value;
                    }
                }
                if (addStationsRadiationVariance && countDirectRadiation > 1)
                {
                    var stationsDirectRadiationMean = sumDirectNormal / (double)countDirectRadiation;
                    var stationsDirectRadiationVariance = (sumDirectRadiatioSquared / (double)countDirectRadiation) - (stationsDirectRadiationMean * stationsDirectRadiationMean);
                    blendedRadiationVariance = updateValue(blendedRadiationVariance, stationsDirectRadiationVariance, 1.0);
                }
                blendedSeries[timeStamp] = new MeteoParameters(
                    time: timeStamp,
                    interval: interval,
                    anchor: anchor,
                    sunshineDuration: blendedSunshineDuration,
                    directRadiation: blendedDirectRadiation,
                    directNormalIrradiance: blendedDirectNormalIrradiance,
                    globalRadiation: blendedGlobalRadiation,
                    diffuseRadiation: blendedDiffuseRadiation,
                    temperature: blendedTemperature,
                    windSpeed: blendedWindSpeed,
                    windDirection: blendedWindDirection,
                    snowDepth: blendedSnowDepth,
                    relativeHumidity: blendedRelativeHumidity,
                    dewPoint: blendedDewPoint,
                    radiationVariance: blendedRadiationVariance);
            }

            return blendedSeries;
        }

        // *******************************************************************************************************

        private static double[] NormalizeWeights(double[] weights)
        {
            var weightSum = weights.Sum();
            if (weightSum <= 0.0)
                throw new ArgumentException("Sum of weights must be greater than zero");
            return weights.Select(w => w / weightSum).ToArray();
        }

        private static double? updateValue(double? parameterValue, double? newValue, double newWeight)
        {
            if (parameterValue.HasValue && newValue.HasValue)
            {
                return parameterValue + newValue.Value * newWeight;
            }
            else if (newValue.HasValue)
            {
                return newValue * newWeight;
            }
            else
            {
                return parameterValue;
            }
        }
    }
}
