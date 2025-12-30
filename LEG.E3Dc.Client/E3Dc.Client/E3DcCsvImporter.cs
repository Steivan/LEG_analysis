using LEG.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LEG.PvImport.Clients.E3Dc.Client
{
    public static class E3DcCsvImporter
    {
        public static List<E3DcRecord> ImportE3DcRecords(string filePath, string delimiter = ";")
        {
            // Read all lines
            var allLines = File.ReadAllLines(filePath);

            // Preprocess header: remove leading/trailing quotes and replace double double-quotes
            var headerLine = allLines[0];
            if (headerLine.StartsWith("\"") && headerLine.EndsWith("\""))
            {
                headerLine = headerLine.Substring(1, headerLine.Length - 2);
            }
            headerLine = headerLine.Replace("\"\"", "\"");

            // Now split and re-join to ensure correct delimiter usage
            var headerFields = headerLine.Split(';').Select(f => f.Trim('\"')).ToArray();
            var cleanHeaderLine = string.Join(";", headerFields);

            // Write to a temp file
            var tempFile = Path.GetTempFileName();
            File.WriteAllLines(tempFile, new[] { cleanHeaderLine }.Concat(allLines.Skip(1)));

            // Detect new format
            bool isNewFormat = cleanHeaderLine.Contains("State of charge [%]");

            // Import records
            var records = ImportCsv.ImportFromFile<E3DcRecord>(tempFile, delimiter, csv =>
            {
                if (isNewFormat)
                    csv.Context.RegisterClassMap<E3DcRecordNewMap>();
                else
                    csv.Context.RegisterClassMap<E3DcRecordOldMap>();
            });

            // Apply conversion for new-format records
            if (isNewFormat)
            {
                foreach (var record in records)
                {
                    record.ConvertPowerFieldsToWh();
                }
            }

            return records;
        }
    }
}