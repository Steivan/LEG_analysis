using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.CoreLib.Abstractions.ReferenceData
{
    public interface IProfileToStationDictionary
    {
        Dictionary<string, Dictionary<string, WeightMeteoParameters>> ProfileToStationDictionary { get; }
    }
}
