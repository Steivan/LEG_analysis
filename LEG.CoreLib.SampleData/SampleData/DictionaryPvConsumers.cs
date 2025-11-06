using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using static LEG.CoreLib.SampleData.SampleData.ListSites;
using static LEG.CoreLib.SampleData.SampleData.ListConsumerProfiles;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal class DictionaryPvConsumers
    {
        internal static readonly Dictionary<string, Consumer> PvConsumerDataDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Bagnera + "_1"] =
                    new Consumer(
                        SystemName: Bagnera + "_1",
                        SiteId: Bagnera,
                        Label: Bagnera + "_1",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Bos_cha + "_1"] =
                    new Consumer(
                        SystemName: Bos_cha + "_1",
                        SiteId: Bos_cha,
                        Label: Bos_cha + "_1",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Clozza + "_1"] =
                    new Consumer(
                        SystemName: Clozza + "_1",
                        SiteId: Clozza,
                        Label: Clozza + "_1",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Ftan + "_1"] =
                    new Consumer(
                        SystemName: Ftan + "_1",
                        SiteId: Ftan,
                        Label: Ftan + "_1",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Fuorcla + "_1"] =
                    new Consumer(
                        SystemName: Fuorcla + "_1",
                        SiteId: Fuorcla,
                        Label: Fuorcla + "_1",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Guldenen + "_1"] =
                    new Consumer(
                        SystemName: Guldenen + "_1",
                        SiteId: Guldenen,
                        Label: Guldenen + " G1",
                        AnnualEnergy: 7800.0,
                        PeakPower: 4.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [Guldenen + "_2"] =
                    new Consumer(
                        SystemName: Guldenen + "_2",
                        SiteId: Guldenen,
                        Label: Guldenen + " G2",
                        AnnualEnergy: 4200.0,
                        PeakPower: 4.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [Guldenen + "_3"] =
                    new Consumer(
                        SystemName: Guldenen + "_3",
                        SiteId: Guldenen,
                        Label: Guldenen + " G3",
                        AnnualEnergy: 2800.0,
                        PeakPower: 4.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [Guldenen + "_4"] =
                    new Consumer(
                        SystemName: Guldenen + "_4",
                        SiteId: Guldenen,
                        Label: Guldenen + " G4",
                        AnnualEnergy: 5900.0,
                        PeakPower: 4.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [Guldenen + "_5"] =
                    new Consumer(
                        SystemName: Guldenen + "_5",
                        SiteId: Guldenen,
                        Label: Guldenen + " G5",
                        AnnualEnergy: 4200.0,
                        PeakPower: 4.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [Guldenen + "_6"] =
                    new Consumer(
                        SystemName: Guldenen + "_6",
                        SiteId: Guldenen,
                        Label: Guldenen + " K6",
                        AnnualEnergy: 4700.0,
                        PeakPower: 4.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [Guldenen + "_7"] =
                    new Consumer(
                        SystemName: Guldenen + "_7",
                        SiteId: Guldenen,
                        Label: Guldenen + " K7",
                        AnnualEnergy: 2100.0,
                        PeakPower: 4.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [Guldenen + "_8"] =
                    new Consumer(
                        SystemName: Guldenen + "_A",
                        SiteId: Guldenen,
                        Label: Guldenen + " AG",
                        AnnualEnergy: 11500.0,
                        PeakPower: 10.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [Guldenen + "_9"] =
                    new Consumer(
                        SystemName: Guldenen + "_9",
                        SiteId: Guldenen,
                        Label: Guldenen + " WP",
                        AnnualEnergy: 25400.0,
                        PeakPower: 12.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Kleiner + "_1"] =
                    new Consumer(
                        SystemName: Kleiner + "_1",
                        SiteId: Kleiner,
                        Label: Kleiner + " HH",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Liuns + "_1"] =
                    new Consumer(
                        SystemName: Liuns + "_1",
                        SiteId: Liuns,
                        Label: Liuns + " HH",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [Liuns + "_2"] =
                    new Consumer(
                        SystemName: Liuns + "_2",
                        SiteId: Liuns,
                        Label: Liuns + " WP",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Lotz + "_1"] =
                    new Consumer(
                        SystemName: Lotz + "_1",
                        SiteId: Lotz,
                        Label: Lotz + "_1",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Senn + "_1"] =
                    new Consumer(
                        SystemName: Senn + "_1",
                        SiteId: Senn,
                        Label: Senn + "_1",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [SennV + "_1"] =
                    new Consumer(
                        SystemName: SennV + "_1",
                        SiteId: Senn,
                        Label: SennV + " HH",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
                [SennV + "_2"] =
                    new Consumer(
                        SystemName: SennV + "_2",
                        SiteId: Senn,
                        Label: SennV + " WB",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [TestSite + "_1"] =
                    new Consumer(
                        SystemName: TestSite + "_1",
                        SiteId: TestSite,
                        Label: TestSite + "_1",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),

                [Tof + "_1"] =
                    new Consumer(
                        SystemName: Tof + "_1",
                        SiteId: Tof,
                        Label: Tof + "_1",
                        AnnualEnergy: 10000.0,
                        PeakPower: 5.0,
                        AnnualProfileId: AnnualResidential,
                        WeeklyProfileId: WeeklyResidential,
                        DailyProfileId: DailyResidential,
                        HourlyProfileId: HourlyResidential
                    ),
            };
    }
}

