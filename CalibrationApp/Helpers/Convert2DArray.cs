
namespace CalibrationApp.Helpers
{
    internal class Convert2DArray
    {
        internal static double[] GetRow(double[,] data, int rowIndex, int columns)
        {
            double[] row = Enumerable.Range(0, columns)
            .Select(j => data[rowIndex, j])
            .ToArray();

            return row;
        }

    }
}
