using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record PvRoof(
    [property: Key] string SystemName,
    string EgrId,
    string Inverter,  // ID of parent inverter
    double Azi,       // Orientation of roof in [deg] deviation from S ('+'=W, '-'=E)
    double Elev,      // Elevation of roof in [deg] (0°=flat, 90°=vertical)
    double Elev2,     // 2nd elevation -> currently not used
    double Area,      // Area of roof in [m^2]
    double Peak       // Installed power in [kWp]
);
