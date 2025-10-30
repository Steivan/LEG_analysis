
namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record SolarProductionDetails(
    string SiteId,
    string Town,
    int EvaluationYear,
    int UtcShift,
    int DimensionRoofs,                                 // DimensionRoofs   
    int ValidRecordsCount,                              // StepsPerHour * 24 * (365 or 366)
    double[] PeakPowerPerRoof,                          // [DimensionRoofs]
    DateTime[] TimeStamps,                              // [StepsPerHour * 24 * 366]
    double[,] TheoreticalIrradiationPerRoofAndInterval,       // [DimensionRoofs, StepsPerHour * 24 * 366]
    double[,] EffectiveIrradiationPerRoofAndInterval,
    int[] CountPerMonth,
    int[] CountPerHour
);
