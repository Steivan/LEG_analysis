using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace LEG.CoreLib.Abstractions.ReferenceData
{
    public interface IDictionaryMeteoProfiles
    {
        IReadOnlyDictionary<string, MeteoProfile> MeteoDict { get; }
    }
}