using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;
using LEG.PV.Data.Processor.Simulator;

namespace LEG.PV.Data.Processor.Interfaces
{
    public class MeteoRandomSeriesGenerator
    {
        public static Dictionary<DateTime, MeteoParameters> GetMeteoSampleDictionary(DateTime startTime, TimeSpan interval, int countOfRecords)
        {
            return MeteoSeriesSimulator.GetMeteoSampleDictionary(startTime, interval, countOfRecords);
        }
    }
}