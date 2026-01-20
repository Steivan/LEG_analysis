using LEG.PV.Core.Models.Structures;
using LEG.PV.Data.Processor.Interfaces;
using static LEG.PV.Core.Models.PvDataClass;
using static LEG.PV.Core.Models.PvProductionModel.PvPowerJacobian;
using static LEG.PV.Data.Processor.Simulator.SimulatorParameters;

namespace LEG.PV.Data.Processor.Simulator
{
    internal class PvProductionSimulator
    {
        internal static (Dictionary<DateTime, PvRecord>, Dictionary<DateTime, bool>) GetPvSimulatedRecordsDictionary(
            DateTime startTime, 
            DateTime endTime, 
            int minutesPerPeriod,
            PvModelParams pvParams,
            double siteLatitude = 46,
            double siteLongitude = 10,
            double installedPower = 10000,
            double roofAzimuth = -30,
            double roofElevation = 20,
            bool applyRandomNoise = false,
            bool applySnowDays = false,
            bool applyFoggyDays = false,
            bool applyOutliers = false)
        {
            if (60 % minutesPerPeriod != 0)
            {
                throw new ArgumentException("minutesPerPeriod must be a divisor of 60.");
            }

            var periodsPerHour = 60 / minutesPerPeriod;
            var interval = TimeSpan.FromMinutes(minutesPerPeriod);
            startTime = NormalizedDateTime(startTime, minutesPerPeriod);
            endTime = NormalizedDateTime(endTime.AddMinutes(minutesPerPeriod - 1), minutesPerPeriod);
            var startYear = startTime.Year;
            var sampleCount = 1 + (int)((endTime - startTime) / interval);

            var meteoSeries = MeteoRandomSeriesGenerator.GetMeteoSampleDictionary(
                startTime, interval, sampleCount,
                siteLatitude: siteLatitude,
                siteLongitude: siteLongitude,
                applySnowDays: applySnowDays,
                applyFoggyDays: applyFoggyDays,
                applyOutliers: applyOutliers);

            var random = new Random();

            int block = -1;
            int blockHour = -1;
            var blockOutlier = false;
            var hourOutlier = false;

            var pvRecordsDictionary = new Dictionary<DateTime, PvRecord>();
            var pvValidReordDictionary = new Dictionary<DateTime, bool>();
            for (int i = 0; i < sampleCount; i++)
            {
                var timestamp = startTime + TimeSpan.FromMinutes(i * minutesPerPeriod);
                var age = (timestamp - startTime).TotalDays / daysPerYears;
                var newBlock = timestamp.Hour / hoursPerBlock;
                var newBlockHour = timestamp.Hour % hoursPerBlock;
                var period = timestamp.Minute / minutesPerPeriod;

                if (newBlock != block)
                {
                    block = newBlock;
                    blockOutlier = random.NextDouble() < probabilityBlockOutlier;
                }
                if (newBlockHour != blockHour)
                {
                    blockHour = newBlockHour;
                    hourOutlier = random.NextDouble() < probabilityHourOutlier;
                }
                var periodOutlier = random.NextDouble() < probabilityPeriodOutlier;
                var isOutlier = blockOutlier || hourOutlier || periodOutlier;

                var meteoParams = meteoSeries[timestamp];
                var weight = 1E-3 + Math.Pow((meteoParams.DirectNormalIrradiance?? 0.0) / maxDirectIrratiance, 3);

                var solarGeometry = SolarGeometryRecordSimulator.GetSimulatedPvSolarGeometry(
                    startYear: startYear,
                    timeStamp: timestamp,
                    siteLatitude: siteLatitude,
                    siteLongitude: siteLongitude,
                    roofAzimuth: roofAzimuth,
                    roofElevation: roofElevation
                );

                var calculatedPower = EffectiveCellPower(installedPower, periodsPerHour, solarGeometry, meteoParams, age, pvParams);
                var isSnowyDay = calculatedPower.PowerGRTWS != calculatedPower.PowerGRTW;
                var isFoggyDay = calculatedPower.PowerGRTWSF != calculatedPower.PowerGRTWS;

                // If applcable, add some noise to the measured power and apply outlier factor
                var noiseFactor = 1.0 + (applyRandomNoise ? randomNoiseVariation * (random.NextDouble() - 0.5) : 0.0);
                var outlierFactor = (applyOutliers && isOutlier) ? 1.5 : 1.0;
                var measuredPower = (calculatedPower.PowerGRTWSF > 0 ? calculatedPower.PowerGRTWSF : 0.0) * noiseFactor * outlierFactor;

                // Create PvRecord
                var pvRecord = new PvRecord(
                    timestamp: timestamp,
                    index: i,
                    geometryFactors: solarGeometry,
                    meteoParameters: meteoParams,
                    weight: weight,
                    Math.Round(age, 2),
                    Math.Round(measuredPower, 1)
                );
                //var checkedComputedPower = pvRecord.ComputedPower(pvParams, installedPower, periodsPerHour);

                var isValidRecord = (weight > 0) && (!applySnowDays || !isSnowyDay) && (!applyFoggyDays || !isFoggyDay) && (!applyOutliers || !isOutlier);

                pvRecordsDictionary[timestamp] = pvRecord;
                pvValidReordDictionary[timestamp] = isValidRecord;
            }

            return (pvRecordsDictionary, pvValidReordDictionary);
        }

        private static DateTime NormalizedDateTime(DateTime dateTime, int minutesPerPeriod)
        {
            var minute = (dateTime.Minute / minutesPerPeriod) * minutesPerPeriod;
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, minute, 0, DateTimeKind.Utc);
        }
    }
}
