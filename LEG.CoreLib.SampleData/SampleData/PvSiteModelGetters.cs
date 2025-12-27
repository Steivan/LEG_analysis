using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.MeteoSwiss.Abstractions.Models;
using LEG.SwissTopo.Client.SwissTopo;
using static LEG.CoreLib.SampleData.ReferenceData.MeteoStationProfile;
using static LEG.CoreLib.SampleData.SampleData.DictionaryPvSiteModel;
using static LEG.CoreLib.SampleData.SampleData.DictionarySiteCoordinates;
using static LEG.CoreLib.SampleData.SampleData.DictionarySiteHorizonControls;
using static LEG.CoreLib.SampleData.SampleData.SiteNamesList;

namespace LEG.CoreLib.SampleData.SampleData
{
    public class PvSiteModelGetters
    {
        public static List<string> GetSitesList() => SitesList;

        public static IPvSiteModel GetSiteDataModel(string siteId)
        {
            if (!PvSiteModelDict.TryGetValue(siteId, out var siteDataModel))
            { throw new ArgumentException($"Site ID '{siteId}' not found."); }

            return siteDataModel;
        }

        public static async Task<IPvSiteModel> GetSiteDataModelAsync(string siteId)
        {
            if (!PvSiteModelDict.TryGetValue(siteId, out var siteDataModel))
                throw new ArgumentException($"Site ID '{siteId}' not found.");

            await siteDataModel.FetchBuildingPropertiesAsync(
                new BuildingFinder(),
                new CoordinateTransformation());
            return siteDataModel;
        }

        public static SiteLocation GetSiteCoordinates(string siteId)
        {
            if (!SiteLatLonElevDict.TryGetValue(siteId, out var siteLocation))
                throw new ArgumentException($"Site ID '{siteId}' not found.");

            return siteLocation;
        }


        public static (bool getHorizon, double aziStep) GetSiteHorizonControls(string siteId)
        {
            if (!SiteGetHorizonDict.TryGetValue(siteId, out var horizonControls))
                throw new ArgumentException($"Site ID '{siteId}' not found.");

            return horizonControls;
        }


        public static Dictionary<string, WeightMeteoParameters> GetSiteMeteoGroup(string siteId)
        {
            var meteoGroup = GetSiteDataModel(siteId).PvSite.MeteoGroupId ?? "";
            var validMeteoGroup = ProfileToStationDictionary.Keys.Contains(meteoGroup);
            if (meteoGroup == "" || !validMeteoGroup)
            {
                throw new Exception($"No MeteoGroup has been assigned to Site ID {siteId}");
            }

            return ProfileToStationDictionary[meteoGroup];
        }
    }
}