using System.Text;
using System.Reflection;

namespace LEG.CoreLib.DataSource.Csv
{
    public interface ICsvRecord
    {
        public static string GetHeader => "=> Missing Header";
    }

    public class SolarDataBaseImport
    {

        // Define data records for the CSV import
        // Define site dictionary
        public const int IndexSite = 0;
        private static Dictionary<string, ICsvRecord> _siteDictionary = [];
        private const string SiteHeader = "SystemName,State, StreetName, Zip, Town, Lon, Lat, UtcShift, MeteoId, Inverters, Roofs, Consumers";

        public class CsvSiteRecord : ICsvRecord
        // SystemName	State	    StreetName	    ZIP	        Town	    Lon	    Lat	    UtcShift	MeteoId	        Inverters	Roofs	Consumers
        // default 	    study	    default	    default	    default	    10	    47	    -1	        default_meteo	1	        1	    0
        {
            public string? State { get; set; }
            public string? Address { get; set; }
            public string? Zip { get; set; }
            public string? Town { get; set; }
            public double Lon { get; set; }
            public double Lat { get; set; }
            public int UtcShift { get; set; }
            public string? MeteoId { get; set; }
            public int Inverters { get; set; }
            public int Roofs { get; set; }
            public int Consumers { get; set; }
            public static string GetHeader => SiteHeader;
            public override string ToString()
            {
                return $"{State}, {Address}, {Zip}, {Town}, {Lon}, {Lat}, {UtcShift}, {MeteoId}, {Inverters}, {Roofs}, {Consumers}";
            }
        }

        // Define inverter dictionary
        public const int IndexInverter = 1;
        private static Dictionary<string, ICsvRecord> InverterDictionary = [];
        public const string InverterHeader = "SystemName, PvSite, HasBattery	Capacity, MaxLoad, MaxDrain, Roofs";
        public class CsvInverterRecord : ICsvRecord
        // SystemName	PvSite        HasBattery	Capacity	MaxLoad	    MaxDrain	Roofs
        // default 	    default	    False	    0	        0	        0	        1
        {
            public string? Site { get; set; }
            public bool HasBattery { get; set; }
            public double Capacity { get; set; }
            public double MaxLoad { get; set; }
            public double MaxDrain { get; set; }
            public int Roofs { get; set; }
            public static string GetHeader => InverterHeader;
            public override string ToString()
            {
                return $"{Site}, {HasBattery}, {Capacity}, {MaxLoad}, {MaxDrain}, {Roofs}";
            }
        }

        // Define roof dictionary
        public const int IndexRoof = 2;
        private static Dictionary<string, ICsvRecord> RoofDictionary = [];
        public const string RoofHeader = "SystemName, Inverter, Azi, Elev, Elev2, Area, PeakPowerPerRoof";
        public class CsvRoofRecord : ICsvRecord
        // SystemName	Inverter	Azi	    Elev	Elev2	Area	PeakPowerPerRoof
        // default 	    default	    0	    30	    0	    50	    10
        {
            public string? Inverter { get; set; }
            public double Azi { get; set; }
            public double Elev { get; set; }
            public double Elev2 { get; set; }
            public double Area { get; set; }
            public double Peak { get; set; }
            public static string GetHeader => RoofHeader;

            public override string ToString()
            {
                return $"{Inverter}, {Azi}, {Elev}, {Elev2}, {Area}, {Peak}";
            }
        }

        // Define consumer dictionary
        public const int IndexConsumer = 3;
        private static Dictionary<string, ICsvRecord> ConsumerDictionary = [];
        public const string ConsumerHeader = "SystemName, PvSite, Label, AnnualEnergy, PeakPowerPerRoof, AnnualProfileId, WeeklyProfileId, DailyProfileId, HourlyProfileId";
        public class CsvConsumerRecord : ICsvRecord
        // SystemName	PvSite	    Label	AnnualEnergy	PeakPowerPerRoof	    AnnualProfileId	    WeeklyProfileId 	DailyProfileId	    HourlyProfileId
        // default_1	default	    G1	    7’800	4	    a_household         w_household	        d_household	        h_household
        {
            public string? Site { get; set; }
            public string? Label { get; set; }
            public double AnnualEnergy { get; set; }
            public double PeakPower { get; set; }
            public string? AnnualProfileId { get; set; }
            public string? WeeklyProfileId { get; set; }
            public string? DailyProfileId { get; set; }
            public string? HourlyProfileId { get; set; }
            public static string GetHeader => ConsumerHeader;
            public override string ToString()
            {
                return $"{Site}, {Label}, {AnnualEnergy}, {PeakPower}, {AnnualProfileId}, {WeeklyProfileId}, {DailyProfileId}, {HourlyProfileId}";
            }
        }

        // Define meteo dictionary
        public const int IndexMeteo = 4;
        private static Dictionary<string, ICsvRecord> MeteoDictionary = [];
        public const string MeteoHeader = "SystemName, Owner, NrFourier, na, Jan, Feb, Mar, Apr, Mai, Jun, Jul, Aug, Sep, Okt, Nov, Dez";
        public class CsvMeteoProfile : ICsvRecord
        // SystemName	    Owner   NrFourier	na	    Jan 	Feb 	Mar 	Apr 	Mai 	Jun 	Jul 	Aug 	Sep 	Okt 	Nov 	Dez 
        // default_meteo 	none    0	        0	    0.5	    0.5	    0.5	    0.5	    0.5	    0.5	    0.5	    0.5	    0.5	    0.5	    0.5	    0.5
        {
            public string? Owner { get; set; } = "none";
            public int NrFourier { get; set; } = 0;
            public string[] MonthProfile { get; set; } = new string[13];

            public override string ToString()
            {
                return $"{Owner}, {NrFourier}, {string.Join(", ", MonthProfile)}";
            }
        }

        // Define sun profile dictionary
        public const int IndexSun = 5;
        private static Dictionary<string, ICsvRecord> SunDictionary = [];
        public const string SunHeader = "SystemName, Owner, Type, NrFourier, Jan_1, Jan_8, Jan_15, Jan_23, Feb_1, Feb_8, Feb_15, Feb_23, Mar_1, Mar_8, Mar_15, Mar_23, Apr_1, Apr_8, Apr_15, Apr_23, May_1, May_8, May_15, May_23, Jun_1, Jun_8, Jun_15, Jun_23, Jul_1, Jul_8, Jul_15, Jul_23, Aug_1, Aug_8, Aug_15, Aug_23, Sep_1, Sep_8, Sep_15, Sep_23, Oct_1, Oct_8, Oct_15, Oct_23, Nov_1, Nov_8, Nov_15, Nov_23, Dec_1, Dec_8, Dec_15, Dec_23";
        public class CsvSunProfile : ICsvRecord
        {
            public string? Owner { get; set; } = "none";
            public string? Kind { get; set; } = "none";
            public int NrFourier { get; set; } = 0;
            public string[] SunProfile { get; set; } = new string[48];

            public override string ToString()
            {
                return $"{Owner}, {Kind}, {NrFourier}, {string.Join(", ", SunProfile)}";
            }
        }

        // Define annual profile dictionary
        public const int IndexAnnual = 6;
        private static Dictionary<string, ICsvRecord> AnnualDictionary = [];
        public const string AnnualHeader = "SystemName, Ownwer, na, Jan, Feb, Mar, Apr, Mai, Jun, Jul, Aug, Sep, Okt, Nov, Dez";
        public class CsvAnnualProfile : ICsvRecord
        // SystemName	Owner   na      Jan     Feb 	Mar 	Apr 	Mai 	Jun 	Jul 	Aug 	Sep 	Okt 	Nov 	Dez 
        // a_flat	    none    0	    31	    28	    31	    30	    31	    30	    31	    31	    30	    31	    30	    31
        {
            public string? Owner { get; set; } = "none";
            public string[] AnnualProfile { get; set; } = new string[13];

            public override string ToString()
            {
                return $"{Owner}, {string.Join(", ", AnnualProfile)}";
            }
        }

        // Define weekly profile dictionary
        public const int IndexWeekly = 7;
        private static Dictionary<string, ICsvRecord> WeeklyDictionary = [];
        public const string WeeklyHeader = "SystemName, Owner, Mo, Tu, We, Th, Fr, Sa, Su";
        public class CsvWeekProfile : ICsvRecord
        // SystemName	Owner   Mo	Tu	We	Th	Fr	Sa	Su
        //w_flat	    none    24	24	24	24	24	24	24
        {
            public string? Owner { get; set; } = "none";
            public string[] WeeklyProfile { get; set; } = new string[7];

            public override string ToString()
            {
                return $"{Owner}, {string.Join(", ", WeeklyProfile)}";
            }
        }

        // Define daily profile dictionary
        public const int IndexDaily = 8;
        private static Dictionary<string, ICsvRecord> DailyDictionary = [];
        public const string DailyHeader = "SystemName, Owner, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23";
        public class CsvDayProfile : ICsvRecord
        // SystemName	Owner   0	1	2	3	4	5	6	7	8	9	10	11	12	13	14	15	16	17	18	19	20	21	22	23
        // d_flat	    none    1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1	1
        {
            public string? Owner { get; set; } = "none";
            public string[] DailyProfile { get; set; } = new string[24];

            public override string ToString()
            {
                return $"{Owner}, {string.Join(", ", DailyProfile)}";
            }
        }

        // Define hourly profile dictionary
        public const int IndexHourly = 9;
        private static Dictionary<string, ICsvRecord> HourlyDictionary = [];
        public const string HourlyHeader = "SystemName, Owner, avg_hours, min_hours, max_hours";
        public class CsvHourProfile : ICsvRecord
        // SystemName	    Owner   avg_hours   min_hours   max_hours
        // h_household	    none    5	        2	        10
        {
            public string? Owner { get; set; } = "none";
            public string[] HourlyProfile { get; set; } = new string[3];

            public override string ToString()
            {
                return $"{Owner}, {string.Join(", ", HourlyProfile)}";
            }
        }

        // Create lists to hold the dictionary related data structures
        private static List<Dictionary<string, ICsvRecord>> Dictionaries = [                   _siteDictionary,                   InverterDictionary,                  RoofDictionary,                    ConsumerDictionary,                 MeteoDictionary,
                                                                                              SunDictionary,                    AnnualDictionary,                    WeeklyDictionary,                  DailyDictionary,                    HourlyDictionary ];
        private static readonly List<string> _dictionaryNames = [                   "Sites",                          "Inverters",                         "Roofs",                           "Consumers",                        "Meteo",
                                                                                              "SunProfiles",                    "AnnualProfiles",                    "WeeklyProfiles",                  "DailyProfiles",                    "HourlyProfiles" ];
        private static readonly List<string> CsvFileNameList = [                   "Sites_old.csv",                  "Inverters_old.csv",                 "Roofs_old.csv",                   "Consumers_old.csv",                "Meteo_old.csv",
                                                                                              "SunProfiles_old.csv",            "AnnualProfiles_old.csv",            "WeekProfiles_old.csv",            "DayProfiles_old.csv",              "HourProfiles_old.csv" ];
        private static readonly List<List<string>> HeaderList = [ [SiteHeader],  [InverterHeader], [RoofHeader], [ConsumerHeader], [MeteoHeader],
            [SunHeader],  [AnnualHeader], [WeeklyHeader], [DailyHeader], [HourlyHeader] ];


        // ********************************************************************************************************************
        // ********************************************************************************************************************

        public SolarDataBaseImport(string pathImportFiles)
        {
            // Import data from CSV files
            // string pathImportFiles = @"C:\code\Solar\test\MeshWeaver.Solar.test\files\";
            ImportDictionariesFromCsvFiles(pathImportFiles);
        }

        // ********************************************************************************************************************

        public static Dictionary<string, T> ImportCsvToDictionary<T>(string filePath) where T : ICsvRecord, new()
        {
            var dictionary = new Dictionary<string, T>();

            try
            {
                using var reader = new StreamReader(filePath);
                // Read the header line
                var header = reader.ReadLine();
                if (header != null)
                {
                }
                else
                {
                    throw new Exception("CSV file is empty.");
                }

                // Read each subsequent line
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null) continue;

                    var values = line.Split(',');

                    if (values.Length < 2)
                    {
                        throw new Exception("CSV file format is incorrect.");
                    }

                    var key = values[0].Trim();
                    var dataValues = values.Skip(1).Select(v => v.Trim()).ToList();

                    if (!dictionary.ContainsKey(key))
                    {
                        var dataRecord = new T();
                        PopulateDataRecord(dataRecord, dataValues);
                        dictionary.Add(key, dataRecord);
                    }
                    else
                    {
                        Console.WriteLine($"Duplicate key found: {key}. Skipping entry.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CSV file: {ex.Message}");
            }

            return dictionary;
        }

        public static void ExportDictionaryToCsv<TKey, TValue>(Dictionary<TKey, TValue> dictionary, string filePath, string header) where TKey : notnull
        {
            var csv = new StringBuilder();
            csv.AppendLine(header); // Add header line
            foreach (var kvp in dictionary)
            {
                var line = $"{kvp.Key},{kvp.Value}";
                csv.AppendLine(line);
            }
            File.WriteAllText(filePath, csv.ToString());
        }

        // ********************************************************************************************************************

        private static void PopulateDataRecord<T>(T dataRecord, List<string> values) where T : ICsvRecord
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            int valueIndex = 0;

            foreach (var property in properties)
            {
                if (property.CanWrite)
                {
                    try
                    {
                        if (property.PropertyType.IsArray && property.PropertyType.GetElementType() == typeof(string))
                        {
                            // Handle array of strings dynamically
                            var arrayLength = values.Count - valueIndex;
                            var array = new string[arrayLength];
                            for (var j = 0; j < arrayLength && valueIndex < values.Count; j++, valueIndex++)
                            {
                                array[j] = values[valueIndex];
                            }
                            property.SetValue(dataRecord, array);
                        }
                        else
                        {
                            if (valueIndex < values.Count)
                            {
                                var value = Convert.ChangeType(values[valueIndex], property.PropertyType);
                                property.SetValue(dataRecord, value);
                                valueIndex++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (valueIndex < values.Count)
                        {
                            Console.WriteLine($"Error converting value '{values[valueIndex]}' to type '{property.PropertyType}': {ex.Message}");
                        }
                        else
                        {
                            Console.WriteLine($"Error setting property '{property.Name}': not enough values in the data row. {ex.Message}");
                        }
                    }
                }
            }
        }

        // ********************************************************************************************************************

        public static void ImportDictionariesFromCsvFiles(string pathImportFiles, bool printData = false)
        {
            // Import all data records from CSV files
            _siteDictionary = ImportCsvToDictionary<CsvSiteRecord>(pathImportFiles + CsvFileNameList[IndexSite])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            InverterDictionary = ImportCsvToDictionary<CsvInverterRecord>(pathImportFiles + CsvFileNameList[IndexInverter])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            RoofDictionary = ImportCsvToDictionary<CsvRoofRecord>(pathImportFiles + CsvFileNameList[IndexRoof])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            ConsumerDictionary = ImportCsvToDictionary<CsvConsumerRecord>(pathImportFiles + CsvFileNameList[IndexConsumer])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            MeteoDictionary = ImportCsvToDictionary<CsvMeteoProfile>(pathImportFiles + CsvFileNameList[IndexMeteo])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            SunDictionary = ImportCsvToDictionary<CsvSunProfile>(pathImportFiles + CsvFileNameList[IndexSun])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            AnnualDictionary = ImportCsvToDictionary<CsvAnnualProfile>(pathImportFiles + CsvFileNameList[IndexAnnual])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            WeeklyDictionary = ImportCsvToDictionary<CsvWeekProfile>(pathImportFiles + CsvFileNameList[IndexWeekly])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            DailyDictionary = ImportCsvToDictionary<CsvDayProfile>(pathImportFiles + CsvFileNameList[IndexDaily])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            HourlyDictionary = ImportCsvToDictionary<CsvHourProfile>(pathImportFiles + CsvFileNameList[IndexHourly])
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => (ICsvRecord)kvp.Value!);

            // Update the list holding all the Dictionaries
            Dictionaries = [_siteDictionary, InverterDictionary, RoofDictionary, ConsumerDictionary, MeteoDictionary, SunDictionary, AnnualDictionary, WeeklyDictionary, DailyDictionary, HourlyDictionary];

            // Print the Dictionaries to verify the import
            if (printData)
            {
                for (var i = 0; i < Dictionaries.Count; i++)
                {
                    Console.WriteLine($"Data Dictionary {i + 1}: {_dictionaryNames[i]}");
                    foreach (var kvp in Dictionaries[i])
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                }
            }

            // Test data consistency: read all data records
        }

        public static void ExportAllDictionariesToCsvFiles(string exportPath)
        {
            Console.WriteLine($"Exporting Dictionaries to CSV files in directory {exportPath}:");
            for (var i = 0; i < Dictionaries.Count; i++)
            {
                Console.WriteLine($"  - data in dictionary {_dictionaryNames[i]} to file: {CsvFileNameList[i]}");
                ExportDictionaryToCsv(Dictionaries[i], Path.Combine(exportPath, CsvFileNameList[i]), HeaderList[i][0]);
            }
            Console.WriteLine();
        }

        public static void DeleteSiteFromCsvDictionaries(string siteId)
        {
            Console.WriteLine($"Deleting site {siteId} from CSV files in directory:");
            var csvSiteList = _siteDictionary.Keys.ToList();
            var csvInverterList = InverterDictionary.Keys.ToList();
            var csvRoofList = RoofDictionary.Keys.ToList();
            var csvConsumerList = InverterDictionary.Keys.ToList();
            if (!csvSiteList.Contains(siteId))
            {
                Console.WriteLine($"  => PvSite ID '{siteId}' not found in site dictionary {_dictionaryNames[IndexSite]}.");
            }
            else
            {
                _siteDictionary.Remove(siteId);
                Console.WriteLine($"  - PvSite ID '{siteId}' removed");
                foreach (var inverterId in csvInverterList)
                {
                    if (InverterDictionary.TryGetValue(inverterId, out ICsvRecord? inverterRecord))
                    {
                        if (((CsvInverterRecord)inverterRecord).Site == siteId)
                        {
                            InverterDictionary.Remove(inverterId);
                            Console.WriteLine($"    -- Inverter ID '{inverterId}' removed");
                            foreach (var roofId in csvRoofList)
                            {
                                if (RoofDictionary.TryGetValue(roofId, out ICsvRecord? roofRecord))
                                {
                                    if (((CsvRoofRecord)roofRecord).Inverter == inverterId)
                                    {
                                        RoofDictionary.Remove(roofId);
                                        Console.WriteLine($"      --- PvRoof ID '{roofId}' removed");
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (var consumerId in csvConsumerList)
                {
                    if (ConsumerDictionary.TryGetValue(consumerId, out ICsvRecord? consumerRecord))
                    {
                        if (((CsvConsumerRecord)consumerRecord).Site == siteId)
                        {
                            ConsumerDictionary.Remove(consumerId);
                            Console.WriteLine($"    -- Consumer ID '{consumerId}' removed");
                        }
                    }
                }
            }
            Console.WriteLine();
        }
    }
}