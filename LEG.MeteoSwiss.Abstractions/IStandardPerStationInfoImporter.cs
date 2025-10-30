using System.Collections.Generic;

namespace LEG.MeteoSwiss.Abstractions
{
    public interface IStandardPerStationInfoImporter
    {
        Dictionary<string, StandardPerStationMetaInfo> Import(string filePath);
    }
}
