using static LEG.CoreLib.SampleData.SampleData.ListSites;
using static LEG.CoreLib.SampleData.SampleData.ListConsumerProfiles;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal class ConsumerProfilesHourly
    {
        internal static readonly Dictionary<string, ProfileHourlyRecord> HourlyProfilesDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [DailyDefault] = new ProfileHourlyRecord(
                    DailyDefault, "System", 7, 3, 11
                ),
                [DailyFlat] = new ProfileHourlyRecord(
                    DailyFlat, "System", 1, 1, 1
                ),

                [DailyResidential] = new ProfileHourlyRecord(
                    DailyResidential, "System", 5, 2, 10
                ),
                [DailyCommercial] = new ProfileHourlyRecord(
                    DailyCommercial, "System", 8, 4, 12
                ),
                [DailyPublic] = new ProfileHourlyRecord(
                    DailyPublic, "System", 10, 6, 15
                ),

                [DailyTest] = new ProfileHourlyRecord(
                    DailyTest, TestSite, 12, 10, 15
                ),
            };
    }
}

// CSVImport: Define hourly profile dictionary
// HourlyHeader = "SystemName, Owner, avg_hours, min_hours, max_hours";
// SystemName	    Owner   avg_hours   min_hours   max_hours
// h_household	    none    5	        2	        10