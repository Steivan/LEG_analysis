using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.CoreLib.SampleData.ReferenceData;
using LEG.CoreLib.SolarCalculations.Domain;
using static LEG.CoreLib.SampleData.SampleData.DictionaryPvConsumers;
using static LEG.CoreLib.SampleData.SampleData.DictionaryPvInverters;
using static LEG.CoreLib.SampleData.SampleData.DictionaryPvRoofData;
using static LEG.CoreLib.SampleData.SampleData.DictionaryPvSiteData;
using static LEG.CoreLib.SampleData.SampleData.ListSites;


namespace LEG.CoreLib.SampleData.SampleData
{
    internal class DictionaryPvSiteModel
    {
        private static readonly DictionaryMeteoProfiles MeteoProfiles = new();

        internal static readonly Dictionary<string, PvSiteModel> PvSiteModelDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Bagnera] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Bagnera],
                    inverters: [PvInverterDataDict[Bagnera + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Bagnera + "_1", [PvRoofDataDict[Bagnera + "_1"], PvRoofDataDict[Bagnera + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[Bagnera + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Bagnera].MeteoId]
                ),

                [Bos_cha] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Bos_cha],
                    inverters: [PvInverterDataDict[Bos_cha + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Bos_cha + "_1", [PvRoofDataDict[Bos_cha + "_1"], PvRoofDataDict[Bos_cha + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[Bos_cha + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Bos_cha].MeteoId]
                ),

                [Clozza] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Clozza],
                    inverters: [PvInverterDataDict[Clozza + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Clozza + "_1", [PvRoofDataDict[Clozza + "_1"], PvRoofDataDict[Clozza + "_2"], PvRoofDataDict[Clozza + "_3"]] }
                    },
                    consumers: [PvConsumerDataDict[Clozza + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Clozza].MeteoId]
                ),

                [Ftan] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Ftan],
                    inverters: [PvInverterDataDict[Ftan + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Ftan + "_1", [PvRoofDataDict[Ftan + "_1"], PvRoofDataDict[Ftan + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[Ftan + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Ftan].MeteoId]
                ),

                [Fuorcla] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Fuorcla],
                    inverters: [PvInverterDataDict[Fuorcla + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Fuorcla + "_1", [PvRoofDataDict[Fuorcla + "_1"]] }
                    },
                    consumers: [PvConsumerDataDict[Fuorcla + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Fuorcla].MeteoId]
                ),

                [Guldenen] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Guldenen],
                    inverters: [PvInverterDataDict[Guldenen + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Guldenen + "_1", [PvRoofDataDict[Guldenen + "_1"], PvRoofDataDict[Guldenen + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[Guldenen + "_1"], PvConsumerDataDict[Guldenen + "_2"], PvConsumerDataDict[Guldenen + "_3"],
                                PvConsumerDataDict[Guldenen + "_4"], PvConsumerDataDict[Guldenen + "_5"], PvConsumerDataDict[Guldenen + "_6"],
                                PvConsumerDataDict[Guldenen + "_7"], PvConsumerDataDict[Guldenen + "_8"], PvConsumerDataDict[Guldenen + "_9"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Guldenen].MeteoId]
                ),

                [Kleiner] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Kleiner],
                    inverters: [PvInverterDataDict[Kleiner + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Kleiner + "_1", [PvRoofDataDict[Kleiner + "_1"], PvRoofDataDict[Kleiner + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[Kleiner + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Kleiner].MeteoId]
                ),

                [Liuns] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Liuns],
                    inverters: [PvInverterDataDict[Liuns + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Liuns + "_1", [PvRoofDataDict[Liuns + "_1"], PvRoofDataDict[Liuns + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[Liuns + "_1"], PvConsumerDataDict[Liuns + "_2"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Liuns].MeteoId]
                ),

                [Lotz] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Lotz],
                    inverters: [PvInverterDataDict[Lotz + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Lotz + "_1", [PvRoofDataDict[Lotz + "_1"]] }
                    },
                    consumers: [PvConsumerDataDict[Lotz + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Lotz].MeteoId]
                ),

                [Senn] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Senn],
                    inverters: [PvInverterDataDict[Senn + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Senn + "_1", [PvRoofDataDict[Senn + "_1"], PvRoofDataDict[Senn + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[Senn + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Senn].MeteoId]
                ),

                [SennV] = new PvSiteModel(
                    pvSite: PvSiteDataDict[SennV],
                    inverters: [PvInverterDataDict[SennV + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { SennV + "_1", [PvRoofDataDict[SennV + "_1"], PvRoofDataDict[SennV + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[SennV + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[SennV].MeteoId]
                ),

                [TestSite] = new PvSiteModel(
                    pvSite: PvSiteDataDict[TestSite],
                    inverters: [PvInverterDataDict[TestSite + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { TestSite + "_1", [PvRoofDataDict[TestSite + "_1"], PvRoofDataDict[TestSite + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[TestSite + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[TestSite].MeteoId]
                ),

                [Tof] = new PvSiteModel(
                    pvSite: PvSiteDataDict[Tof],
                    inverters: [PvInverterDataDict[Tof + "_1"]],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        { Tof + "_1", [PvRoofDataDict[Tof + "_1"], PvRoofDataDict[Tof + "_2"]] }
                    },
                    consumers: [PvConsumerDataDict[Tof + "_1"]],
                    meteoProfile: MeteoProfiles.MeteoDict[PvSiteDataDict[Tof].MeteoId]
                ),

                ["Manual"] = new PvSiteModel(
                    pvSite: new PvSite(
                        SystemName: "TestSite",
                        EgId: "",
                        Status: "Test",
                        StreetName: "TestAddress",
                        HouseNumber: "",
                        ZipNumber: "TestZIP",
                        Town: "TestTown",
                        Lon: 10.0,
                        Lat: 50.0,
                        UtcShift: -1,
                        MeteoId: "TestMeteoId",
                        IndicativeNrOfInverters: 1,
                        IndicativeNrOfRoofs: 2,
                        IndicativeNrOfConsumers: 0
                    ),
                    inverters:
                    [
                        new Inverter(
                            SystemName: "TestInverter",
                            Site: "TestSite",
                            HasBattery: false,
                            Capacity: 0.0,
                            MaxLoad: 0.0,
                            MaxDrain: 0.0,
                            IndicativeNrOfRoofs: 2
                        )
                    ],
                    roofsPerInverter: new Dictionary<string, PvRoof[]>
                    {
                        {
                            "TestInverter", [
                                new PvRoof(
                                    SystemName: "TestRoof1",
                                    EgrId: "",
                                    Inverter: "TestInverter",
                                    Azi: -80.0,
                                    Elev: 25.0,
                                    Elev2: 0.0,
                                    Area: 100.0,
                                    Peak: 20.0),
                                new PvRoof(
                                    SystemName: "TestRoof2",
                                    EgrId: "",
                                    Inverter: "TestInverter",
                                    Azi: 100.0,
                                    Elev: 25.0,
                                    Elev2: 0.0,
                                    Area: 100.0,
                                    Peak: 20.0)
                            ]
                        }
                    },
                    consumers:
                    [
                        new Consumer(
                            SystemName: "TestConsumer",
                            SiteId: "TestSite",
                            Label: "TestConsumer",
                            AnnualEnergy: 0.0,
                            PeakPower: 0.0,
                            AnnualProfileId: "TestAnnualProfile",
                            WeeklyProfileId: "TestWeeklyProfile",
                            DailyProfileId: "TestDailyProfile",
                            HourlyProfileId: "TestHourlyProfile"
                        )
                    ],
                    meteoProfile: MeteoProfiles.MeteoDict["TestProfile"]
                ),
            };
    }
}