using LEG.Common;

namespace LEG.E3Dc.Client
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

            return ImportCsv.ImportFromFile<E3DcRecord>(dataFile, ";");
        }

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
    }
}
