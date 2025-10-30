using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.CoreLib.SampleData.SampleData;

namespace LEG.CoreLib.SampleData
{
    public class SampleSiteHorizonControlProvider : ISiteHorizonControlProvider
    {
        public IReadOnlyDictionary<string, (bool getHorizon, double aziStep)> GetSiteHorizonControls()
        {
            return DictionarySiteHorizonControls.SiteGetHorizonDict;
        }
    }
}