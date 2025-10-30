using static LEG.CoreLib.SampleData.SampleData.ListSites;
using static LEG.CoreLib.SampleData.SampleData.ListConsumerProfiles;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal class ConsumerProfilesWeekly
    {
        internal static readonly Dictionary<string, ProfileWeeklyRecord> WeeklyProfilesDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [WeeklyDefault] = new ProfileWeeklyRecord(
                    WeeklyDefault, "System",
                    80, 80, 80, 80, 80, 100, 100
                ),

                [WeeklyFlat] = new ProfileWeeklyRecord(
                    WeeklyFlat, "System",
                    100, 100, 100, 100, 100, 100, 100
                ),
                [WeeklyWorkDays] = new ProfileWeeklyRecord(
                    WeeklyWorkDays, "System",
                    100, 100, 100, 100, 100, 0, 0
                ),
                [WeeklyWeekEnd] = new ProfileWeeklyRecord(
                    WeeklyWeekEnd, "System",
                    0, 0, 0, 0, 0, 100, 100
                ),

                [WeeklyResidential] = new ProfileWeeklyRecord(
                    WeeklyResidential, "System",
                    75, 75, 75, 75, 75, 100, 100
                ),
                [WeeklyCommercial] = new ProfileWeeklyRecord(
                    WeeklyCommercial, "System",
                    100, 100, 100, 100, 100, 40, 40
                ),
                [WeeklyPublic] = new ProfileWeeklyRecord(
                    WeeklyPublic, "System",
                    100, 100, 100, 100, 100, 20, 20
                ),

                [WeeklyTest] = new ProfileWeeklyRecord(
                    WeeklyTest, TestSite,
                    40, 50, 60, 70, 80, 90, 100
                ),
            };
    }
}

// CSVImport: Define weekly profile dictionary
// WeeklyHeader = "SystemName, Owner, Mo, Tu, We, Th, Fr, Sa, Su";
// SystemName	Owner   Mo	Tu	We	Th	Fr	Sa	Su
// w_flat	    none    24	24	24	24	24	24	24
