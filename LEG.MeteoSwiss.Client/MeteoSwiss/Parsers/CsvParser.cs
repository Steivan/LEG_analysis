using CsvHelper;
using CsvHelper.Configuration;
using LEG.MeteoSwiss.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LEG.MeteoSwiss.Client.MeteoSwiss.Parsers
{
    public static class CsvParser
    {
        public static List<WeatherData> ParseHistoricalCsv(string[] csvRows)
        {
            if (csvRows == null || csvRows.Length < 2)
            {
                return [];
            }

            // ** THE DEFINITIVE FIX: Create a stream from the lines instead of a single giant string. **
            // This avoids the OutOfMemoryException.
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            foreach (var row in csvRows.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                writer.WriteLine(row);
            }
            writer.Flush();
            stream.Position = 0;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";",
                Comment = '#',
                MissingFieldFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true,
            };

            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);
            try
            {
                var classMap = new DefaultClassMap<WeatherData>();

                classMap.Map(m => m.Timestamp)
                    .Name("reference_timestamp")
                    .TypeConverterOption.Format("dd.MM.yyyy HH:mm")
                    .TypeConverterOption.DateTimeStyles(DateTimeStyles.AssumeUniversal);

                classMap.Map(m => m.tre200s0).Name("tre200s0");
                classMap.Map(m => m.gre000s0).Name("gre000z0");

                classMap.Map(m => m.temperature_2m).Ignore();
                classMap.Map(m => m.global_radiation).Ignore();

                csv.Context.RegisterClassMap(classMap);

                var records = csv.GetRecords<WeatherData>().ToList();
                return records;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CsvParser] Failed to parse CSV data. Error: {ex.Message}");
                // The RawRecord property can be very large, so only log it in debug builds.
#if DEBUG
                Console.WriteLine($"[CsvParser] Raw Record causing error: {csv.Parser?.RawRecord}");
#endif
                return [];
            }
        }
    }
}