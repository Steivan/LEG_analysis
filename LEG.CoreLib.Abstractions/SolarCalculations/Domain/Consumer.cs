using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record Consumer(
    [property: Key] string SystemName,
    string SiteId,
    string Label,
    double AnnualEnergy,
    double PeakPower,
    string AnnualProfileId,
    string WeeklyProfileId,
    string DailyProfileId,
    string HourlyProfileId
);
