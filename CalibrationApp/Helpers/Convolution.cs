namespace CalibrationApp.Helpers
{
    internal class Convolution
    {
        internal static double[] ConvoluteCircular(double[] data, double[] kernel, bool centered = true)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data array cannot be null or empty.");
            }
            if (kernel == null || kernel.Length == 0)
            {
                throw new ArgumentException("Kernel array cannot be null or empty.");
            }
            ReadOnlySpan<double> dataSpan = data;
            ReadOnlySpan<double> kernelSpan = kernel;
            int dataLength = dataSpan.Length;
            int kernelLength = kernelSpan.Length;
            int offset = centered? kernelLength / 2 : 0;
            double[] result = new double[dataLength];
            for (int i = 0; i < dataLength; i++)
            {
                double sum = 0.0;
                for (int j = 0; j < kernelLength; j++)
                {
                    var dataIndex = HelperFunctions.ModuloIndex(i - j + offset, dataLength); // Circular indexing
                    sum += dataSpan[dataIndex] * kernelSpan[j];
                }
                result[i] = sum;
            }

            return result;
        }
    }
}
