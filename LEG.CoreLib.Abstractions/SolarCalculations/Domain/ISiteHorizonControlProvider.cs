using System.Collections.Generic;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain
{
    public interface ISiteHorizonControlProvider
    {
        IReadOnlyDictionary<string, (bool getHorizon, double aziStep)> GetSiteHorizonControls();
    }
}
