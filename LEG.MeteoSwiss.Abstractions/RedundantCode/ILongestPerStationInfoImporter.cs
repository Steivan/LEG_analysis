using LEG.MeteoSwiss.Abstractions.Models;
using System.Collections.Generic;

namespace LEG.MeteoSwiss.Abstractions
{
    public interface ILongestPerStationInfoImporter
    {
        Dictionary<string, LongestPerStationMetaInfo> Import(string filePath);
    }
}
