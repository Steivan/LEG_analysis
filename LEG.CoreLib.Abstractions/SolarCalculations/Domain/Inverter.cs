using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;
public record Inverter(
    [property: Key] string SystemName,
    string Site,                  // ID of parent site
    bool HasBattery,              // TRUE if battery 
    double Capacity,              // Battery storage capacity in [kWh] 
    double MaxLoad,               // Maximal load capacity in [kW]
    double MaxDrain,              // Maximal drain capacity in [kW]
    int IndicativeNrOfRoofs       // Indicative number of roofs -> actual # taken from roof dictionary
);
