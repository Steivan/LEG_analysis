using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;
using static LEG.PV.Data.Processor.Simulator.SimulatorParameters;

namespace LEG.PV.Data.Processor.Helpers
{
    internal class WindVectorsAggregator
    {
        internal static (double windSpeed, double WindDirection) MeanWindVectorFromList(List<MeteoParameters> inputRecords, double[]? weights = null)
        {
            var count = inputRecords.Count;
            if (count == 0)
                return (0.0, 0.0);

            double[] normalizedWeights = Enumerable.Repeat(1.0 / count, count).ToArray();
            if (weights != null && weights.Length >= count)
            {
                var shortenedWeights = weights.Take(count).ToArray();
                var weightsMin = shortenedWeights.Min();
                var weightsSum = shortenedWeights.Sum();
                if (weightsMin >= 0.0 && weightsSum > 0.0)
                {
                    normalizedWeights = shortenedWeights.Select(w => w / weightsSum).ToArray();
                }
            }

            double sumWind_X = 0.0;
            double sumWind_Y = 0.0;
            for (var i = 0; i < count; i++)
            {
                var r = inputRecords[i];
                var w = normalizedWeights[i];
                if (r.WindSpeed.HasValue && r.WindDirection.HasValue)
                {
                    double windDirRad = r.WindDirection.Value * radPerDeg;
                    sumWind_X += r.WindSpeed.Value * Math.Cos(windDirRad) * w;
                    sumWind_Y += r.WindSpeed.Value * Math.Sin(windDirRad) * w;
                }
            }
            var windSpeed = Math.Sqrt(sumWind_X * sumWind_X + sumWind_Y * sumWind_Y);
            var windDirection = Math.Atan2(sumWind_Y, sumWind_X) / radPerDeg;
            if (windDirection < 0) windDirection += 360.0;

            return (windSpeed, windDirection);
        }

        internal static (double windSpeed, double WindDirection) MeanWindVectorFromDict(Dictionary<DateTime, MeteoParameters> inputRecords, double[]? weights = null)
        {
            return MeanWindVectorFromList(MeteoSeriesConverter.MeteoDictToList(inputRecords), weights);
        }
    }
}