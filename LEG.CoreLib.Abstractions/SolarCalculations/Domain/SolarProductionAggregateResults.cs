
namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record SolarProductionAggregateResults(
    string SiteId,
    string Town,
    int EvaluationYear,
    int UtcShift,
    int DimensionRoofs,
    double[] PeakPowerPerRoof,                             // 1 + DimensionRoofs 
    double[,,] TheoreticalAggregation,                    // [1 + DimensionRoofs, 1 + NrMonths, 1 + NrHoursPerDay]
    double[,,] EffectiveAggregation,
    int[] CountPerMonth,
    List<double[]> TheoreticalMonth,
    List<double[]> EffectiveMonth,
    List<double> TheoreticalYear,
    List<double> EffectiveYear
);
