using static LEG.CoreLib.SampleData.SampleData.ListSites;
using static LEG.CoreLib.SampleData.SampleData.ListConsumerProfiles;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal class ConsumerProfilesDaily
    {
        internal static readonly Dictionary<string, ProfileDailyRecord> DailyProfilesDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [DailyDefault] = new ProfileDailyRecord(
                    DailyDefault, "System",
                    50, 50, 50, 50, 50, 50, 75, 100, 100, 100, 100, 100,
                    100, 100, 100, 100, 100, 100, 75, 50, 50, 50, 50, 50
                ),

                [DailyFlat] = new ProfileDailyRecord(
                    DailyFlat, "System",
                    100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100,
                    100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100
                ),
                [DailyDayTime] = new ProfileDailyRecord(
                    DailyDayTime, "System",
                    0, 0, 0, 0, 0, 0, 50, 100, 100, 100, 100, 100,
                    100, 100, 100, 100, 100, 100, 50, 0, 0, 0, 0, 0
                ),
                [DailyNightTime] = new ProfileDailyRecord(
                    DailyNightTime, "System",
                    100, 100, 100, 100, 100, 100, 50, 0, 0, 0, 0, 0, 
                    0, 0, 0, 0, 0, 0, 50, 100, 100, 100, 100, 100
                ),

                [DailyResidential] = new ProfileDailyRecord(
                    DailyResidential, "System",
                    20, 20, 20, 20, 20, 30, 50, 100, 75, 50, 50, 100, 
                    100, 50, 50, 50, 75, 100, 100, 100, 80, 80, 80, 50
                ),
                [DailyCommercial] = new ProfileDailyRecord(
                    DailyCommercial, "System",
                    10, 10, 10, 10, 10, 10, 20, 50, 100, 100, 100, 100,
                    50, 50, 100, 100, 100, 50, 20, 20, 10, 10, 10, 10
                ),
                [DailyPublic] = new ProfileDailyRecord(
                    DailyPublic, "System",
                    10, 10, 10, 10, 10, 10, 10, 20, 80, 100, 100, 80,
                    50, 50, 80, 100, 100, 50, 10, 10, 10, 10, 10, 10
                ),

                [DailyTest] = new ProfileDailyRecord(
                    DailyTest, TestSite,
                    10, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 100,
                    100, 100, 90, 80, 70, 60, 50, 40, 30, 20, 10, 10
                    ),
            };
    }
}

// CSVImport: Define daily profile dictionary
// DailyHeader = "SystemName, Owner, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23";
// SystemName	Owner   0	1	2	3	4	5	6	7	8	9	10	11	12	13	14	15	16	17	18	19	20	21	22	23
// d_flat	    none    1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1
