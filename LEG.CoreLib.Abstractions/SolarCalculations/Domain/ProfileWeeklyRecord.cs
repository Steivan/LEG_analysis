using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record ProfileWeeklyRecord(
    [property: Key] string SystemName,
    string Owner,
    double Mo,
    double Tu,
    double We,
    double Th,
    double Fr,
    double Sa,
    double Su
)
{
    public double GetSum() => ToArray().Sum();
    public double[] ToArray() => [Mo, Tu, We, Th, Fr, Sa, Su];
    public double[] ToNormalizedArray() => [..ToArray().Select(x => x / GetSum())];

}

// CSVImport: Define weekly profile dictionary
// WeeklyHeader = "SystemName, Owner, Mo, Tu, We, Th, Fr, Sa, Su";
// SystemName	Owner   Mo	Tu	We	Th	Fr	Sa	Su
// w_flat	    none    24	24	24	24	24	24	24