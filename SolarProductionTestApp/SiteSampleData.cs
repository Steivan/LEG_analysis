using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using static LEG.CoreLib.SampleData.SampleData.PvSiteModelGetters;

namespace SolarProductionTestApp
{
    public class SiteSampleData
    {
        public static IPvSiteModel GetSiteData(string sampleId)
        {
            return GetSiteDataModel(sampleId);
            //switch (sampleId)
            //{
            //    return GetSiteDataModel(string sampleId)
            //    case Senn:
            //    {
            //        return new PvSiteModel(
            //            PvSite: new PvSite(
            //                SystemName: "TestSite",
            //                Status: "Test",
            //                StreetName: "TestAddress",
            //                ZipNumber: "TestZIP",
            //                Town: "TestTown",
            //                Lon: 10.0,
            //                Lat: 50.0,
            //                UtcShift: -1,
            //                MeteoId: "TestMeteoId",
            //                IndicativeNrOfInverters: 1,
            //                IndicativeNrOfRoofs: 2,
            //                IndicativeNrOfConsumers: 0
            //            ),
            //            Inverters: new List<Inverter>
            //            {
            //                new Inverter(
            //                    SystemName: "TestInverter",
            //                    Site: "TestSite",
            //                    HasBattery: false,
            //                    Capacity: 0.0,
            //                    MaxLoad: 0.0,
            //                    MaxDrain: 0.0,
            //                    IndicativeNrOfRoofs: 2
            //                )
            //            },
            //            RoofsPerInverter: new Dictionary<string, PvRoof[]>
            //            {
            //                {
            //                    "TestInverter", [
            //                        new PvRoof(
            //                            SystemName: "TestRoof1",
            //                            Inverter: "TestInverter",
            //                            Azi: -80.0,
            //                            Elev: 25.0,
            //                            Elev2: 0.0,
            //                            Area: 100.0,
            //                            PeakPowerPerRoof: 20.0),
            //                        new PvRoof(
            //                            SystemName: "TestRoof2",
            //                            Inverter: "TestInverter",
            //                            Azi: 100.0,
            //                            Elev: 25.0,
            //                            Elev2: 0.0,
            //                            Area: 100.0,
            //                            PeakPowerPerRoof: 20.0)
            //                    ]
            //                }
            //            },
            //            Consumers: new List<Consumer>
            //            {
            //                new Consumer(
            //                    SystemName: "TestConsumer",
            //                    SiteId: "TestSite",
            //                    Label: "TestConsumer",
            //                    AnnualEnergy: 0.0,
            //                    PeakPowerPerRoof: 0.0,
            //                    AnnualProfileId: "TestAnnualProfile",
            //                    WeeklyProfileId: "TestWeeklyProfile",
            //                    DailyProfileId: "TestDailyProfile",
            //                    HourlyProfileId: "TestHourlyProfile"
            //                )
            //            },
            //            meteoProfile: new MeteoProfile(
            //                SystemName: "TestMeteoId",
            //                NFourier: 4,
            //                Owner: "TestOwnerMeteo",
            //                Profile: BasicParametersAndConstants.DefaultProfile
            //            )
            //        );
            //    }

            //    default:
            //    {
            //        return new PvSiteModel(
            //            PvSite: new PvSite(
            //                SystemName: "TestSite",
            //                Status: "Test",
            //                StreetName: "TestAddress",
            //                ZipNumber: "TestZIP",
            //                Town: "TestTown",
            //                Lon: 10.0,
            //                Lat: 50.0,
            //                UtcShift: -1,
            //                MeteoId: "TestMeteoId",
            //                IndicativeNrOfInverters: 1,
            //                IndicativeNrOfRoofs: 2,
            //                IndicativeNrOfConsumers: 0
            //            ),
            //            Inverters: new List<Inverter>
            //            {
            //                new Inverter(
            //                    SystemName: "TestInverter",
            //                    Site: "TestSite",
            //                    HasBattery: false,
            //                    Capacity: 0.0,
            //                    MaxLoad: 0.0,
            //                    MaxDrain: 0.0,
            //                    IndicativeNrOfRoofs: 2
            //                )
            //            },
            //            RoofsPerInverter: new Dictionary<string, PvRoof[]>
            //            {
            //                {
            //                    "TestInverter", [
            //                        new PvRoof(
            //                            SystemName: "TestRoof1",
            //                            Inverter: "TestInverter",
            //                            Azi: -80.0,
            //                            Elev: 25.0,
            //                            Elev2: 0.0,
            //                            Area: 100.0,
            //                            PeakPowerPerRoof: 20.0),
            //                        new PvRoof(
            //                            SystemName: "TestRoof2",
            //                            Inverter: "TestInverter",
            //                            Azi: 100.0,
            //                            Elev: 25.0,
            //                            Elev2: 0.0,
            //                            Area: 100.0,
            //                            PeakPowerPerRoof: 20.0)
            //                    ]
            //                }
            //            },
            //            Consumers: new List<Consumer>
            //            {
            //                new Consumer(
            //                    SystemName: "TestConsumer",
            //                    SiteId: "TestSite",
            //                    Label: "TestConsumer",
            //                    AnnualEnergy: 0.0,
            //                    PeakPowerPerRoof: 0.0,
            //                    AnnualProfileId: "TestAnnualProfile",
            //                    WeeklyProfileId: "TestWeeklyProfile",
            //                    DailyProfileId: "TestDailyProfile",
            //                    HourlyProfileId: "TestHourlyProfile"
            //                )
            //            },
            //            meteoProfile: new MeteoProfile(
            //                SystemName: "TestMeteoId",
            //                NFourier: 4,
            //                Owner: "TestOwnerMeteo",
            //                Profile: BasicParametersAndConstants.DefaultProfile
            //            )
            //        );
            //    }
            //}
        }
    }
}