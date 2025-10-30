using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record ProfileDailyRecord(
    [property: Key] string SystemName,
    string Owner,
    double H00,
    double H01,
    double H02,
    double H03,
    double H04,
    double H05,
    double H06,
    double H07,
    double H08,
    double H09,
    double H10,
    double H11,
    double H12,
    double H13,
    double H14,
    double H15,
    double H16,
    double H17,
    double H18,
    double H19,
    double H20,
    double H21,
    double H22,
    double H23
)
{
    public double GetSum() => ToArray().Sum();
    public double[] ToArray() => [ H00, H01, H02, H03, H04, H05, H06, H07, H08, H09, H10, H11, H12, H13, H14, H15, H16, H17, H18, H19, H20, H21, H22, H23 ];
    public double[] ToNormalizedArray() => [..ToArray().Select(x => x / GetSum())];
};

// CSVImport: Define daily profile dictionary
// DailyHeader = "SystemName, Owner, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23";
// SystemName	Owner   0	1	2	3	4	5	6	7	8	9	10	11	12	13	14	15	16	17	18	19	20	21	22	23
// d_flat	    none    1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1

