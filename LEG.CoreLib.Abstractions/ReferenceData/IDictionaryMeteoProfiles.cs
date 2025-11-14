using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using System.Collections.Generic;

namespace LEG.CoreLib.Abstractions.ReferenceData
{
    public interface IDictionaryMeteoProfiles
    {
        IReadOnlyDictionary<string, MeteoProfile> MeteoDict { get; }
    }
}