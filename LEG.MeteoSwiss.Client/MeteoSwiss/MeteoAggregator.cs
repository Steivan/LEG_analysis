using LEG.Common;
using LEG.MeteoSwiss.Abstractions;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;

namespace LEG.MeteoSwiss.Client.MeteoSwiss
{
    public static class MeteoAggregator
    {
        public static void RunMeteoAggregationForPeriod(string stationId, StationMetaInfo stationMetaInfo, int periodStartYear, int periodEndYear, string granularity = "t", bool isTower = false)
        {
            // Helper to compute average, returns null if no valid data
            double? SafeAverage(IEnumerable<double?> values)
            {
#pragma warning disable CS8629
                var valid = values.Where(v => v.HasValue && !double.IsNaN(v.Value)).Select(v => v.Value!).ToList();
#pragma warning restore CS8629
                return valid.Count > 0 ? valid.Average() : (double?)null;
            }

            double? SafeMax(IEnumerable<double?> values)
            {
#pragma warning disable CS8629
                var valid = values.Where(v => v.HasValue && !double.IsNaN(v.Value)).Select(v => v.Value!).ToList();
#pragma warning restore CS8629
                return valid.Count > 0 ? valid.Max() : (double?)null;
            }

            string FormatValue(object? value, int width)
            {
                if (value == null)
                    return "n/a".PadLeft(width);

                if (value is double d)
                    return d.ToString("0.00").PadLeft(width);

                if (value is double v)
                    return v.ToString("0.00").PadLeft(width);

                if (value is double?)
                {
                    var nullable = (double?)value;
                    return nullable.HasValue
                        ? nullable.Value.ToString("0.00").PadLeft(width)
                        : "n/a".PadLeft(width);
                }

                return "n/a".PadLeft(width);
            }

            var allRecords = new List<WeatherCsvRecord>();

            // Determine all relevant decades
            var startDecade = (periodStartYear / 10) * 10;
            var endDecade = (periodEndYear / 10) * 10;
            var sreConversionFactor = granularity switch
            {
                // 10 minutes
                "t" => 2.4,
                // 1 hour
                "h" => 24.0,
                // 1 day
                "d" => 1.0,
                _ => 1.0,
            };
            for (var decade = startDecade; decade <= endDecade; decade += 10)
            {
                var period = $"{decade}-{decade + 9}";
                period = MeteoSwissHelper.NormalizeAndValidatePeriod(period);
                var filePath = "";
                if (isTower)
                {
                    (_, filePath) = MeteoSwissHelper.GetTowerCsvFilename(stationId, period, granularity: granularity);
                }
                else
                {
                    (_, filePath) = MeteoSwissHelper.GetGroundCsvFilename(stationId, period, granularity: granularity);
                }

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Warning: File not found: {filePath}");
                    continue;
                }

                var records = ImportCsv.ImportFromFile<WeatherCsvRecord>(filePath, ";");
                allRecords.AddRange(records);
            }

            // Filter records for the period of interest
            var filteredRecords = allRecords
                .Where(r => r.ReferenceTimestamp.Year >= periodStartYear && r.ReferenceTimestamp.Year <= periodEndYear)
                .ToList();

            if (filteredRecords.Count == 0)
            {
                Console.WriteLine("No records found for the specified period.");
                return;
            }

            // --- Monthly averages ---
            var monthlyAverages = filteredRecords
                .GroupBy(r => new { r.ReferenceTimestamp.Year, r.ReferenceTimestamp.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Ta1Tows0 = SafeAverage(g.Select(r => (double?)r.Ta1Tows0)),
                    TdeTows0 = SafeAverage(g.Select(r => (double?)r.TdeTows0)),
                    UreTows0 = SafeAverage(g.Select(r => (double?)r.UreTows0)),
                    FklTowz1 = SafeAverage(g.Select(r => (double?)r.FklTowz1)),
                    Fk1Towz0 = SafeAverage(g.Select(r => (double?)r.Fk1Towz0)),
                    Dv1Towz0 = SafeAverage(g.Select(r => (double?)r.Dv1Towz0)),
                    Fu3Towz0 = SafeMax(g.Select(r => (double?)r.Fu3Towz0)),
                    Fu3Towz1 = SafeMax(g.Select(r => (double?)r.Fu3Towz1)),
                    Gre000z0 = SafeAverage(g.Select(r => (double?)r.Gre000z0)),
                    Sre000z0 = SafeAverage(g.Select(r => (double?)r.Sre000z0)) is double sre1
                        ? sre1 * sreConversionFactor
                        : (double?)null
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            // --- Annual averages ---
            var annualAverages = filteredRecords
                .GroupBy(r => r.ReferenceTimestamp.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Ta1Tows0 = SafeAverage(g.Select(r => (double?)r.Ta1Tows0)),
                    TdeTows0 = SafeAverage(g.Select(r => (double?)r.TdeTows0)),
                    UreTows0 = SafeAverage(g.Select(r => (double?)r.UreTows0)),
                    FklTowz1 = SafeAverage(g.Select(r => (double?)r.FklTowz1)),
                    Fk1Towz0 = SafeAverage(g.Select(r => (double?)r.Fk1Towz0)),
                    Dv1Towz0 = SafeAverage(g.Select(r => (double?)r.Dv1Towz0)),
                    Fu3Towz0 = SafeMax(g.Select(r => (double?)r.Fu3Towz0)),
                    Fu3Towz1 = SafeMax(g.Select(r => (double?)r.Fu3Towz1)),
                    Gre000z0 = SafeAverage(g.Select(r => (double?)r.Gre000z0)),
                    Sre000z0 = SafeAverage(g.Select(r => (double?)r.Sre000z0)) is double sre2
                        ? sre2 * sreConversionFactor
                        : (double?)null
                })
                .OrderBy(x => x.Year)
                .ToList();

            // --- Overall averages ---
            var overallAverage = new
            {
                Ta1Tows0 = SafeAverage(filteredRecords.Select(r => (double?)r.Ta1Tows0)),
                TdeTows0 = SafeAverage(filteredRecords.Select(r => (double?)r.TdeTows0)),
                UreTows0 = SafeAverage(filteredRecords.Select(r => (double?)r.UreTows0)),
                FklTowz1 = SafeAverage(filteredRecords.Select(r => (double?)r.FklTowz1)),
                Fk1Towz0 = SafeAverage(filteredRecords.Select(r => (double?)r.Fk1Towz0)),
                Dv1Towz0 = SafeAverage(filteredRecords.Select(r => (double?)r.Dv1Towz0)),
                Fu3Towz0 = SafeMax(filteredRecords.Select(r => (double?)r.Fu3Towz0)),
                Fu3Towz1 = SafeMax(filteredRecords.Select(r => (double?)r.Fu3Towz1)),
                Gre000z0 = SafeAverage(filteredRecords.Select(r => (double?)r.Gre000z0)),
                Sre000z0 = SafeAverage(filteredRecords.Select(r => (double?)r.Sre000z0)) is double sre3
                    ? sre3 * sreConversionFactor
                    : (double?)null
            };

            // --- Monthly averages by calendar month (across all years) ---
            var monthNames = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;
            var monthlyAveragesByMonth = filteredRecords
                .GroupBy(r => r.ReferenceTimestamp.Month)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Month = g.Key,
                    Ta1Tows0 = SafeAverage(g.Select(r => (double?)r.Ta1Tows0)),
                    TdeTows0 = SafeAverage(g.Select(r => (double?)r.TdeTows0)),
                    UreTows0 = SafeAverage(g.Select(r => (double?)r.UreTows0)),
                    FklTowz1 = SafeAverage(g.Select(r => (double?)r.FklTowz1)),
                    Fk1Towz0 = SafeAverage(g.Select(r => (double?)r.Fk1Towz0)),
                    Dv1Towz0 = SafeAverage(g.Select(r => (double?)r.Dv1Towz0)),
                    Fu3Towz0 = SafeMax(g.Select(r => (double?)r.Fu3Towz0)),
                    Fu3Towz1 = SafeMax(g.Select(r => (double?)r.Fu3Towz1)),
                    Gre000z0 = SafeAverage(g.Select(r => (double?)r.Gre000z0)),
                    Sre000z0 = SafeAverage(g.Select(r => (double?)r.Sre000z0)) is double sre4
                        ? sre4 * sreConversionFactor
                        : (double?)null
                })
                .ToList();

            // Prepare field codes, labels, and units
            var fieldMeta = new[]
            {
                new { Code = "Ta1Tows0", Label = "Air Temperature", Unit = "[°C]" },
                new { Code = "TdeTows0", Label = "Dew Point", Unit = "[°C]" },
                new { Code = "UreTows0", Label = "Rel. Humidity", Unit = "[%]" },
                new { Code = "FklTowz1", Label = "Wind Speed 10m", Unit = "[km/h]" },
                new { Code = "Fk1Towz0", Label = "Wind Speed 2m", Unit = "[km/h]" },
                new { Code = "Dv1Towz0", Label = "Wind Direction", Unit = "[°]" },
                new { Code = "Fu3Towz0", Label = "Gust Speed 2m", Unit = "[km/h]" },
                new { Code = "Fu3Towz1", Label = "Gust Speed 10m", Unit = "[km/h]" },
                new { Code = "Gre000z0", Label = "Global Radiation", Unit = "[W/m²]" },
                new { Code = "Sre000z0", Label = "Sunshine Duration", Unit = "[h]" }
            };

            var colWidths = fieldMeta.Select(f => Math.Max(f.Label.Length, 10)).ToArray();
            const int dateColWidth = 10;

            // Print station info
            Console.WriteLine($"Station ID = {stationId} / Period = {periodStartYear} - {periodEndYear}");
            var (fullName, locationNotes) = StationMetaImporter.StationInfo(stationMetaInfo);
            Console.WriteLine($"{fullName}, {locationNotes}");
            Console.WriteLine();

            // Write monthly averages as a table
            Console.WriteLine("Monthly Averages:");

            // Header row: labels
            Console.Write($"{"EvaluationYear-Mo",dateColWidth} ");
            for (int i = 0; i < fieldMeta.Length; i++)
                Console.Write($"| {fieldMeta[i].Label.PadRight(colWidths[i])} ");
            Console.WriteLine();

            // Header row: units
            Console.Write(new string(' ', dateColWidth) + " ");
            for (int i = 0; i < fieldMeta.Length; i++)
                Console.Write($"| {fieldMeta[i].Unit.PadRight(colWidths[i])} ");
            Console.WriteLine();

            // Separator
            Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => f.Label.Length + 3 + 1)));

            // Data rows with year separator
            int? lastYear = null;
            foreach (var avg in monthlyAverages)
            {
                if (lastYear != null && avg.Year != lastYear)
                {
                    Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => f.Label.Length + 3 + 1)));
                }

                lastYear = avg.Year;

                Console.Write($"{avg.Year}-{avg.Month:00}".PadLeft(dateColWidth) + " ");
                for (int i = 0; i < fieldMeta.Length; i++)
                {
                    var code = fieldMeta[i].Code;
                    var width = colWidths[i];
                    var prop = avg.GetType().GetProperty(code);
                    var value = prop?.GetValue(avg, null);
                    string formatted = FormatValue(value, width);
                    Console.Write($"| {formatted} ");
                }

                Console.WriteLine();
            }

            // Print separator line before printing annual averages
            Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => f.Label.Length + 3 + 1)));

            // Data rows
            foreach (var avg in annualAverages)
            {
                Console.Write($"{avg.Year}".PadLeft(dateColWidth) + " ");
                for (int i = 0; i < fieldMeta.Length; i++)
                {
                    var code = fieldMeta[i].Code;
                    var width = colWidths[i];
                    var prop = avg.GetType().GetProperty(code);
                    var value = prop?.GetValue(avg, null);
                    string formatted = FormatValue(value, width);
                    Console.Write($"| {formatted} ");
                }

                Console.WriteLine();
            }

            // Print separator line before overall average
            Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => f.Label.Length + 3 + 1)));

            // Print overall average row
            Console.Write($"{"Overall",dateColWidth} ");
            for (int i = 0; i < fieldMeta.Length; i++)
            {
                var code = fieldMeta[i].Code;
                var width = colWidths[i];
                var prop = overallAverage.GetType().GetProperty(code);
                var value = prop?.GetValue(overallAverage, null);
                string formatted = FormatValue(value, width);
                Console.Write($"| {formatted} ");
            }

            Console.WriteLine();

            // Print monthly averages by month table
            Console.WriteLine();
            Console.WriteLine("Monthly Averages by Calendar Month (across all years):");

            // Header row: labels
            Console.Write($"{"Month",dateColWidth} ");
            for (int i = 0; i < fieldMeta.Length; i++)
                Console.Write($"| {fieldMeta[i].Label.PadRight(colWidths[i])} ");
            Console.WriteLine();

            // Header row: units
            Console.Write(new string(' ', dateColWidth) + " ");
            for (int i = 0; i < fieldMeta.Length; i++)
                Console.Write($"| {fieldMeta[i].Unit.PadRight(colWidths[i])} ");
            Console.WriteLine();

            // Separator
            Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => f.Label.Length + 3 + 1)));

            // Data rows
            foreach (var avg in monthlyAveragesByMonth)
            {
                string monthLabel = monthNames[avg.Month - 1];
                Console.Write($"{monthLabel,dateColWidth} ");
                for (int i = 0; i < fieldMeta.Length; i++)
                {
                    var code = fieldMeta[i].Code;
                    var width = colWidths[i];
                    var prop = avg.GetType().GetProperty(code);
                    var value = prop?.GetValue(avg, null);
                    string formatted = FormatValue(value, width);
                    Console.Write($"| {formatted} ");
                }

                Console.WriteLine();
            }
        }
    }
}