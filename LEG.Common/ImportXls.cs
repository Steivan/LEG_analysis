using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace LEG.Common
{
    public class ImportXls
    {
        public static List<T> ImportFromFile<T>(
            string filePath,
            string tabName,
            int headerRow,
            Func<IRow, T> rowMapper)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must not be null or empty.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("XLS file not found.", filePath);

            var result = new List<T>();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var workbook = new HSSFWorkbook(fs);
            var sheet = workbook.GetSheet(tabName);
            if (sheet == null)
                throw new ArgumentException($"Sheet '{tabName}' not found in file '{filePath}'.");

            for (int rowIdx = headerRow + 1; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null) continue;
                var record = rowMapper(row);
                if (record != null)
                    result.Add(record);
            }
            return result;
        }
    }
}
