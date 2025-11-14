using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace LEG.Common
{
    public class ImportCsv
    {
        public static List<T> ImportFromFile<T>(string filePath, string delimiter = ",")
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must not be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("CSV file not found.", filePath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);
            var records = new List<T>(csv.GetRecords<T>());
            return records;
        }
    }
}