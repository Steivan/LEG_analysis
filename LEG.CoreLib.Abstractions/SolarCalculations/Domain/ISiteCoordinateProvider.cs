using System.Collections.Generic;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain
{
    public interface ISiteCoordinateProvider
    {
        IReadOnlyDictionary<string, SiteLocation> GetSiteCoordinates();
    }
}