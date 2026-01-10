
namespace CalibrationApp.Helpers
{
    internal class HelperFunctions
    {
        internal static int ModuloIndex(int index, int length)
        {
            return (index % length + length) % length;
        }

        internal static double LinearBackground(double x, double xLo, double gLo, double gTrend) => gLo + gTrend * (x - xLo);

    }
}
