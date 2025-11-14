using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.SwissTopo.Client.SwissTopo;
using static LEG.CoreLib.SampleData.SampleData.DictionaryPvSiteModel;
using static LEG.CoreLib.SampleData.SampleData.DictionarySiteCoordinates;
using static LEG.CoreLib.SampleData.SampleData.DictionarySiteHorizonControls;
using static LEG.CoreLib.SampleData.SampleData.ListSites;

namespace LEG.CoreLib.SampleData.SampleData
{
    public class PvSiteModelGetters
    {
        public static List<string> GetSitesList() => SitesList;

        public static IPvSiteModel GetSiteDataModel(string sampleId)
        {
            if (!PvSiteModelDict.TryGetValue(sampleId, out var siteDataModel))
            { throw new ArgumentException($"Sample ID '{sampleId}' not found."); }

            return siteDataModel;
        }

        public static async Task<IPvSiteModel> GetSiteDataModelAsync(string sampleId)
        {
            if (!PvSiteModelDict.TryGetValue(sampleId, out var siteDataModel))
                throw new ArgumentException($"Sample ID '{sampleId}' not found.");

            await siteDataModel.FetchBuildingPropertiesAsync(
                new BuildingFinder(),
                new CoordinateTransformation());
            return siteDataModel;
        }

        public static SiteLocation GetSiteCoordinates(string sampleId)
        {
            if (!SiteLatLonElevDict.TryGetValue(sampleId, out var siteLocation))
                throw new ArgumentException($"Sample ID '{sampleId}' not found.");

            return siteLocation;
        }


        public static (bool getHorizon, double aziStep) GetSiteHorizonControls(string sampleId)
        {
            if (!SiteGetHorizonDict.TryGetValue(sampleId, out var horizonControls))
                throw new ArgumentException($"Sample ID '{sampleId}' not found.");

            return horizonControls;
        }

    }
}