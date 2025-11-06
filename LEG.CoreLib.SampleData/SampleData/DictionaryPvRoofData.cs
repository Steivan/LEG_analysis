using static LEG.CoreLib.SampleData.SampleData.ListSites;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal class DictionaryPvRoofData
    {
        internal static readonly Dictionary<string, PvRoof> PvRoofDataDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Bagnera + "_1"] = new PvRoof(
                    SystemName: Bagnera + "_1", 
                    EgrId: "",
                    Inverter: Bagnera + "_1",
                    Azi: 63.5,
                    Elev: 34.0,
                    Elev2: 0.0,
                    Area: 45.0,
                    Peak: 9.0
                    ),
                [Bagnera + "_2"] = new PvRoof(
                    SystemName: Bagnera + "_2",
                    EgrId: "",
                    Inverter: Bagnera + "_1",
                    Azi: 243.5,
                    Elev: 33.0,
                    Elev2: 0.0,
                    Area: 45.0,
                    Peak: 9.0
                ),

                [Bos_cha + "_1"] = new PvRoof(
                    SystemName: Bos_cha + "_1",
                    EgrId: "",
                    Inverter: Bos_cha + "_1",
                    Azi: -62,
                    Elev: 36.0,
                    Elev2: 0.0,
                    Area: 200.0,
                    Peak: 27.0
                ),
                [Bos_cha + "_2"] = new PvRoof(
                    SystemName: Bos_cha + "_2",
                    EgrId: "",
                    Inverter: Bos_cha + "_1",
                    Azi: 118.0,
                    Elev: 36.0,
                    Elev2: 0.0,
                    Area: 200.0,
                    Peak: 26.0
                ),

                [Clozza + "_1"] = new PvRoof(
                    SystemName: Clozza + "_1",
                    EgrId: "",
                    Inverter: Clozza + "_1",
                    Azi: -45.0,
                    Elev: 30.0,
                    Elev2: 0.0,
                    Area: 50.0,
                    Peak: 10.0
                ),
                [Clozza + "_2"] = new PvRoof(
                    SystemName: Clozza + "_2",
                    EgrId: "",
                    Inverter: Clozza + "_2",
                    Azi: -45.0,
                    Elev: 45.0,
                    Elev2: 0.0,
                    Area: 50.0,
                    Peak: 10.0
                ),
                [Clozza + "_3"] = new PvRoof(
                    SystemName: Clozza + "_3",
                    EgrId: "",
                    Inverter: Clozza + "_3",
                    Azi: -45.0,
                    Elev: 90.0,
                    Elev2: 0.0,
                    Area: 50.0,
                    Peak: 8.0
                ),

                [Ftan + "_1"] = new PvRoof(
                    SystemName: Ftan + "_1",
                    EgrId: "",
                    Inverter: Ftan + "_1",
                    Azi: -90.0,
                    Elev: 20.0,
                    Elev2: 0.0,
                    Area: 55.0,
                    Peak: 11.0
                ),
                [Ftan + "_2"] = new PvRoof(
                    SystemName: Ftan + "_2",
                    EgrId: "",
                    Inverter: Ftan + "_2",
                    Azi: 90.0,
                    Elev: 20.0,
                    Elev2: 0.0,
                    Area: 55.0,
                    Peak: 11.0
                ),

                [Fuorcla + "_1"] = new PvRoof(
                    SystemName: Fuorcla + "_1",
                    EgrId: "",
                    Inverter: Fuorcla + "_1",
                    Azi: -20.0,
                    Elev: 60.0,
                    Elev2: 0.0,
                    Area: 10.0,
                    Peak: 2.0
                ),

                [Guldenen + "_1"] = new PvRoof(
                    SystemName: Guldenen + "_1",
                    EgrId: "",
                    Inverter: Guldenen + "_1",
                    Azi: -94.0,
                    Elev: 10.0,
                    Elev2: 0.0,
                    Area: 31.0,
                    Peak: 6.0
                ),
                [Guldenen + "_2"] = new PvRoof(
                    SystemName: Guldenen + "_2",
                    EgrId: "",
                    Inverter: Guldenen + "_2",
                    Azi: 86.0,
                    Elev: 20.0,
                    Elev2: 0.0,
                    Area: 31.0,
                    Peak: 6.0
                ),

                [Kleiner + "_1"] = new PvRoof(
                    SystemName: Kleiner + "_1",
                    EgrId: "",
                    Inverter: Kleiner + "_1",
                    Azi: 56.0,
                    Elev: 42.0,
                    Elev2: 0.0,
                    Area: 130.0,
                    Peak: 10.0
                ),
                [Kleiner + "_2"] = new PvRoof(
                    SystemName: Kleiner + "_2",
                    EgrId: "",
                    Inverter: Kleiner + "_2",
                    Azi: -124.0,
                    Elev: 41.0,
                    Elev2: 0.0,
                    Area: 130.0,
                    Peak: 10.0
                ),

                [Liuns + "_1"] = new PvRoof(
                    SystemName: Liuns + "_1",
                    EgrId: "",
                    Inverter: Liuns + "_1",
                    Azi: -36.8,
                    Elev: 20.0,
                    Elev2: 0.0,
                    Area: 57.0,
                    Peak: 33 * 0.465 // 13.53
                ),
                [Liuns + "_2"] = new PvRoof(
                    SystemName: Liuns + "_2",
                    EgrId: "",
                    Inverter: Liuns + "_2",
                    Azi: 143.2,
                    Elev: 20.0,
                    Elev2: 0.0,
                    Area: 41.0,
                    Peak: 22 * 0.41 // 9.02
                ),

                [Lotz + "_1"] = new PvRoof(
                    SystemName: Lotz + "_1",
                    EgrId: "",
                    Inverter: Lotz + "_1",
                    Azi: 1.2,
                    Elev: 35.0,
                    Elev2: 0.0,
                    Area: 86.36,
                    Peak: 17.34
                ),

                [Senn + "_1"] = new PvRoof(
                    SystemName: Senn + "_1",
                    EgrId: "",
                    Inverter: Senn + "_1",
                    Azi: -21.4,
                    Elev: 35.0,
                    Elev2: 0.0,
                    Area: 25.0,
                    Peak: 5.88
                ),
                [Senn + "_2"] = new PvRoof(
                    SystemName: Senn + "_2",
                    EgrId: "",
                    Inverter: Senn + "_2",
                    Azi: -21.4,
                    Elev: 9.0,
                    Elev2: 0.0,
                    Area: 25.0,
                    Peak: 5.41
                ),

                [SennV + "_1"] = new PvRoof(
                    SystemName: SennV + "_1",
                    EgrId: "",
                    Inverter: SennV + "_1",
                    Azi: -21.4,
                    Elev: 35.0,
                    Elev2: 0.0,
                    Area: 25.0,
                    Peak: 8.67
                ),
                [SennV + "_2"] = new PvRoof(
                    SystemName: SennV + "_2",
                    EgrId: "",
                    Inverter: SennV + "_2",
                    Azi: -21.4,
                    Elev: 9.0,
                    Elev2: 0.0,
                    Area: 25,
                    Peak: 8.02
                ),

                [TestSite + "_1"] = new PvRoof(
                    SystemName: TestSite + "_1",
                    EgrId: "",
                    Inverter: TestSite + "_1",
                    Azi: -80.0,
                    Elev: 25.0,
                    Elev2: 0.0,
                    Area: 100.0,
                    Peak: 20.0
                ),
                [TestSite + "_2"] = new PvRoof(
                    SystemName: TestSite + "_2",
                    EgrId: "",
                    Inverter: TestSite + "_2",
                    Azi: 100.0,
                    Elev: 25.0,
                    Elev2: 0.0,
                    Area: 100.0,
                    Peak: 20.0
                ),

                [Tof + "_1"] = new PvRoof(
                    SystemName: Tof + "_1",
                    EgrId: "",
                    Inverter: Tof + "_1",
                    Azi: -50.0,
                    Elev: 32.0,
                    Elev2: 0.0,
                    Area: 75.0,
                    Peak: 36 * 0.44 // 15.84
                ),
                [Tof + "_2"] = new PvRoof(
                    SystemName: Tof + "_2",
                    EgrId: "",
                    Inverter: Tof + "_2",
                    Azi: 130.0,
                    Elev: 22.0,
                    Elev2: 0.0,
                    Area: 75.0,
                    Peak: 62 * 0.44 // 27.28
                ),
            };
    }
}
