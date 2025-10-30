
namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record SolarProductionAggregate(
    string SiteId,
    string Town,
    int EvaluationYear,
    int UtcShift,
    int DimensionRoofs,
    double[] PeakPowerPerRoof,
    double[,,] TheoreticalAggregation,
    double[,,] EffectiveAggregation,
    int[] CountPerMonth
);