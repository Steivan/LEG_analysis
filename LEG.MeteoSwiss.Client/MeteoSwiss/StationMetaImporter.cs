using System.Globalization;
using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.MeteoSwiss.Client.MeteoSwiss
{
    public static class StationMetaImporter
    {
        public static (string FullName, string LocationNotes) StationInfo(StationMetaInfo info)
        {
            var stationInfo = info.StationCanton
                              //+ ", " + info.StationWigosId
                              + ", " + info.StationTypeEn
                              //+ ", " + info.StationDataowner 
                              + ", " + info.StationDataSince
                              + ", " + info.StationHeightMasl
                              //+ ", " + info.StationHeightBarometerMasl
                              //+ ", " + info.StationCoordinatesLv95East
                              //+ ", " + info.StationCoordinatesLv95North
                              + ", " + info.StationCoordinatesWgs84Lat
                              + ", " + info.StationCoordinatesWgs84Lon
                              //+ ", " + info.StationExpositionEn
                              //+ ", " + info.StationUrlEn
                              ;
            return (info.StationName, stationInfo);
        }

        public static Dictionary<string, StationMetaInfo> Import(string csvPath)
        {
            var dict = new Dictionary<string, StationMetaInfo>(StringComparer.OrdinalIgnoreCase);

            using var reader = new StreamReader(csvPath, System.Text.Encoding.UTF8);
            string? headerLine = reader.ReadLine() ?? throw new InvalidOperationException("CSV file is empty.");
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = line.Split(';');
                if (fields.Length != 24)
                {
                    Console.Error.WriteLine($"Warning: Skipping line with {fields.Length} fields (expected 24): {line}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(fields[0]))
                {
                    Console.Error.WriteLine($"Warning: Skipping line with empty station abbreviation: {line}");
                    continue;
                }

                if (dict.ContainsKey(fields[0]))
                {
                    Console.Error.WriteLine(
                        $"Warning: Duplicate station abbreviation {fields[0]} found, skipping: {line}");
                    continue;
                }

                double? ParseDouble(string s)
                {
                    if (string.IsNullOrEmpty(s) || !double.TryParse(s, NumberStyles.Any,
                            CultureInfo.InvariantCulture, out double d))
                        return null;
                    return d;
                }

                string ParseString(string s) => s ?? "";

                var info = new StationMetaInfo(
                    stationName: ParseString(fields[1]),
                    stationCanton: ParseString(fields[2]),
                    stationWigosId: ParseString(fields[3]),
                    stationTypeDe: ParseString(fields[4]),
                    stationTypeFr: ParseString(fields[5]),
                    stationTypeIt: ParseString(fields[6]),
                    stationTypeEn: ParseString(fields[7]),
                    stationDataowner: ParseString(fields[8]),
                    stationDataSince: ParseString(fields[9]),
                    stationHeightMasl: ParseDouble(fields[10]),
                    stationHeightBarometerMasl: ParseDouble(fields[11]),
                    stationCoordinatesLv95East: ParseDouble(fields[12]),
                    stationCoordinatesLv95North: ParseDouble(fields[13]),
                    stationCoordinatesWgs84Lat: ParseDouble(fields[14]),
                    stationCoordinatesWgs84Lon: ParseDouble(fields[15]),
                    stationExpositionDe: ParseString(fields[16]),
                    stationExpositionFr: ParseString(fields[17]),
                    stationExpositionIt: ParseString(fields[18]),
                    stationExpositionEn: ParseString(fields[19]),
                    stationUrlDe: ParseString(fields[20]),
                    stationUrlFr: ParseString(fields[21]),
                    stationUrlIt: ParseString(fields[22]),
                    stationUrlEn: ParseString(fields[23])
                );

                dict[fields[0]] = info;
            }

            return dict;
        }

        public static List<string> GroundStations(string csvPath)
        {
            var stationMetaDict = Import(csvPath);
            return [.. stationMetaDict.Keys];
        }
    }
}