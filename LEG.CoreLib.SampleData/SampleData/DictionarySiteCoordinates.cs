using static LEG.CoreLib.SampleData.SampleData.ListSites;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.Common.Utils;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal static class DictionarySiteCoordinates
    {
        internal static readonly Dictionary<string, SiteLocation> SiteLatLonElevDict =
            new(StringComparer.OrdinalIgnoreCase)
        {
            [Bagnera] = new SiteLocation(new Dms(46, 47, 51.9), new Dms(10, 18, 11.9), 1237),
            [Bos_cha] = new SiteLocation(new Dms(46, 46, 36.2), new Dms(10, 10, 7.6), 1669),
            [Clozza] = new SiteLocation(new Dms(46, 47, 57.6), new Dms(10, 18, 18.2), 1242),
            [Guldenen] = new SiteLocation(new Dms(47, 19, 22.2), new Dms(8, 39, 19.8), 696),
            [Ftan] = new SiteLocation(new Dms(46, 47, 48.0), new Dms(10, 15, 21.9), 1672),
            [Fuorcla] = new SiteLocation(new Dms(46, 25, 45.1), new Dms(9, 50, 22.5), 2760),
            [Liuns] = new SiteLocation(new Dms(46, 47, 56.7), new Dms(10, 17, 40.8), 1324),
            [Lotz] = new SiteLocation(new Dms(47, 19, 20.8), new Dms(8, 39, 7.2), 685),
            [Senn] = new SiteLocation(new Dms(47, 20, 19.3), new Dms(8, 39, 49.5), 527),
            [Tof] = new SiteLocation(new Dms(46, 47, 58.3), new Dms(10, 17, 39.2), 1337),
        };

    }
}
