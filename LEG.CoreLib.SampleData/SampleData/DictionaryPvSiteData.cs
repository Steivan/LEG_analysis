using static LEG.CoreLib.SampleData.SampleData.ListSites;
using static LEG.CoreLib.SampleData.SampleData.DictionarySiteCoordinates;
using static LEG.MeteoSwiss.Abstractions.ReferenceData.MeteoStations;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using static LEG.CoreLib.Abstractions.ReferenceData.SiteStatus;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal class DictionaryPvSiteData
    {
        internal static readonly Dictionary<string, PvSite> PvSiteDataDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Bagnera] = new PvSite(
                    SystemName: Bagnera,
                    EgId: "",
                    Status: Plan,
                    StreetName: "Bagnera",
                    HouseNumber: "187",
                    ZipNumber: "7550",
                    Town: "Scuol",
                    Lon: SiteLatLonElevDict[Bagnera].GetLongitude(),
                    Lat: SiteLatLonElevDict[Bagnera].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: SCU,
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 0
                ),

                [Bos_cha] = new PvSite(
                    SystemName: Bos_cha,
                    EgId: "",
                    Status: Plan,
                    StreetName: "Craista",
                    HouseNumber: "223",
                    ZipNumber: "7545",
                    Town: "Guarda",
                    Lon: SiteLatLonElevDict[Bos_cha].GetLongitude(),
                    Lat: SiteLatLonElevDict[Bos_cha].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: "Bos_cha_meteo_all",
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 0
                ),

                [Clozza] = new PvSite(
                    SystemName: Clozza,
                    EgId: "",
                    Status: Active,
                    StreetName: "Stradun",
                    HouseNumber: "248",
                    ZipNumber: "7550",
                    Town: "Scuol",
                    Lon: SiteLatLonElevDict[Clozza].GetLongitude(),
                    Lat: SiteLatLonElevDict[Clozza].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: SCU,
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 4,
                    IndicativeNrOfConsumers: 5
                ),

                [Ftan] = new PvSite(
                    SystemName: Ftan,
                    EgId: "",
                    Status: Plan,
                    StreetName: "",
                    HouseNumber: "",
                    ZipNumber: "",
                    Town: "Ftan Pitschen",
                    Lon: SiteLatLonElevDict[Ftan].GetLongitude(),
                    Lat: SiteLatLonElevDict[Ftan].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: SCU,
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 0
                ),

                [Fuorcla] = new PvSite(
                    SystemName: Fuorcla,
                    EgId: "",
                    Status: Active,
                    StreetName: "Via da l'Alp",
                    HouseNumber: "110",
                    ZipNumber: "7513",
                    Town: "Silvaplana-Surlej",
                    Lon: SiteLatLonElevDict[Fuorcla].GetLongitude(),
                    Lat: SiteLatLonElevDict[Fuorcla].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: COV,
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 1,
                    IndicativeNrOfConsumers: 0
                ),

                [Guldenen] = new PvSite(
                    SystemName: Guldenen,
                    EgId: "",
                    Status: Active,
                    StreetName: "Guldenenstrasse",
                    HouseNumber: "7B",
                    ZipNumber: "8127",
                    Town: "Forch",
                    Lon: SiteLatLonElevDict[Guldenen].GetLongitude(),
                    Lat: SiteLatLonElevDict[Guldenen].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: "Maur_meteo",
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 9
                ),

                [Kleiner] = new PvSite(
                    SystemName: Kleiner,
                    EgId: "",
                    Status: Active,
                    StreetName: "Unterdorfstrasse",
                    HouseNumber: "11",
                    ZipNumber: "8489",
                    Town: "Wildberg",
                    Lon: SiteLatLonElevDict[Kleiner].GetLongitude(),
                    Lat: SiteLatLonElevDict[Kleiner].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: "Maur_meteo",
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 0
                ),

                [Liuns] = new PvSite(
                    SystemName: Liuns,
                    EgId: "",
                    Status: Active,
                    StreetName: "Via da Liuns",
                    HouseNumber: "751",                       // Could also be 750
                    ZipNumber: "7550",
                    Town: "Scuol",
                    Lon: SiteLatLonElevDict[Liuns].GetLongitude(),
                    Lat: SiteLatLonElevDict[Liuns].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: SCU,
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 0
                ),

                [Lotz] = new PvSite(
                    SystemName: Lotz,
                    EgId: "",
                    Status: Active,
                    StreetName: "Chisligstrasse",
                    HouseNumber: "7",
                    ZipNumber: "8127",
                    Town: "Forch",
                    Lon: SiteLatLonElevDict[Lotz].GetLongitude(),
                    Lat: SiteLatLonElevDict[Lotz].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: "Maur_meteo",
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 1,
                    IndicativeNrOfConsumers: 0
                ),

                [Senn] = new PvSite(
                    SystemName: Senn,
                    EgId: "",
                    Status: Active,
                    StreetName: "Hubrainstasse",
                    HouseNumber: "46",
                    ZipNumber: "8124",
                    Town: "Maur",
                    Lon: SiteLatLonElevDict[Senn].GetLongitude(),
                    Lat: SiteLatLonElevDict[Senn].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: "Senn_meteo",
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 1
                ),

                [SennV] = new PvSite(
                    SystemName: SennV,
                    EgId: "",
                    Status: Active,
                    StreetName: "Hubrainstasse",
                    HouseNumber: "50",
                    ZipNumber: "8124",
                    Town: "Maur",
                    Lon: SiteLatLonElevDict[Senn].GetLongitude(),
                    Lat: SiteLatLonElevDict[Senn].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: "Senn_meteo",
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 2
                ),

                [TestSite] = new PvSite(
                    SystemName: TestSite,
                    EgId: "",
                    Status: Test,
                    StreetName: "TestAddress",
                    HouseNumber: "",
                    ZipNumber: "TestZIP",
                    Town: "TestTown",
                    Lon: 10.0,
                    Lat: 50.0,
                    UtcShift: -1,
                    MeteoId: "TestProfile",
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 1
                ),

                [Tof] = new PvSite(
                    SystemName: Tof,
                    EgId: "",
                    Status: Active,
                    StreetName: "Tof",
                    HouseNumber: "",  // Old house: "Nr. 751",
                    ZipNumber: "7550",
                    Town: "Scuol",
                    Lon: SiteLatLonElevDict[Tof].GetLongitude(),
                    Lat: SiteLatLonElevDict[Tof].GetLatitude(),
                    UtcShift: -1,
                    MeteoId: SCU,
                    IndicativeNrOfInverters: 1,
                    IndicativeNrOfRoofs: 2,
                    IndicativeNrOfConsumers: 0
                ),
            };

    }
}
