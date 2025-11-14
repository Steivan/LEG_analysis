using static LEG.CoreLib.SampleData.SampleData.ListSites;
using static LEG.CoreLib.SampleData.SampleData.ListConsumerProfiles;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal class ConsumerProfilesAnnual
    {
        internal static readonly Dictionary<string, ProfileAnnualRecord> AnnualProfilesDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [AnnualDefault] = new ProfileAnnualRecord(
                    AnnualDefault, "System", 0.0,
                    31, 28.25, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
                ),

                [AnnualFlat] = new ProfileAnnualRecord(
                    AnnualFlat, "System", 0.0,
                    31, 28.25, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
                ),
                [Annual360] = new ProfileAnnualRecord(
                    Annual360, "System", 0.0,
                    30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30
                ),
                [AnnualSummer] = new ProfileAnnualRecord(
                    AnnualSummer, "System", 0.0,
                    0, 0, 10, 30, 31, 30, 31, 31, 20, 0, 0, 0
                ),
                [AnnualWinter] = new ProfileAnnualRecord(
                    AnnualWinter, "System", 0.0,
                    31, 28.25, 21, 0, 0, 0, 0, 0, 10, 31, 30, 31
                ),

                [AnnualResidential] = new  ProfileAnnualRecord(
                    AnnualResidential, "System", 0.0,
                    31, 28.25, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
                    ),
                [AnnualCommercial] = new ProfileAnnualRecord(
                    AnnualCommercial, "System", 0.0,
                    19, 18, 19, 20, 23, 21, 20, 20, 21, 20, 21, 18
                    ),
                [AnnualPublic] = new ProfileAnnualRecord(
                    AnnualPublic, "System", 0.0,
                    19, 18, 19, 20, 23, 21, 20, 20, 21, 20, 21, 18
                    ),

                [AnnualTest] = new ProfileAnnualRecord(
                    AnnualTest, TestSite, 0.0,
                    30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30
                ),
            };
    }
}

// CSVImport: Define annual profile dictionary
// AnnualHeader = "SystemName, Owner, na, Jan, Feb, Mar, Apr, Mai, Jun, Jul, Aug, Sep, Okt, Nov, Dez";
// SystemName	Owner   na      Jan     Feb 	Mar 	Apr 	Mai 	Jun 	Jul 	Aug 	Sep 	Okt 	Nov 	Dez 
// a_flat	    none    0	    31	    28	    31	    30	    31	    30	    31	    31	    30	    31	    30	    31



