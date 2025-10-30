using static LEG.CoreLib.SampleData.SampleData.ListSites;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace LEG.CoreLib.SampleData.SampleData
{
    internal class DictionaryPvInverters
    {
        internal static readonly Dictionary<string, Inverter> PvInverterDataDict =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Bagnera + "_1"] = new Inverter(
                    SystemName: Bagnera + "_1",
                    Site: Bagnera,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 2
                ),

                [Bos_cha + "_1"] = new Inverter(
                    SystemName: Bos_cha + "_1",
                    Site: Bos_cha,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 2
                ),

                [Clozza + "_1"] = new Inverter(
                    SystemName: Clozza + "_1",
                    Site: Clozza,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 1
                ),

                [Guldenen + "_1"] = new Inverter(
                    SystemName: Guldenen + "_1",
                    Site: Guldenen,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 1
                ),

                [Ftan + "_1"] = new Inverter(
                    SystemName: Ftan + "_1",
                    Site: Ftan,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 2
                ),

                [Fuorcla + "_1"] = new Inverter(
                    SystemName: Fuorcla + "_1",
                    Site: Fuorcla,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 1
                ),

                [Liuns + "_1"] = new Inverter(
                    SystemName: Liuns + "_1",
                    Site: Liuns,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 2
                ),

                [Lotz + "_1"] = new Inverter(
                    SystemName: Lotz + "_1",
                    Site: Lotz,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 1
                ),

                [Senn + "_1"] = new Inverter(
                    SystemName: Senn + "_1",
                    Site: Senn,
                    HasBattery: true,
                    Capacity: 16.0,
                    MaxLoad: 3.0,
                    MaxDrain: 3.0,
                    IndicativeNrOfRoofs: 2
                ),

                [SennV + "_1"] = new Inverter(
                    SystemName: SennV + "_1",
                    Site: SennV,
                    HasBattery: true,
                    Capacity: 16.0,
                    MaxLoad: 8.0,
                    MaxDrain: 8.0,
                    IndicativeNrOfRoofs: 2
                ),

                [TestSite + "_1"] = new Inverter(
                    SystemName: TestSite + "_1",
                    Site: TestSite,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 2
                ),

                [Tof + "_1"] = new Inverter(
                    SystemName: Tof + "_1",
                    Site: Tof,
                    HasBattery: false,
                    Capacity: 0.0,
                    MaxLoad: 0.0,
                    MaxDrain: 0.0,
                    IndicativeNrOfRoofs: 2
                ),
            };
    }
}
