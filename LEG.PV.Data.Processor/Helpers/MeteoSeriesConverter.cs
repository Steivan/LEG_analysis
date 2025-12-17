using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.PV.Data.Processor.Helpers
{
    public class MeteoSeriesConverter
    {
        public static List<MeteoParameters> MeteoDictToList(Dictionary<DateTime, MeteoParameters> inputRecords)
        {
            return inputRecords.Select(kv => kv.Value).ToList();
        }

        public static Dictionary<DateTime, MeteoParameters> MeteoListToDict(List<MeteoParameters> inputRecords)
        {
            var dict = new Dictionary<DateTime, MeteoParameters>();
            foreach (var record in inputRecords)
            {
                dict[record.Time] = record;
            }

            return dict;
        }
    }
}
