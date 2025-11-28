
namespace LEG.MeteoSwiss.Client.Forecast
{
    public record BlendedPeriod
    (
        DateTime Time,          // Timestamp of the blended forecast period: request is explicitely in UTC
        double? TempC,
        double? WindKmh,
        double? DirectHRWm2,    // Direct horizontal raduiation (DHI) in W/m² : used in nearcast model
        double? DNIWm2,         // Direct normal irradiance (DNI) in W/m² : used in midcast and longcast model
        double? DiffuseHRWm2,
        double? SnowDepthM      // SnowDepth is stored in meters (m) as received from API
    )
    {
        public double? GlobalHRWm2 = (DirectHRWm2.HasValue && DiffuseHRWm2.HasValue) ? DirectHRWm2.Value + DiffuseHRWm2.Value : (double?)null;
        public double? SnowDepthCm => SnowDepthM.HasValue ? SnowDepthM.Value * 100.0 : (double?)null;
    }
internal class SmoothBlender
    {
        const double DegToRad = Math.PI / 180;

        public static Dictionary<DateTime, BlendedPeriod> SmoothBlendedPeriod(Dictionary<DateTime, BlendedPeriod> quarterForecast, int filterId = 0)
        {
            void UpdateRowSource(ref double sumValues, ref double sumWeights, double value, double weight)
            {
                sumValues += value * weight;
                sumWeights += weight;
            }

            var filterIndices = new int[] {  -3,   -2,   -1,    0,    1,   2 };
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
            var smoothedQuarterForecast = new Dictionary<DateTime, BlendedPeriod>();
            for (int i = 0; i < forecastCount; i++)
            {
                var quarterTime = sortedKeys[i];

                var sumTempC = 0.0;
                var sumWindKmh = 0.0;
                var sumDirectHRWm2 = 0.0;
                var sumDNIWm2 = 0.0;
                var sumDiffuseHRWm2 = 0.0;
                var sumSnowDepthM = 0.0;

                var weightTempC = 0.0;
                var weightWindKmh = 0.0;
                var weightDirectHRWm2 = 0.0;
                var weightDNIWm2 = 0.0;
                var weightDiffuseHRWm2 = 0.0;
                var weightSnowDepthM = 0.0;

                for (int j = 0; j < filterLength; j++)
                {
                    int index_ij = i + filterIndices[j];
                    if (index_ij >= 0 && index_ij < forecastCount)
                    {
                        var quarterForecast_ij = quarterForecast[sortedKeys[index_ij]];
                        double weight = filterWeights[j];

                        if (quarterForecast_ij.TempC.HasValue) UpdateRowSource(ref sumTempC, ref weightTempC, quarterForecast_ij.TempC.Value, weight);
                        if (quarterForecast_ij.WindKmh.HasValue) UpdateRowSource(ref sumWindKmh, ref weightWindKmh, quarterForecast_ij.WindKmh.Value, weight);
                        if (quarterForecast_ij.DirectHRWm2.HasValue) UpdateRowSource(ref sumDirectHRWm2, ref weightDirectHRWm2, quarterForecast_ij.DirectHRWm2.Value, weight);
                        if (quarterForecast_ij.DNIWm2.HasValue) UpdateRowSource(ref sumDNIWm2, ref weightDNIWm2, quarterForecast_ij.DNIWm2.Value, weight);
                        if (quarterForecast_ij.DiffuseHRWm2.HasValue) UpdateRowSource(ref sumDiffuseHRWm2, ref weightDiffuseHRWm2, quarterForecast_ij.DiffuseHRWm2.Value, weight);
                        if (quarterForecast_ij.SnowDepthM.HasValue) UpdateRowSource(ref sumSnowDepthM, ref weightSnowDepthM, quarterForecast_ij.SnowDepthM.Value, weight);
                    }
                }
                smoothedQuarterForecast[quarterTime] = new BlendedPeriod(
                    Time: quarterTime,
                    weightTempC > 0 ? sumTempC / weightTempC : null,
                    weightWindKmh > 0 ? sumWindKmh / weightWindKmh : null,
                    weightDirectHRWm2 > 0 ? sumDirectHRWm2 / weightDirectHRWm2 : null,
                    weightDNIWm2 > 0 ? sumDNIWm2 / weightDNIWm2 : null,
                    weightDiffuseHRWm2 > 0 ? sumDiffuseHRWm2 / weightDiffuseHRWm2 : null,
                    weightSnowDepthM > 0 ? sumSnowDepthM / weightSnowDepthM : null
                );
            }

            return smoothedQuarterForecast;
        }
    }
}

