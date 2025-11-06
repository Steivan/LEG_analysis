using static LEG.CoreLib.SampleData.SampleData.ListSites;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal class DictionarySiteHorizonControls
    {
        internal static readonly Dictionary<string, (bool getHorizon, double aziStep)> SiteGetHorizonDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Bagnera] = (true, 5),
                [Bos_cha] = (true, 5),
                [Clozza] = (true, 5),
                [Ftan] = (true, 5),
                [Fuorcla] = (true, 5),
                [Guldenen] = (true, 10),
                [Kleiner] = (true, 10),
                [Liuns] = (true, 5),
                [Lotz] = (true, 10),
                [Senn] = (true, 10),
                [SennV] = (false, 10),// not cached => fallback to Senn
                [TestSite] = (false, 30),
                [Tof] = (false, 5), // not cached => fallback to Liuns
            };
    }
}
