using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;
public record ProfileHourlyRecord(
    [property: Key] string SystemName,
    string Owner,
    double Average,
    double Minimum,
    double Maximum
);


// CSVImport: Define hourly profile dictionary
// HourlyHeader = "SystemName, Owner, avg_hours, min_hours, max_hours";
// SystemName	    Owner   avg_hours   min_hours   max_hours
// h_household	    none    5	        2	        10