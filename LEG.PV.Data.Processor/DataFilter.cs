using static LEG.PV.Data.Processor.DataRecords;
using static LEG.PV.Data.Processor.AnomalyDetector;

namespace LEG.PV.Data.Processor
{
    public class DataFilter
    {
        public static List<bool> ExcludeFoggyRecords(
            List<PvRecord> pvRecords,
            List<bool> initialValidRecords,
            double installedPower,
            PvModelParams pvModelParams,
            int patternType = 0,
            bool relativeThreshold = true,      // use a relative threshold for detecting records impacted by (morning) fog
            int thresholdType = 2,              // only used if relativeThreshold = true 
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
        public static List<bool> ExcludeSnowyRecords(
            List<PvRecord> pvRecords,
            List<bool> initialValidRecords,
            double installedPower,
            PvModelParams pvModelParams,
            int patternType = 0,
            bool relativeThreshold = false,     // use an absolute threshold for detecting records impacted by snow covering the roof
            int thresholdType = 2,              // only used if relativeThreshold = true
            double loThreshold = 0.1,
            double hiThreshold = 0.8)
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
                    var lastValidIndex = diurnalIndices[4] - 1;             // last  index with value >= hiThreshold
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
        public static List<bool> ExcludeOutlierRecords(
            List<PvRecord> pvRecords,
            List<bool> initialValidRecords,
            double installedPower,
            PvModelParams pvModelParams,
            double periodThreshold = 2.0,
            double hourlyThreshold = 1.75,
            double blockThreshold = 1.5)
        {
            var recordsCount = pvRecords.Count;

            var (periodRatiosList, hourlyRatiosList, blockRatiosList, periodsPerDay, hoursPerDay, blocksPerDay, indexOffset) = CalculateDiurnalRatios(
                pvRecords,
                installedPower,
                pvModelParams);

            var periodsPerHour = periodsPerDay / hoursPerDay;
            var periodsPerBlock = periodsPerDay / blocksPerDay;

            var countDays = (pvRecords.Last().Timestamp - pvRecords.First().Timestamp).Days + 1;
            if (countDays == periodRatiosList.Count)
            {                                                               // Mark records outside valid diurnal patterns as invalid
                for (int day = 0; day < countDays; day++)
                {
                    var startIndex = day * periodsPerDay - indexOffset;
                    var (hasPeriodData, periodRatios) = periodRatiosList[day];
                    var (hasHourlyData, hourlyRatios) = hourlyRatiosList[day];
                    var (hasBlockData, blockRatios) = blockRatiosList[day];
                    for (var periodIndex = 0; periodIndex < periodsPerDay; periodIndex++)
                    {
                        var hourlyIndex = periodIndex / periodsPerHour;
                        var blockIndex = periodIndex / periodsPerBlock;
                        var recordIndex = startIndex + periodIndex;
                        if (periodRatios[periodIndex] > periodThreshold || hourlyRatios[hourlyIndex] > hourlyThreshold || blockRatios[blockIndex] > blockThreshold)
                        {
                            initialValidRecords[recordIndex] = false;
                        }
                    }
                }
            }

            return initialValidRecords;
        }
    }
}
