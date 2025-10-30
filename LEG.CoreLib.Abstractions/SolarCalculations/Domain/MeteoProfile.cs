using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record MeteoProfile(
    [property: Key] string SystemName,
    int NFourier,  
    string Owner, 
    double[] Profile
);


//public record MeteoProfileForCalculation(string SystemName, int NFourier, string Owner, string Type, Double[] Profile);
