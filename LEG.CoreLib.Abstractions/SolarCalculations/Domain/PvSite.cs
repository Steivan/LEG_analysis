using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record PvSite(
    [property: Key]string SystemName,
    string Status,
    string StreetName,
    string HouseNumber,
    string ZipNumber,
    string Town,
    string EgId,
    double Lon,
    double Lat,
    int UtcShift,
    string MeteoId,
    int IndicativeNrOfInverters,
    int IndicativeNrOfRoofs,
    int IndicativeNrOfConsumers
);
