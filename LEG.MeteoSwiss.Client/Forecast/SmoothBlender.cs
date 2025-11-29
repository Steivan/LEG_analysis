
using LEG.MeteoSwiss.Abstractions.Models;
    
    namespace LEG.MeteoSwiss.Client.Forecast
{

internal class SmoothBlender
    {
        const double DegToRad = Math.PI / 180;

        public static Dictionary<DateTime, MeteoParameters> SmoothBlendedPeriod(Dictionary<DateTime, MeteoParameters> quarterForecast, int filterId = 0)
        {
            void UpdateRowSource(ref double sumValues, ref double sumWeights, double value, double weight)
            {
                sumValues += value * weight;
                sumWeights += weight;
            }

            var filterIndices = new int[] {  -3,   -2,   -1,    0,    1,   2 };     // Symmetric filter shifted to the left for causal filtering (aggregated hourly data is reported at the end of 1h period
            List<List<double>> filterWeightsList = [
                [ 0.10, 0.15, 0.25, 0.25, 0.15, 0.10 ],
                [ 0.05, 0.15, 0.30, 0.30, 0.15, 0.05 ],
                [ 0.02, 0.08, 0.40, 0.40, 0.08, 0.02 ],
                [ 0.01, 0.06, 0.43, 0.43, 0.06, 0.01 ]
            ];

            filterId = filterId % filterWeightsList.Count;
            var filterWeights = filterWeightsList[filterId].ToArray();

            var forecastCount = quarterForecast.Count;
            var filterLength = Math.Min(filterIndices.Length, filterWeights.Length);

            var sortedKeys = quarterForecast.Keys.OrderBy(dt => dt).ToList();
            var smoothedQuarterForecast = new Dictionary<DateTime, MeteoParameters>();
            for (int i = 0; i < forecastCount; i++)
            {
                var quarterTime = sortedKeys[i];

                var sumSunshineDuration = 0.0;
                var sumDirectRadiation = 0.0;
                var sumDirectNormalIrradiance = 0.0;
                var sumGlobalRadiation = 0.0;
                var sumDiffuseRadiation = 0.0;
                var sumTemperatue = 0.0;
                var sumWindSpeed = 0.0;
                var sumSnowDepth = 0.0;
                var sumDirectRadiationVariance = 0.0;

                var weightSunshineDuration = 0.0;
                var weightDirectRadiation = 0.0;
                var weightDirectNormalIrradiance = 0.0;
                var weightGlobalRadiation = 0.0;
                var weightDiffuseRadiation = 0.0;
                var weightTemperature = 0.0;
                var weightWindSpeed = 0.0;
                var weightSnowDepth = 0.0;
                var weightDirectRadiationVariance = 0.0;

                for (int j = 0; j < filterLength; j++)
                {
                    int index_ij = i + filterIndices[j];
                    if (index_ij >= 0 && index_ij < forecastCount)
                    {
                        var quarterForecast_ij = quarterForecast[sortedKeys[index_ij]];
                        double weight = filterWeights[j];

                        if (quarterForecast_ij.SunshineDuration.HasValue) UpdateRowSource(ref sumSunshineDuration, ref weightSunshineDuration, quarterForecast_ij.SunshineDuration.Value, weight);
                        if (quarterForecast_ij.DirectRadiation.HasValue) UpdateRowSource(ref sumDirectRadiation, ref weightDirectRadiation, quarterForecast_ij.DirectRadiation.Value, weight);
                        if (quarterForecast_ij.DirectNormalIrradiance.HasValue) UpdateRowSource(ref sumDirectNormalIrradiance, ref weightDirectNormalIrradiance, quarterForecast_ij.DirectNormalIrradiance.Value, weight);
                        if (quarterForecast_ij.GlobalRadiation.HasValue) UpdateRowSource(ref sumGlobalRadiation, ref weightGlobalRadiation, quarterForecast_ij.GlobalRadiation.Value, weight);
                        if (quarterForecast_ij.DiffuseRadiation.HasValue) UpdateRowSource(ref sumDiffuseRadiation, ref weightDiffuseRadiation, quarterForecast_ij.DiffuseRadiation.Value, weight);
                        if (quarterForecast_ij.Temperature.HasValue) UpdateRowSource(ref sumTemperatue, ref weightTemperature, quarterForecast_ij.Temperature.Value, weight);
                        if (quarterForecast_ij.WindSpeed.HasValue) UpdateRowSource(ref sumWindSpeed, ref weightWindSpeed, quarterForecast_ij.WindSpeed.Value, weight);
                        if (quarterForecast_ij.SnowDepth.HasValue) UpdateRowSource(ref sumSnowDepth, ref weightSnowDepth, quarterForecast_ij.SnowDepth.Value, weight);
                        if (quarterForecast_ij.DirectRadiationVariance.HasValue) UpdateRowSource(ref sumDirectRadiationVariance, ref weightDirectRadiationVariance, quarterForecast_ij.DirectRadiationVariance.Value, weight);
                    }
                }
                smoothedQuarterForecast[quarterTime] = new MeteoParameters(
                    Time: quarterTime,
                    weightSunshineDuration > 0 ? sumSunshineDuration / weightSunshineDuration : null,
                    weightDirectRadiation > 0 ? sumDirectRadiation / weightDirectRadiation : null,
                    weightDirectNormalIrradiance > 0 ? sumDirectNormalIrradiance / weightDirectNormalIrradiance : null,
                    weightGlobalRadiation > 0 ? sumGlobalRadiation / weightGlobalRadiation : null,
                    weightDiffuseRadiation > 0 ? sumDiffuseRadiation / weightDiffuseRadiation : null,
                    weightTemperature > 0 ? sumTemperatue / weightTemperature : null,
                    weightWindSpeed > 0 ? sumWindSpeed / weightWindSpeed : null,
                    weightSnowDepth > 0 ? sumSnowDepth / weightSnowDepth : null,
                    weightDirectRadiationVariance > 0 ? sumDirectRadiationVariance / weightDirectRadiationVariance : null
                );
            }

            return smoothedQuarterForecast;
        }
    }
}

