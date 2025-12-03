using LEG.Common;
using LEG.MeteoSwiss.Abstractions.Models;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LEG.Tests")]

namespace LEG.MeteoSwiss.Client.MeteoSwiss
{
    public static class MeteoAggregator
    {
        public static List<WeatherCsvRecord> GetFilteredRecords(
            string stationId,
            StationMetaInfo stationMetaInfo,
            int periodStartYear,
            int periodEndYear,
            string granularity = "t",
            bool isTower = false,
            bool includeRecent = true,
            bool includeNow = false)
        {
            var getRecent = includeRecent && DateTime.UtcNow.Year <= periodEndYear;
            var getNow = includeNow && getRecent;

            var startDecade = (periodStartYear / 10) * 10;
            var endDecade = (periodEndYear / 10) * 10;

            var allRecords = new List<WeatherCsvRecord>();
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
            if (getRecent)
            {
                // Also get recent data from the "recent" file
                var recentPeriod = "recent";
                var recentFilePath = "";
                if (isTower)
                {
                    (_, recentFilePath) = MeteoSwissHelper.GetTowerCsvFilename(stationId, recentPeriod, granularity: granularity);
                }
                else
                {
                    (_, recentFilePath) = MeteoSwissHelper.GetGroundCsvFilename(stationId, recentPeriod, granularity: granularity);
                }
                if (File.Exists(recentFilePath))
                {
                    var recentRecords = ImportCsv.ImportFromFile<WeatherCsvRecord>(recentFilePath, ";");
                    allRecords.AddRange(recentRecords);
                }
                else
                {
                    Console.WriteLine($"Warning: Recent file not found: {recentFilePath}");
                }
            }
            if (getNow)
            {
                var nowRecords = GetNowRecords(stationId, stationMetaInfo, granularity: granularity, isTower: isTower);
                if (nowRecords != null) allRecords.AddRange(nowRecords);
            }

            // Filter records for the period of interest
            var filteredRecords = allRecords
                .Where(r => r.ReferenceTimestamp.Year >= periodStartYear && r.ReferenceTimestamp.Year <= periodEndYear)
                .ToList();

            if (filteredRecords.Count == 0)
            {
                Console.WriteLine("No records found for the specified period.");
                return [];
            }
            return filteredRecords;
        }

        // Convenience method to get the latest "now" record
        public static WeatherCsvRecord? GetStationLatestRecord(
            string stationId,
            StationMetaInfo stationMetaInfo,
            string granularity = "t",
            bool isTower = false)
        {
            var nowRecords = GetNowRecords(stationId, stationMetaInfo, granularity: granularity, isTower: isTower);

            return nowRecords == null ? null : nowRecords[^1];
        }

        // Convenience method to get MeteoParameters records directly
        public static List<MeteoParameters> GetFilteredMeteoParametersRecords(
            string stationId,
            StationMetaInfo stationMetaInfo,
            int periodStartYear,
            int periodEndYear,
            string granularity = "t",
            bool isTower = false,
            bool includeRecent = true,
            bool includeNow = false)
        {
            var weatherRecords = GetFilteredRecords(
                stationId: stationId,
                stationMetaInfo: stationMetaInfo,
                periodStartYear: periodStartYear,
                periodEndYear: periodEndYear,
                granularity: granularity,
                isTower: isTower,
                includeRecent: includeRecent,
                includeNow: includeNow);

            return weatherRecords
                .Select(r => r.ToMeteoParameters())
                .ToList();
        }

        public static MeteoParameters? GetStationLatestMeteoParametersRecord(
            string stationId,
            StationMetaInfo stationMetaInfo,
            string granularity = "t",
            bool isTower = false)
        {
            var weatherRecord = GetStationLatestRecord(
                stationId: stationId,
                stationMetaInfo: stationMetaInfo,
                granularity: granularity,
                isTower: isTower);

            return weatherRecord == null ? null : weatherRecord.ToMeteoParameters();
        }


        // Helpermethod to get "now" records
        private static List<WeatherCsvRecord>? GetNowRecords(
            string stationId,
            StationMetaInfo stationMetaInfo,
            string granularity = "t",
            bool isTower = false)
        {
            var nowPeriod = "now";
            var nowFilePath = "";
            if (isTower)
            {
                (_, nowFilePath) = MeteoSwissHelper.GetTowerCsvFilename(stationId, nowPeriod, granularity: granularity);
            }
            else
            {
                (_, nowFilePath) = MeteoSwissHelper.GetGroundCsvFilename(stationId, nowPeriod, granularity: granularity);
            }
            if (File.Exists(nowFilePath))
            {
                var nowRecords = ImportCsv.ImportFromFile<WeatherCsvRecord>(nowFilePath, ";");
                return nowRecords;
            }
            else
            {
                Console.WriteLine($"Warning: Now file not found: {nowFilePath}");
                return null;
            }
        }

        public static void RunMeteoAggregationForPeriod(string stationId, StationMetaInfo stationMetaInfo, int periodStartYear, int periodEndYear, string granularity = "t", bool isTower = false)
        {
            var filteredRecords = GetFilteredRecords(stationId, stationMetaInfo, periodStartYear, periodEndYear, granularity, isTower, includeRecent: false, includeNow: false);
            // --- Monthly averages ---
            var monthlyAverages = filteredRecords
                .GroupBy(r => new { r.ReferenceTimestamp.Year, r.ReferenceTimestamp.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Temperature2m = SafeAverage(g.Select(r => r.Temperature2m)),
                    Temperature5cm = SafeAverage(g.Select(r => r.Temperature5cm)),
                    TemperatureSurface = SafeAverage(g.Select(r => r.TemperatureSurface)),
                    WindChill = SafeAverage(g.Select(r => r.WindChill)),
                    RelativeHumidity2m = SafeAverage(g.Select(r => r.RelativeHumidity2m)),
                    DewPoint2m = SafeAverage(g.Select(r => r.DewPoint2m)),
                    VaporPressure2m = SafeAverage(g.Select(r => r.VaporPressure2m)),
                    PressureAtStation = SafeAverage(g.Select(r => r.PressureAtStation)),
                    PressureQNH = SafeAverage(g.Select(r => r.PressureQNH)),
                    PressureQFF = SafeAverage(g.Select(r => r.PressureQFF)),
                    GeopotentialHeight850hPa = SafeAverage(g.Select(r => r.GeopotentialHeight850hPa)),
                    GeopotentialHeight700hPa = SafeAverage(g.Select(r => r.GeopotentialHeight700hPa)),
                    WindGust1s = SafeMax(g.Select(r => r.WindGust1s)),
                    WindSpeedVectorial10min = SafeAverage(g.Select(r => r.WindSpeedVectorial10min)),
                    WindSpeedScalar10min = SafeAverage(g.Select(r => r.WindSpeedScalar10min)),
                    WindDirection = SafeVectorAverageWindDirection(g),
                    FoehnIndex = SafeAverage(g.Select(r => r.FoehnIndex)),
                    WindSpeed10min_kmh = SafeAverage(g.Select(r => r.WindSpeed10min_kmh)),
                    WindGust3s = SafeMax(g.Select(r => r.WindGust3s)),
                    WindGust1s_kmh = SafeMax(g.Select(r => r.WindGust1s_kmh)),
                    WindGust3s_kmh = SafeMax(g.Select(r => r.WindGust3s_kmh)),
                    Precipitation = SafeSum(g.Select(r => r.Precipitation)),
                    SnowDepth = SafeAverage(g.Select(r => r.SnowDepth)),
                    DirectRadiation = SafeAverage(g.Select(r => r.ShortWaveRadiation)),
                    DirectNormalRadiation = SafeAverage(g.Select(r => r.DirectNormalIrradiance)),
                    DiffuseRadiation = SafeAverage(g.Select(r => r.DiffuseRadiation)),
                    LongwaveRadiationIncoming = SafeAverage(g.Select(r => r.LongwaveRadiationIncoming)),
                    LongwaveRadiationOutgoing = SafeAverage(g.Select(r => r.LongwaveRadiationOutgoing)),
                    ShortwaveRadiationReflected = SafeAverage(g.Select(r => r.ShortwaveRadiationReflected)),
                    SunshineDuration = SafeSum(g.Select(r => r.SunshineDuration)) / (DateTime.DaysInMonth(g.Key.Year, g.Key.Month) * 60.0)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            // --- Annual averages ---
            var annualAverages = filteredRecords
                .GroupBy(r => r.ReferenceTimestamp.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Temperature2m = SafeAverage(g.Select(r => r.Temperature2m)),
                    Temperature5cm = SafeAverage(g.Select(r => r.Temperature5cm)),
                    TemperatureSurface = SafeAverage(g.Select(r => r.TemperatureSurface)),
                    WindChill = SafeAverage(g.Select(r => r.WindChill)),
                    RelativeHumidity2m = SafeAverage(g.Select(r => r.RelativeHumidity2m)),
                    DewPoint2m = SafeAverage(g.Select(r => r.DewPoint2m)),
                    VaporPressure2m = SafeAverage(g.Select(r => r.VaporPressure2m)),
                    PressureAtStation = SafeAverage(g.Select(r => r.PressureAtStation)),
                    PressureQNH = SafeAverage(g.Select(r => r.PressureQNH)),
                    PressureQFF = SafeAverage(g.Select(r => r.PressureQFF)),
                    GeopotentialHeight850hPa = SafeAverage(g.Select(r => r.GeopotentialHeight850hPa)),
                    GeopotentialHeight700hPa = SafeAverage(g.Select(r => r.GeopotentialHeight700hPa)),
                    WindGust1s = SafeMax(g.Select(r => r.WindGust1s)),
                    WindSpeedVectorial10min = SafeAverage(g.Select(r => r.WindSpeedVectorial10min)),
                    WindSpeedScalar10min = SafeAverage(g.Select(r => r.WindSpeedScalar10min)),
                    WindDirection = SafeVectorAverageWindDirection(g),
                    FoehnIndex = SafeAverage(g.Select(r => r.FoehnIndex)),
                    WindSpeed10min_kmh = SafeAverage(g.Select(r => r.WindSpeed10min_kmh)),
                    WindGust3s = SafeMax(g.Select(r => r.WindGust3s)),
                    WindGust1s_kmh = SafeMax(g.Select(r => r.WindGust1s_kmh)),
                    WindGust3s_kmh = SafeMax(g.Select(r => r.WindGust3s_kmh)),
                    Precipitation = SafeSum(g.Select(r => r.Precipitation)),
                    SnowDepth = SafeAverage(g.Select(r => r.SnowDepth)),
                    DirectRadiation = SafeAverage(g.Select(r => r.ShortWaveRadiation)),
                    DirectNormalRadiation = SafeAverage(g.Select(r => r.DirectNormalIrradiance)),
                    DiffuseRadiation = SafeAverage(g.Select(r => r.DiffuseRadiation)),
                    LongwaveRadiationIncoming = SafeAverage(g.Select(r => r.LongwaveRadiationIncoming)),
                    LongwaveRadiationOutgoing = SafeAverage(g.Select(r => r.LongwaveRadiationOutgoing)),
                    ShortwaveRadiationReflected = SafeAverage(g.Select(r => r.ShortwaveRadiationReflected)),
                    SunshineDuration = SafeSum(g.Select(r => r.SunshineDuration)) / ((DateTime.IsLeapYear(g.Key) ? 366 : 365) * 60.0)
                })
                .OrderBy(x => x.Year)
                .ToList();

            // --- Overall averages ---
            var totalDays = filteredRecords.Select(r => r.ReferenceTimestamp.Date).Distinct().Count();
            var overallAverage = new
            {
                Temperature2m = SafeAverage(filteredRecords.Select(r => r.Temperature2m)),
                Temperature5cm = SafeAverage(filteredRecords.Select(r => r.Temperature5cm)),
                TemperatureSurface = SafeAverage(filteredRecords.Select(r => r.TemperatureSurface)),
                WindChill = SafeAverage(filteredRecords.Select(r => r.WindChill)),
                RelativeHumidity2m = SafeAverage(filteredRecords.Select(r => r.RelativeHumidity2m)),
                DewPoint2m = SafeAverage(filteredRecords.Select(r => r.DewPoint2m)),
                VaporPressure2m = SafeAverage(filteredRecords.Select(r => r.VaporPressure2m)),
                PressureAtStation = SafeAverage(filteredRecords.Select(r => r.PressureAtStation)),
                PressureQNH = SafeAverage(filteredRecords.Select(r => r.PressureQNH)),
                PressureQFF = SafeAverage(filteredRecords.Select(r => r.PressureQFF)),
                GeopotentialHeight850hPa = SafeAverage(filteredRecords.Select(r => r.GeopotentialHeight850hPa)),
                GeopotentialHeight700hPa = SafeAverage(filteredRecords.Select(r => r.GeopotentialHeight700hPa)),
                WindGust1s = SafeMax(filteredRecords.Select(r => r.WindGust1s)),
                WindSpeedVectorial10min = SafeAverage(filteredRecords.Select(r => r.WindSpeedVectorial10min)),
                WindSpeedScalar10min = SafeAverage(filteredRecords.Select(r => r.WindSpeedScalar10min)),
                WindDirection = SafeVectorAverageWindDirection(filteredRecords),
                FoehnIndex = SafeAverage(filteredRecords.Select(r => r.FoehnIndex)),
                WindSpeed10min_kmh = SafeAverage(filteredRecords.Select(r => r.WindSpeed10min_kmh)),
                WindGust3s = SafeMax(filteredRecords.Select(r => r.WindGust3s)),
                WindGust1s_kmh = SafeMax(filteredRecords.Select(r => r.WindGust1s_kmh)),
                WindGust3s_kmh = SafeMax(filteredRecords.Select(r => r.WindGust3s_kmh)),
                Precipitation = SafeSum(filteredRecords.Select(r => r.Precipitation)),
                SnowDepth = SafeAverage(filteredRecords.Select(r => r.SnowDepth)),
                DirectRadiation = SafeAverage(filteredRecords.Select(r => r.ShortWaveRadiation)),
                DirectNormalRadiation = SafeAverage(filteredRecords.Select(r => r.DirectNormalIrradiance)),
                DiffuseRadiation = SafeAverage(filteredRecords.Select(r => r.DiffuseRadiation)),
                LongwaveRadiationIncoming = SafeAverage(filteredRecords.Select(r => r.LongwaveRadiationIncoming)),
                LongwaveRadiationOutgoing = SafeAverage(filteredRecords.Select(r => r.LongwaveRadiationOutgoing)),
                ShortwaveRadiationReflected = SafeAverage(filteredRecords.Select(r => r.ShortwaveRadiationReflected)),
                SunshineDuration = totalDays > 0 ? SafeSum(filteredRecords.Select(r => r.SunshineDuration)) / (totalDays * 60.0) : null
            };

            // --- Monthly averages by calendar month (across all years) ---
            var monthNames = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;
            var monthlyAveragesByMonth = filteredRecords
                .GroupBy(r => r.ReferenceTimestamp.Month)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var totalDaysInMonth = g.Select(r => r.ReferenceTimestamp.Date).Distinct().Count();
                    return new
                    {
                        Month = g.Key,
                        Temperature2m = SafeAverage(g.Select(r => r.Temperature2m)),
                        Temperature5cm = SafeAverage(g.Select(r => r.Temperature5cm)),
                        TemperatureSurface = SafeAverage(g.Select(r => r.TemperatureSurface)),
                        WindChill = SafeAverage(g.Select(r => r.WindChill)),
                        RelativeHumidity2m = SafeAverage(g.Select(r => r.RelativeHumidity2m)),
                        DewPoint2m = SafeAverage(g.Select(r => r.DewPoint2m)),
                        VaporPressure2m = SafeAverage(g.Select(r => r.VaporPressure2m)),
                        PressureAtStation = SafeAverage(g.Select(r => r.PressureAtStation)),
                        PressureQNH = SafeAverage(g.Select(r => r.PressureQNH)),
                        PressureQFF = SafeAverage(g.Select(r => r.PressureQFF)),
                        GeopotentialHeight850hPa = SafeAverage(g.Select(r => r.GeopotentialHeight850hPa)),
                        GeopotentialHeight700hPa = SafeAverage(g.Select(r => r.GeopotentialHeight700hPa)),
                        WindGust1s = SafeMax(g.Select(r => r.WindGust1s)),
                        WindSpeedVectorial10min = SafeAverage(g.Select(r => r.WindSpeedVectorial10min)),
                        WindSpeedScalar10min = SafeAverage(g.Select(r => r.WindSpeedScalar10min)),
                        WindDirection = SafeVectorAverageWindDirection(g),
                        FoehnIndex = SafeAverage(g.Select(r => r.FoehnIndex)),
                        WindSpeed10min_kmh = SafeAverage(g.Select(r => r.WindSpeed10min_kmh)),
                        WindGust3s = SafeMax(g.Select(r => r.WindGust3s)),
                        WindGust1s_kmh = SafeMax(g.Select(r => r.WindGust1s_kmh)),
                        WindGust3s_kmh = SafeMax(g.Select(r => r.WindGust3s_kmh)),
                        Precipitation = SafeSum(g.Select(r => r.Precipitation)),
                        SnowDepth = SafeAverage(g.Select(r => r.SnowDepth)),
                        DirectRadiation = SafeAverage(g.Select(r => r.ShortWaveRadiation)),
                        DirectNormalRadiation = SafeAverage(g.Select(r => r.DirectNormalIrradiance)),
                        DiffuseRadiation = SafeAverage(g.Select(r => r.DiffuseRadiation)),
                        LongwaveRadiationIncoming = SafeAverage(g.Select(r => r.LongwaveRadiationIncoming)),
                        LongwaveRadiationOutgoing = SafeAverage(g.Select(r => r.LongwaveRadiationOutgoing)),
                        ShortwaveRadiationReflected = SafeAverage(g.Select(r => r.ShortwaveRadiationReflected)),
                        SunshineDuration = totalDaysInMonth > 0 ? SafeSum(g.Select(r => r.SunshineDuration)) / (totalDaysInMonth * 60.0) : null
                    };
                })
                .ToList();

            // Prepare field codes, labels, and units
            var fieldMeta = new[]
            {
                new { Code = "Temperature2m", Label = "Temp 2m", Unit = "[°C]" },
                new { Code = "Temperature5cm", Label = "Temp 5cm", Unit = "[°C]" },
                new { Code = "TemperatureSurface", Label = "Temp Sfc", Unit = "[°C]" },
                new { Code = "WindChill", Label = "W. Chill", Unit = "[°C]" },
                new { Code = "RelativeHumidity2m", Label = "Rel Hum", Unit = "[%]" },
                new { Code = "DewPoint2m", Label = "Dew Point", Unit = "[°C]" },
                new { Code = "VaporPressure2m", Label = "Vapor Prs", Unit = "[hPa]" },
                new { Code = "PressureAtStation", Label = "Pressure", Unit = "[hPa]" },
                new { Code = "PressureQNH", Label = "Prs QNH", Unit = "[hPa]" },
                new { Code = "PressureQFF", Label = "Prs QFF", Unit = "[hPa]" },
                new { Code = "GeopotentialHeight850hPa", Label = "GeoPot 850", Unit = "[gpm]" },
                new { Code = "GeopotentialHeight700hPa", Label = "GeoPot 700", Unit = "[gpm]" },
                new { Code = "WindSpeedVectorial10min", Label = "WSpd Vec", Unit = "[m/s]" },
                new { Code = "WindSpeedScalar10min", Label = "WSpd Scal", Unit = "[m/s]" },
                new { Code = "WindDirection", Label = "Wind Dir", Unit = "[°]" },
                new { Code = "WindSpeed10min_kmh", Label = "WSpd 10min", Unit = "[km/h]" },
                new { Code = "WindGust1s", Label = "Gust 1s", Unit = "[m/s]" },
                new { Code = "WindGust3s", Label = "Gust 3s", Unit = "[m/s]" },
                new { Code = "WindGust1s_kmh", Label = "Gust 1s", Unit = "[km/h]" },
                new { Code = "WindGust3s_kmh", Label = "Gust 3s", Unit = "[km/h]" },
                new { Code = "Precipitation", Label = "Precip", Unit = "[mm]" },
                new { Code = "SnowDepth", Label = "Snow Dpt", Unit = "[cm]" },
                new { Code = "DirectRadiation", Label = "Glob Rad", Unit = "[W/m²]" },
                new { Code = "DirectNormalRadiation", Label = "DNI", Unit = "[W/m²]" },
                new { Code = "DiffuseRadiation", Label = "Diff Rad", Unit = "[W/m²]" },
                new { Code = "SunshineDuration", Label = "Sun Dur", Unit = "[h/day]" },
                new { Code = "LongwaveRadiationIncoming", Label = "LW Rad In", Unit = "[W/m²]" },
                new { Code = "LongwaveRadiationOutgoing", Label = "LW Rad Out", Unit = "[W/m²]" },
                new { Code = "ShortwaveRadiationReflected", Label = "SW Rad Ref", Unit = "[W/m²]" },
                new { Code = "FoehnIndex", Label = "Foehn Idx", Unit = "[code]" }
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
            Console.Write($"{"Year-Mo",dateColWidth} ");
            for (int i = 0; i < fieldMeta.Length; i++)
                Console.Write($"| {fieldMeta[i].Label.PadRight(colWidths[i])} ");
            Console.WriteLine();

            // Header row: units
            Console.Write(new string(' ', dateColWidth) + " ");
            for (int i = 0; i < fieldMeta.Length; i++)
                Console.Write($"| {fieldMeta[i].Unit.PadRight(colWidths[i])} ");
            Console.WriteLine();

            // Separator
            Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => colWidths[Array.IndexOf(fieldMeta, f)] + 3)));


            // Data rows with year separator
            int? lastYear = null;
            foreach (var avg in monthlyAverages)
            {
                if (lastYear != null && avg.Year != lastYear)
                {
                    Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => colWidths[Array.IndexOf(fieldMeta, f)] + 3)));
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
            Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => colWidths[Array.IndexOf(fieldMeta, f)] + 3)));

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
            Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => colWidths[Array.IndexOf(fieldMeta, f)] + 3)));

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
            Console.WriteLine(new string('-', dateColWidth + 1 + fieldMeta.Sum(f => colWidths[Array.IndexOf(fieldMeta, f)] + 3)));

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

        // Helper to compute average, returns null if no valid data
        internal static double? SafeAverage(IEnumerable<double?> values)
        {
#pragma warning disable CS8629
            var valid = values.Where(v => v.HasValue && !double.IsNaN(v.Value)).Select(v => v.Value!).ToList();
#pragma warning restore CS8629
            return valid.Count > 0 ? valid.Average() : (double?)null;
        }

        internal static double? SafeMax(IEnumerable<double?> values)
        {
#pragma warning disable CS8629
            var valid = values.Where(v => v.HasValue && !double.IsNaN(v.Value)).Select(v => v.Value!).ToList();
#pragma warning restore CS8629
            return valid.Count > 0 ? valid.Max() : (double?)null;
        }

        // Helper to compute sum, returns null if no valid data
        internal static double? SafeSum(IEnumerable<double?> values)
        {
            var valid = values.Where(v => v.HasValue && !double.IsNaN(v.Value)).Select(v => v.Value!).ToList();
            return valid.Count > 0 ? valid.Sum() : (double?)null;
        }

        // Helper to compute vector average for wind direction
        internal static double? SafeVectorAverageWindDirection(IEnumerable<WeatherCsvRecord> records)
        {
            var vectors = records
                .Where(r => r.WindDirection.HasValue && r.WindSpeedVectorial10min.HasValue && !double.IsNaN(r.WindDirection.Value) && !double.IsNaN(r.WindSpeedVectorial10min.Value))
                .Select(r => new { Speed = r.WindSpeedVectorial10min.Value, Direction = r.WindDirection.Value * Math.PI / 180.0 }) // Convert direction to radians
                .ToList();

            if (vectors.Count == 0)
                return null;

            // Sum of U (East-West) and V (North-South) components
            var sumU = vectors.Sum(v => v.Speed * Math.Sin(v.Direction));
            var sumV = vectors.Sum(v => v.Speed * Math.Cos(v.Direction));

            // Check for a near-zero vector magnitude to avoid undefined direction
            if (Math.Abs(sumU) < 1e-10 && Math.Abs(sumV) < 1e-10)
                return null; // Or 0, depending on desired behavior for calm/opposing winds

            var avgU = sumU / vectors.Count;
            var avgV = sumV / vectors.Count;

            var avgDirectionRad = Math.Atan2(avgU, avgV);
            var avgDirectionDeg = avgDirectionRad * 180.0 / Math.PI;

            // Convert from mathematical angle to meteorological angle (0-360°)
            return (avgDirectionDeg + 360) % 360;
        }

        private static string FormatValue(object? value, int width)
        {
            if (value == null)
                return "n/a".PadLeft(width);

            if (value is double d)
                return d.ToString("0.00").PadLeft(width);

            if (value is double?)
            {
                var nullable = (double?)value;
                return nullable.HasValue
                    ? nullable.Value.ToString("0.00").PadLeft(width)
                    : "n/a".PadLeft(width);
            }

            return "n/a".PadLeft(width);
        }
    }
}