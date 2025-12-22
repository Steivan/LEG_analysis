using CsvHelper;
using LEG.Common;
using LEG.PvImport.Abstractions;
using LEG.PvImport.Abstractions.Fronius.Abstractions;
using NPOI.SS.UserModel;
using System.Globalization;

namespace LEG.PvImport.Clients.Fronius.Client
{
    public class FromiusLoadRecords
    {
        public static List<IPowerRecord?> ImportFroniusRecords(string filePath, string tabName= FroniusConstants.PowerTab, int headerRow=FroniusConstants.HeaderRow)
        {
            return ImportXls.ImportFromFile(
                filePath,
                tabName,
                headerRow,
                row =>
                {
                    var cellA = row.GetCell(0);
                    var cellB = row.GetCell(1);
                    if (cellA == null || cellB == null) return null;

                    DateTime? timestamp = null;
                    double? value = null;

                    if (cellA.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cellA))
                        timestamp = cellA.DateCellValue;
                    else if (cellA.CellType == CellType.String && DateTime.TryParse(cellA.StringCellValue, out var dt))
                        timestamp = dt;

                    if (cellB.CellType == CellType.Numeric)
                        value = cellB.NumericCellValue;
                    else if (cellB.CellType == CellType.String && double.TryParse(cellB.StringCellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                        value = v;

                    // Only return a record if both are non-null
                    if (timestamp is null || value is null)
                        return null;

                    return new IPowerRecord
                    {
                        Timestamp = timestamp.Value,
                        SolarProduction = value.Value
                    };
                }
            );
        }
    }
}
