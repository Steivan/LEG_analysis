using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.CoreLib.SampleData.SampleData;

namespace LEG.CoreLib.SampleData
{
    public class SampleSiteCoordinateProvider : ISiteCoordinateProvider
    {
        public IReadOnlyDictionary<string, SiteLocation> GetSiteCoordinates()
        {
            return DictionarySiteCoordinates.SiteLatLonElevDict;
        }
    }
}