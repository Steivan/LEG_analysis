using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record ProfileAnnualRecord(
    [property: Key] string SystemName,
    string Owner,
    double Na,
    double Jan,
    double Feb,
    double Mar,
    double Apr,
    double Mai,
    double Jun,
    double Jul,
    double Aug,
    double Sep,
    double Okt,
    double Nov,
    double Dez
)
{
    public double GetSum() => ToArray().Sum();
    public double[] ToArray() => [Jan, Feb, Mar, Apr, Mai, Jun, Jul, Aug, Sep, Okt, Nov, Dez];
    public double[] ToNormalizedArray() => [..ToArray().Select(x => x / GetSum())];
};


// CSVImport: Define annual profile dictionary
// AnnualHeader = "SystemName, Ownwer, na, Jan, Feb, Mar, Apr, Mai, Jun, Jul, Aug, Sep, Okt, Nov, Dez";
// SystemName	Owner   na      Jan     Feb 	Mar 	Apr 	Mai 	Jun 	Jul 	Aug 	Sep 	Okt 	Nov 	Dez 
// a_flat	    none    0	    31	    28	    31	    30	    31	    30	    31	    31	    30	    31	    30	    31


