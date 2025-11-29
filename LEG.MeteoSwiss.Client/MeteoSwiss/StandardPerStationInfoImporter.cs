using System.Globalization;
using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.MeteoSwiss.Client.MeteoSwiss
{
    public static class StandardPerStationInfoImporter
    {
        public static Dictionary<string, StandardPerStationMetaInfo> Import(string csvPath)
        {
            var result = new Dictionary<string, StandardPerStationMetaInfo>(StringComparer.OrdinalIgnoreCase);
            using var reader = new StreamReader(csvPath);
            string? line;
            bool foundSeparator = false;
            // Search for the separator line
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("##-----------------------------------------------------------"))
                {
                    foundSeparator = true;
                    break;
                }
            }

            if (!foundSeparator)
                throw new InvalidOperationException("Separator row not found in CSV.");

            // The next non-empty line is the header
            string? headerLine = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    headerLine = line;
                    break;
                }
            }

            if (headerLine == null)
                throw new InvalidOperationException("Header row not found after separator in CSV.");

            // Now process data rows
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = line.Split(';');
                if (fields.Length < 19)
                {
                    Console.Error.WriteLine($"Warning: Skipping line with {fields.Length} fields (expected at least 19): {line}");
                    continue;
                }

                var info = new StandardPerStationMetaInfo
                {
                    Name = fields[0].Trim('"'),
                    NatAbbr = fields[1].Trim('"'),
                    WmoInd = fields[2].Trim('"'),
                    Chx = ParseDouble(fields[3]),
                    Chy = ParseDouble(fields[4]),
                    Lon = ParseDouble(fields[5]),
                    Lat = ParseDouble(fields[6]),
                    Height = ParseDouble(fields[7]),
                    ClimateRegion = fields[8].Trim('"'),
                    ClimateRegionNr = ParseInt(fields[9]),
                    FirstYearDailyObs = ParseInt(fields[10]),
                    LastYearDailyObs = ParseInt(fields[11]),
                    NrYearsDailyObsInStandardPer = ParseInt(fields[12]),
                    NrNaYearsDailyObsInStandardPer = ParseInt(fields[13]),
                    FirstYearHourlyObs = ParseInt(fields[14]),
                    LastYearHourlyObs = ParseInt(fields[15]),
                    NrYearsHourlyObs = ParseInt(fields[16]),
                    NrNaYearsHourlyObs = ParseInt(fields[17]),
                    HasHourlyData = ParseBool(fields[18])
                };

                // Use NatAbbr as the key
                if (!string.IsNullOrWhiteSpace(info.NatAbbr) && !result.ContainsKey(info.NatAbbr))
                    result[info.NatAbbr] = info;
            }
            return result;
        }

        private static double? ParseDouble(string s)
        {
            return (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) ? (double?)d : null;
        }

        private static int? ParseInt(string s)
        {
            return (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out int i)) ? (int?)i : null;
        }

        private static bool ParseBool(string s)
        {
            return s.Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        }
    }
}