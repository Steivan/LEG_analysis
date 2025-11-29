using LEG.MeteoSwiss.Abstractions.Models;
using System.Collections.Generic;

namespace LEG.MeteoSwiss.Abstractions
{
    public interface IStationMetaImporter
    {
        Dictionary<string, StationMetaInfo> Import(string filePath);
    }
}
