using System;
using System.Globalization;
using LEG.E3Dc.Abstractions;

namespace LEG.E3Dc.Client
{
    public static class E3DcFileHelper
    {
        public static DateTime MinDate => new(
            Math.Min(E3DcConstants.FirstYear1, E3DcConstants.FirstYear2),
            Math.Min(E3DcConstants.FirstMonth1, E3DcConstants.FirstMonth2),
            1);
        public static DateTime MaxDate => DateTime.Now;
        public static int NrOfFolders => E3DcConstants.NrOfSubFolders;
        public static string FileBody => E3DcConstants.CsvFileBody;
        public static string FileTail(int year, int month) => $"{year:00}.{month:00}";
        public static string FileExtension => E3DcConstants.CsvExtension;
        public static string FileName(int year, int month) => $"{FileBody}_{FileTail(year, month)}{FileExtension}";
        public static int GetDaysInMonth(int year, int month) => DateTime.DaysInMonth(2000 + year, month);

        public static (string dataFolder, string subFolder) GetFolder(int folderNumber)
        {
            return folderNumber switch
            {
                1 => (E3DcConstants.DataFolder, E3DcConstants.SubFolder1),
                2 => (E3DcConstants.DataFolder, E3DcConstants.SubFolder2),
                _ => throw new ArgumentOutOfRangeException(nameof(folderNumber), $"Folder number must be 1 ... {E3DcConstants.NrOfSubFolders}."),
            };
        }

        public static (int firstYear, int lastYear) GetYears(int folderNumber)
        {
            return folderNumber switch
            {
                1 => (E3DcConstants.FirstYear1, E3DcConstants.LastYear1),
                2 => (E3DcConstants.FirstYear2, E3DcConstants.LastYear2),
                _ => throw new ArgumentOutOfRangeException(nameof(folderNumber), $"Folder number must be 1 ... {E3DcConstants.NrOfSubFolders}."),
            };
        }

        public static (int first, int last) GetMonthsRange(int folderNumber, int year)
        {
            (int first, int last) Range(int firstYear, int firstMonth, int lastYear, int lastMonth)
            {
                if (year < firstYear || year > lastYear)
                    throw new ArgumentOutOfRangeException(nameof(year), $"EvaluationYear must be in range {firstYear} ... {lastYear}.");
                if (year == firstYear && year == lastYear)
                    return (firstMonth, lastMonth);
                if (year == firstYear)
                    return (firstMonth, 12);
                if (year == lastYear)
                    return (1, lastMonth);
                return (1, 12);
            }

            return folderNumber switch
            {
                1 => Range(E3DcConstants.FirstYear1, E3DcConstants.FirstMonth1, E3DcConstants.LastYear1, E3DcConstants.LastMonth1),
                2 => Range(E3DcConstants.FirstYear2, E3DcConstants.FirstMonth2, E3DcConstants.LastYear2, E3DcConstants.LastMonth2),
                _ => throw new ArgumentOutOfRangeException(nameof(folderNumber), $"Folder number must be 1 ... {E3DcConstants.NrOfSubFolders}."),
            };
        }
        public static DateTime ParseTimestamp(string timestamp)
        {
            if (DateTime.TryParseExact(timestamp, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            throw new FormatException($"Invalid timestamp format: {timestamp}");
        }
    }
}