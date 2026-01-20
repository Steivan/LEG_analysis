
using MathNet.Numerics.Statistics;

namespace CalibrationApp.Helpers
{
    internal class Normalize2DArrays
    {
        internal static (double[] weekdayFactors, double[,] modelArray) GetModelArray(double[,] mean)
        { 
            int dim0 = mean.GetLength(0);
            int dim1 = mean.GetLength(1);

            var normalizedAggregatePattern = new double[dim1];
            var means0 = new double[dim0];
            for (int i = 0; i < dim0; i++)
            {
                var periodPattern = Convert2DArray.GetRow(mean, i, dim1);
                var periodMean = periodPattern.Mean();
                means0[i] = periodMean;
                var weight = 1.0 / periodMean / dim0;
                for (int j = 0; j < dim1; j++)
                {
                    normalizedAggregatePattern[j] += periodPattern[j] * weight;
                }
            }

            var modelArray = new double[dim0, dim1];
            for (int i = 0; i < dim0; i++)
            {
                for (int j = 0; j < dim1; j++)
                {
                    modelArray[i, j] = means0[i] * normalizedAggregatePattern[j];
                }
            }

            return (normalizedAggregatePattern, modelArray);
        }
    
    }
}
