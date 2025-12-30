using LEG.PvImport.Abstractions;
using System.Data;

namespace LEG.PvImport.Clients.E3Dc.Client
{
    public class E3DcLoadPeriodRecords
    {
        private static List<E3DcRecord> LoadE3DCRecordsForMonth(string folderName, int year, int month)
        {
            var dataFile = folderName + E3DcFileHelper.FileName(year, month);
            if (!File.Exists(dataFile))
            {
                return [];
            }

            using var fileStream = new FileStream(dataFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fileStream);
            var headerLine = reader.ReadLine();

            fileStream.Position = 0;
            reader.DiscardBufferedData();

            using var csv = new CsvHelper.CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                PrepareHeaderForMatch = args => args.Header.Trim('\"')
            });

            bool isNewFormat = E3DcFileHelper.IsNewPortalFormat(headerLine);
            if (isNewFormat)
            {
                csv.Context.RegisterClassMap<E3DcRecordNewMap>();
            }
            else
            {
                csv.Context.RegisterClassMap<E3DcRecordOldMap>();
            }

            var records = csv.GetRecords<E3DcRecord>().ToList();

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

        //private static List<E3DcRecord> LoadE3DCRecordsForMonth(string folderName, int year, int month)
        //{
        //    var dataFile = folderName + E3DcFileHelper.FileName(year, month);
        //    if (!File.Exists(dataFile))
        //    {
        //        return [];
        //    }

        //    return E3DcCsvImporter.ImportE3DcRecords(dataFile, ";");
        //}

        public static List<E3DcRecord> LoadRecords(int folderNumber, DateTime? startDateTime = null, DateTime? endDateTime = null)
        {
            var (dataFolder, subFolder) = E3DcFileHelper.GetFolder(folderNumber);
            var folder = dataFolder + subFolder;

            var (firstYear, lastYear) = E3DcFileHelper.GetYears(folderNumber);
            var (startMonth, _) = E3DcFileHelper.GetMonthsRange(folderNumber, firstYear);
            var (_, endMonth) = E3DcFileHelper.GetMonthsRange(folderNumber, lastYear);
            startDateTime ??= new DateTime(2000 + firstYear, startMonth, 1, 0, 0, 0);
            endDateTime ??= new DateTime(2000 + lastYear, endMonth, 1, 0, 0, 0).AddMonths(1).AddSeconds(-1);

            var periodRecords = new List<E3DcRecord>();
            for (var year = firstYear; year <= lastYear; year++)
            {
                var (firstMonth, lastMonth) = E3DcFileHelper.GetMonthsRange(folderNumber, year);
                for (var month = firstMonth; month <= lastMonth; month++)
                {
                    var records = LoadE3DCRecordsForMonth(folder, year, month);
                    foreach (var record in records)
                    {
                        var timestamp = E3DcFileHelper.ParseTimestamp(record.Timestamp);
                        if (timestamp < startDateTime || timestamp > endDateTime)
                        {
                            continue;
                        }
                        periodRecords.Add(record);
                    }
                }
            }

            return periodRecords;
        }

        public static List<IPowerRecord> LoadPowerRecords(string siteID, DateTime? startDateTime = null, DateTime? endDateTime = null)
        {
            var folderNumber = siteID== "Senn" ? 1 : siteID == "SennV" ? 2 : 3;
            var periodRecords = LoadRecords(folderNumber, startDateTime, endDateTime);
            var powerRecords = periodRecords.Select(r => new IPowerRecord
            {
                Timestamp = E3DcFileHelper.ParseTimestamp(r.Timestamp),
                SolarProduction = r.SolarProduction
            }).ToList();

            return powerRecords;
        }
    }
}
