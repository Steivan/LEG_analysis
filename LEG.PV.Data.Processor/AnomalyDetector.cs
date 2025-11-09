using LEG.PV.Core.Models;
using static LEG.PV.Data.Processor.DataRecords;

namespace LEG.PV.Data.Processor
{
    public class AnomalyDetector
    {
        private static int[] GetRomboidIndices(int recordsPerDay, int hoursPerDay, int blocksPerDay,
            bool[] hasPeriodData, bool[] hasHourData, bool[] hasBlockData, 
            double[] periodRatios, double[] hourlyRatios, double[] blockRatios,
            int patternType = 0,
            bool relativeThreshold = true,
            int thresholdType = 2, 
            double loThreshold = 0.1, 
            double hiThreshold = 0.9)
        {
            var recordsPerHour = recordsPerDay / hoursPerDay;
            var recordsPerBlock = recordsPerDay / blocksPerDay;

            int[] GetIndices(int recordsCount, int stepSize, bool[] hasData, double[] ratios, double loBoundRatio, double hiBoundRatio)
            {

                int firstValidIndex = -1;
                int firstNonZeroIndex = -1;
                int firstXsLoBoundIndex = -1;
                int firstXsHiBoundIndex = -1;

                int lastValidIndex = -1;
                int lastNonZeroIndex = -1;
                int lastXsLoBoundIndex = -1;
                int lastXsHiBoundIndex = -1;

                for (int i = 0; i < recordsCount; i++)
                {
                    if (firstValidIndex == -1 && hasData[i])
                        firstValidIndex = i;
                    if (firstNonZeroIndex == -1 && ratios[i] > 0.0)
                        firstNonZeroIndex = i;
                    if (firstXsLoBoundIndex == -1 && ratios[i] >= loBoundRatio)
                        firstXsLoBoundIndex = i;
                    if (firstXsHiBoundIndex == -1 && ratios[i] >= hiBoundRatio)
                        firstXsHiBoundIndex = i;

                    int revIndex = recordsCount - 1 - i;
                    if (lastValidIndex == -1 && hasData[revIndex])
                        lastValidIndex = revIndex;
                    if (lastNonZeroIndex == -1 && ratios[revIndex] > 0.0)
                        lastNonZeroIndex = revIndex;
                    if (lastXsLoBoundIndex == -1 && ratios[revIndex] >= loBoundRatio)
                        lastXsLoBoundIndex = revIndex;
                    if (lastXsHiBoundIndex == -1 && ratios[revIndex] >= hiBoundRatio)
                        lastXsHiBoundIndex = revIndex;
                }

                return [
                    firstValidIndex * stepSize,
                    firstNonZeroIndex * stepSize,
                    firstXsLoBoundIndex * stepSize,
                    firstXsHiBoundIndex * stepSize,
                    lastValidIndex * stepSize,
                    lastNonZeroIndex * stepSize,
                    lastXsLoBoundIndex * stepSize,
                    lastXsHiBoundIndex * stepSize
                    ];
            }

            // Default: use maximum block ratio as a reference
            var maxRatio = !relativeThreshold ? 1.0 : thresholdType == 0 ? periodRatios.Max() : thresholdType == 1 ? hourlyRatios.Max() : blockRatios.Max();
            var loBoundRatio = maxRatio * loThreshold;
            var hiBoundRatio = maxRatio * hiThreshold;

            switch (patternType)
            {
                case 1:
                    return GetIndices(hoursPerDay, recordsPerHour, hasHourData, hourlyRatios, loBoundRatio, hiBoundRatio);      // block ranges above thresholds
                case 2:
                    return GetIndices(blocksPerDay, recordsPerBlock, hasBlockData, blockRatios, loBoundRatio, hiBoundRatio);    // hour ranges above thresholds
                default:
                    return GetIndices(recordsPerDay, 1, hasPeriodData, periodRatios, loBoundRatio, hiBoundRatio);               // record ranges above thresholds
            }
        }

        public static (List<int[]> diurnalIndicesList, int periodsPerDay, int indexOffset) GetDiurnalPatterns(
            List<PvRecord> pvRecords,
            double installedPower,
            PvModelParams pvModelParams,
            int patternType = 0,
            bool relativeThreshold = true,
            int thresholdType = 2, 
            double loThreshold = 0.1, 
            double hiThreshold = 0.9)
        {
            const double daysPerYear = 365.2522;
            const int hoursPerDay = 24;
            const int minutesPerHour = 60;
            const int blocksPerDay = 8;
            const int hoursPerBlock = hoursPerDay / blocksPerDay;

            var firstRecordDate = pvRecords.First().Timestamp;
            var secondRecordDate = pvRecords[1].Timestamp;
            var lastRecordDate = pvRecords.Last().Timestamp;

            var minutesPerPeriod = (secondRecordDate - firstRecordDate).Minutes;
            var periodsPerHour = minutesPerHour / minutesPerPeriod;
            var periodsPerDay = hoursPerDay * periodsPerHour;

            var diurnalIndicesList = new List<int[]>();

            var recordsCount = pvRecords.Count;
            var recordIndex = 0;
            var indexOffset = -1;
            while (recordIndex < recordsCount)
            {
                var pTheoretical = new double[periodsPerDay];
                var pMeasured = new double[periodsPerDay];
                var hasPeriodData = new bool[periodsPerDay];
                var hasHourData = new bool[hoursPerDay];
                var hasBlockData = new bool[blocksPerDay];

                // Collect data for Day
                var dayOfMonth = pvRecords[recordIndex].Timestamp.Day;
                while (recordIndex < recordsCount && pvRecords[recordIndex].Timestamp.Day == dayOfMonth)
                {
                    var record = pvRecords[recordIndex];
                    var timeOfDay = record.Timestamp.TimeOfDay;
                    var timeIndex = (timeOfDay.Hours * periodsPerHour) + (timeOfDay.Minutes / minutesPerPeriod);
                    if (indexOffset == -1)
                    {
                        indexOffset = timeIndex;
                    }
                    var age = (record.Timestamp - firstRecordDate).Days / daysPerYear;
                    pTheoretical[timeIndex] = PvJacobian.EffectiveCellPower(installedPower, record.GeometryFactor,
                        record.Irradiation, record.AmbientTemp, record.WindVelocity, age,
                        pvModelParams.Etha, pvModelParams.Gamma, pvModelParams.U0, pvModelParams.U1, pvModelParams.LDegr);
                    pMeasured[timeIndex] = record.MeasuredPower;
                    hasPeriodData[timeIndex] = pTheoretical[timeIndex] > 0;

                    recordIndex++;
                }

                // Get hourly rations
                var periodRatios = new double[periodsPerDay];
                var hourlySumTheoretical = new double[hoursPerDay];
                var hourlySumMeasured = new double[hoursPerDay];
                var hourlyRatios = new double[hoursPerDay];

                var blockSumTheoretical = new double[blocksPerDay];
                var blockSumMeasured = new double[blocksPerDay];
                var blockRatios = new double[blocksPerDay];

                for (var block = 0; block < blocksPerDay; block++)
                {
                    var sumBlockTheoretical = 0.0;
                    var sumBlockMeasured = 0.0;
                    for (var blockHour = 0; blockHour < hoursPerBlock; blockHour++)
                    {
                        var hour = block * hoursPerBlock + blockHour;
                        var sumHourTheoretical = 0.0;
                        var sumHourMeasured = 0.0;
                        for (var hIndex = 0; hIndex < periodsPerHour; hIndex++)
                        {
                            var timeIndex = hour * periodsPerHour + hIndex;
                            if (hasPeriodData[timeIndex])
                            {
                                var pTheor = pTheoretical[timeIndex];
                                var pMeas = pMeasured[timeIndex];

                                periodRatios[timeIndex] = pTheor > 0 ? pMeas / pTheor : 0.0;

                                sumHourTheoretical += pTheor;
                                sumHourMeasured += pMeas;

                                hasHourData[hour] = true;
                                hasBlockData[block] = true;
                            }
                        }
                        hourlySumTheoretical[hour] = sumHourTheoretical;
                        hourlySumMeasured[hour] = sumHourMeasured;
                        hourlyRatios[hour] = sumHourTheoretical > 0 ? sumHourMeasured / sumHourTheoretical : 0.0;

                        sumBlockTheoretical += sumHourTheoretical;
                        sumBlockMeasured += sumHourMeasured;
                    }
                    blockSumTheoretical[block] = sumBlockTheoretical;
                    blockSumMeasured[block] = sumBlockMeasured;
                    blockRatios[block] = sumBlockTheoretical > 0 ? sumBlockMeasured / sumBlockTheoretical : 0.0;
                }
                var maxPeriodRatio = periodRatios.Max();
                var maxHourRatio = hourlyRatios.Max();
                var maxBlockRatio = blockRatios.Max();

                var daySumTheoretical = blockSumTheoretical.Sum();
                var daySumMeasured = blockSumMeasured.Sum();
                var daySumRatio = daySumTheoretical > 0 ? daySumMeasured / daySumTheoretical : 0.0;

                diurnalIndicesList.Add(GetRomboidIndices(
                    periodsPerDay, hoursPerDay, blocksPerDay,
                    hasPeriodData, hasHourData, hasBlockData,
                    periodRatios, hourlyRatios, blockRatios,
                    patternType: patternType,
                    relativeThreshold: relativeThreshold, 
                    thresholdType: thresholdType, 
                    loThreshold: loThreshold, 
                    hiThreshold: hiThreshold)
                    ); 
            }

            return (diurnalIndicesList, periodsPerDay, indexOffset);
        }
        public static List<bool> ExcludeFoggyRecords(
            List<PvRecord> pvRecords,
            List<bool> initialValidRecords,
            double installedPower,
            PvModelParams pvModelParams,
            int patternType = 0,
            bool relativeThreshold = true,
            int thresholdType = 2, 
            double loThreshold = 0.1, 
            double hiThreshold = 0.9)
        {
            var recordsCount = pvRecords.Count;

            var (diurnalIndicesList, periodsPerDay, indexOffset) = GetDiurnalPatterns(
                pvRecords,
                installedPower,
                pvModelParams,
                patternType: patternType,
                relativeThreshold: relativeThreshold,
                thresholdType: thresholdType, 
                loThreshold: loThreshold, 
                hiThreshold: hiThreshold);

            var countDays = (pvRecords.Last().Timestamp - pvRecords.First().Timestamp).Days + 1;
            if (countDays == diurnalIndicesList.Count)
            {                                                               // Mark records outside valid diurnal patterns as invalid
                for (int day = 0; day < countDays; day++)
                {
                    var startIndex = day * periodsPerDay - indexOffset;
                    var diurnalIndices = diurnalIndicesList[day];
                    var firstValidIndex = diurnalIndices[3];                // first index with value >= hiThreshold
                    var lastValidIndex = diurnalIndices[6] - 1;             // last index with value > 0 
                    for (int i = 0; i < periodsPerDay; i++)
                    {
                        var recordIndex = startIndex + i;
                        if (recordIndex >= 0 && recordIndex < recordsCount)
                        {
                            if (i < firstValidIndex || i > lastValidIndex)
                            {
                                initialValidRecords[recordIndex] = false;
                            }
                        }
                    }
                }
            }

            return initialValidRecords;
        }
    }
}
