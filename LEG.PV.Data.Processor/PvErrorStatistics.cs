using static LEG.PV.Data.Processor.DataRecords;
using static LEG.PV.Core.Models.PvJacobian;
using System.ComponentModel;


namespace LEG.PV.Data.Processor
{
    public static class PvErrorStatistics
    {
        public static List<double> GetErrorList(
            List<PvRecord> pvRecords,
            List<bool>? initialValidRecords,
            double installedPower,
            PvModelParams pvModelParams)
        {
            initialValidRecords ??= pvRecords.Select(r => r.GeometryFactor > 0.0).ToList();

            var errorList = new List<double>();
            for (var recordIndex = 0; recordIndex < pvRecords.Count; recordIndex++) 
            {
                if ( !initialValidRecords[recordIndex] )
                    continue;
                var pvRecord = pvRecords[recordIndex];
                var modeledPower = EffectiveCellPower(
                    installedPower,
                    pvRecord.GeometryFactor,
                    pvRecord.Irradiation,
                    pvRecord.AmbientTemp,
                    pvRecord.WindVelocity,
                    pvRecord.Age,
                    pvModelParams.Etha,
                    pvModelParams.Gamma,
                    pvModelParams.U0,
                    pvModelParams.U1,
                    pvModelParams.LDegr
                    );
                errorList.Add(pvRecord.MeasuredPower - modeledPower);
            }

            return errorList;
        }

        public static (double minError, double maxError, double meanError) BaseLineStatistics(
            List<PvRecord> pvRecords,
            List<bool>? initialValidRecords,
            double installedPower,
            PvModelParams pvModelParams)
        {
            var errorList = GetErrorList(
                pvRecords,
                initialValidRecords,
                installedPower,
                pvModelParams
                );

            var minError = double.MaxValue;
            var maxError = double.MinValue;
            var summedSquaredErrors = 0.0;
            var countValidRecords = 0;
            foreach (var error in errorList)
            {
                minError = Math.Min(minError, error);
                maxError = Math.Max(maxError, error);
                summedSquaredErrors += error * error;
                countValidRecords++;
            }

            var meanError = countValidRecords > 1 ? Math.Sqrt(summedSquaredErrors / (countValidRecords - 1)) : double.NaN;

            return (minError, maxError, meanError);
        }
        public static double ComputeMeanError(
            List<PvRecord> pvRecords,
            List<bool>? initialValidRecords,
            double installedPower,
            PvModelParams pvModelParams)
        {
            var (_, _, meanError) = BaseLineStatistics(
                pvRecords,
                initialValidRecords,
                installedPower,
                pvModelParams
                );

            return meanError;
        }


        public static (double minError, double maxError, double meanError, 
            double binSize, double[] binCenter, int[] binCount) 
            ComputeHistograms(
            List<PvRecord> pvRecords,
            List<bool>? initialValidRecords,
            double installedPower,
            PvModelParams pvModelParams,
            int countOfBins = 100)
        {
            initialValidRecords ??= pvRecords.Select(r => r.GeometryFactor > 0.0).ToList();

            var errorList = GetErrorList(
                pvRecords,
                initialValidRecords,
                installedPower,
                pvModelParams
                );

            var minError = errorList.Min();
            var maxError = errorList.Max();
            var binSize = (maxError - minError) / (countOfBins - 1);
            var lowerBound = minError - binSize / 2;
            var binCenters = new DoubleConverter[countOfBins].Select((v,i) => minError + binSize * i).ToArray();
            var binCounts = new int[countOfBins];

            var summedSquaredErrors = 0.0;
            var countValidRecords = 0;
            foreach (var error in errorList)
            {
                summedSquaredErrors += error * error;
                countValidRecords++;

                var binIndex = (int)((error -  lowerBound) / binSize);
                binCounts[binIndex]++;
            }

            var meanError = countValidRecords > 1 ? Math.Sqrt(summedSquaredErrors / (countValidRecords - 1)) : double.NaN;

            return (minError, maxError, meanError, binSize, binCenters, binCounts);
        }

    public static List<double> ComputeQuantiles(
        List<PvRecord> pvRecords,
        List<bool>? initialValidRecords,
        double installedPower,
        PvModelParams pvModelParams,
        List<double> pCumulative)
        {
            initialValidRecords ??= pvRecords.Select(r => r.GeometryFactor > 0.0).ToList();

            var errorList = GetErrorList(
                pvRecords,
                initialValidRecords,
                installedPower,
                pvModelParams
                );
            errorList.Sort();

            var countOfRecords = errorList.Count;
            var countOfQuantiles = pCumulative.Count;
            var quantileList = pCumulative.Select(v => 0.0).ToList();
            for (var errorIndex=0; errorIndex<countOfRecords; errorIndex++)
            {
                var p = (double)errorIndex / countOfRecords;
                for (var pIndex=0; pIndex<countOfQuantiles; pIndex++)
                {
                    if (p <= pCumulative[pIndex])
                    {
                        quantileList[pIndex] = errorList[errorIndex];
                    }
                }
            }

            return quantileList;
        }
    }
}
