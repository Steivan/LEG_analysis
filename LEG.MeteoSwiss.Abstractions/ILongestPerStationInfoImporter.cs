using System.Collections.Generic;

namespace LEG.MeteoSwiss.Abstractions
{
    public interface ILongestPerStationInfoImporter
    {
        Dictionary<string, LongestPerStationMetaInfo> Import(string filePath);
    }
}
