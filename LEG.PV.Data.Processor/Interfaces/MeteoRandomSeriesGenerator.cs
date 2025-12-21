using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;
using LEG.PV.Data.Processor.Simulator;

namespace LEG.PV.Data.Processor.Interfaces
{
    public class MeteoRandomSeriesGenerator
    {
        public static Dictionary<DateTime, MeteoParameters> GetMeteoSampleDictionary(
            DateTime startTime, TimeSpan interval, int countOfRecords,
            double siteLatitude = 46,
            double siteLongitude = 10,
            bool applySnowDays = false,
            bool applyFoggyDays = false,
            bool applyOutliers = false)
        {
            return MeteoSeriesSimulator.GetMeteoSampleDictionary(
                startTime, interval, countOfRecords,
                siteLatitude: siteLatitude,
                siteLongitude: siteLongitude,
                applySnowDays: applySnowDays,
                applyFoggyDays: applyFoggyDays,
                applyOutliers: applyOutliers);
        }
    }
}