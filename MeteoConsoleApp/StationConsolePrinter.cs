using LEG.MeteoSwiss.Abstractions;

namespace MeteoConsoleApp
{
    /// <summary>
    /// Helper class for printing station metadata to the console.
    /// This presentation logic now resides directly in the console application.
    /// </summary>
    public static class StationConsolePrinter
    {
        public static void PrintStationMetaSeparator()
        {
            Console.WriteLine(new string('-', 120));
        }

        public static List<string> PrintStationMetaTable(string title, Dictionary<string, StationMetaInfo> metaDict, string cantonFilter, Func<double, string> toDms, bool lonLatAsDms)
        {
            Console.WriteLine(title);
            PrintStationMetaSeparator();
            Console.WriteLine("{0,-5} {1,-25} {2,8} {3,12} {4,12} {5,8}", "ID", "Name", "Height", "Lon", "Lat", "Canton");
            PrintStationMetaSeparator();

            var idList = new List<string>();
            foreach (var entry in metaDict.Where(e => cantonFilter == "CH" || e.Value.StationCanton == cantonFilter).OrderBy(e => e.Key))
            {
                // ** THE CORRECTED FIX: Use the full property names from the StationMetaInfo struct. **
                var info = entry.Value;
                // Handle nullable double properties before passing to formatting functions.
                var lon = lonLatAsDms && info.StationCoordinatesWgs84Lon.HasValue ? toDms(info.StationCoordinatesWgs84Lon.Value) : info.StationCoordinatesWgs84Lon?.ToString("F4") ?? "N/A";
                var lat = lonLatAsDms && info.StationCoordinatesWgs84Lat.HasValue ? toDms(info.StationCoordinatesWgs84Lat.Value) : info.StationCoordinatesWgs84Lat?.ToString("F4") ?? "N/A";
                Console.WriteLine("{0,-5} {1,-25} {2,8} {3,12} {4,12} {5,8}", entry.Key, info.StationName, info.StationHeightMasl?.ToString("F0") ?? "N/A", lon, lat, info.StationCanton);
                idList.Add(entry.Key);
            }
            return idList;
        }

        public static void PrintStationInfoTable<T>(
            string title,
            Dictionary<string, T> infoDict,
            List<string> idFilter,
            Func<T, object>[] valueSelectors,
            Func<double, string> toDms,
            bool lonLatAsDms)
        {
            _ = toDms;                        // To silence warnings if not used
            _ = lonLatAsDms;

            Console.WriteLine();
            Console.WriteLine(title);
            PrintStationMetaSeparator();

            var headers = new List<string> { "ID" };
            if (typeof(T) == typeof(LongestPerStationMetaInfo))
            {
                headers.AddRange([
                    "Name", "Height", "Lon", "Lat", "First Year", "Last Year", "Total Years", "Years in Ref", "Hourly Data"
                ]);
            }
            else if (typeof(T) == typeof(StandardPerStationMetaInfo))
            {
                headers.AddRange([
                    "Name", "Height", "Lon", "Lat", "First Year", "Last Year", "Years in Std", "NA Years", "Hourly Data"
                ]);
            }
            Console.WriteLine("{0,-5} {1,-25} {2,8} {3,12} {4,12} {5,12} {6,12} {7,12} {8,12} {9,12}", headers.ToArray<object>());
            PrintStationMetaSeparator();

            foreach (var id in idFilter)
            {
                if (infoDict.TryGetValue(id, out T? info))
                {
                    var values = new List<object> { id };
                    values.AddRange(valueSelectors.Select(selector => selector(info)));

                    Console.WriteLine("{0,-5} {1,-25} {2,8} {3,12} {4,12} {5,12} {6,12} {7,12} {8,12} {9,12}", [..values]);
                }
            }
        }

    }
}