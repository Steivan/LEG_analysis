using LEG.PV.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LEG.PV.Data.Processor.DataRecords;

namespace LEG.PV.Data.Processor
{
    internal class AnomalyDetector
    {
        public static List<bool> ExcludeFoggyRecords(
            List<PvRecord> pvRecords,
            double installedPower,
            PvModelParams pvModelParams)
        {
            const double daysPerYear = 365.2522;

            var firstRecordDate = pvRecords.First().Timestamp;
            var secondRecordDate = pvRecords[1].Timestamp;
            var lastRecordDate = pvRecords.Last().Timestamp;

            var minutesPerPeriod = (secondRecordDate - firstRecordDate).Minutes;
            var recordsPerHour = 60 / minutesPerPeriod;
            var recordsPerDay = 24 * recordsPerHour;

            var recordsCount = pvRecords.Count;
            var validRecords = new List<bool>(new bool[recordsCount]);
            var recordIndex = 0;
            while (recordIndex < recordsCount)
            {
                var pTheoretical = new double[recordsPerDay];
                var pMeasured = new double[recordsPerDay];
                var hasData = new bool[recordsPerDay];

                // Collect data for Day
                var dayOfMonth = pvRecords[recordIndex].Timestamp.Day;
                while (recordIndex < recordsCount && pvRecords[recordIndex].Timestamp.Day == dayOfMonth)
                {
                    var record = pvRecords[recordIndex];
                    var timeOfDay = record.Timestamp.TimeOfDay;
                    var timeIndex = (timeOfDay.Hours * recordsPerHour) + (timeOfDay.Minutes / minutesPerPeriod);
                    var age = (record.Timestamp - firstRecordDate).Days / daysPerYear;
                    pTheoretical[timeIndex] = PvJacobian.EffectiveCellPower(installedPower, record.GeometryFactor, 
                        record.Irradiation,  record.AmbientTemp, record.WindVelocity, age,
                        pvModelParams.Etha, pvModelParams.Gamma, pvModelParams.U0, pvModelParams.U1, pvModelParams.LDegr);
                    pMeasured[timeIndex] = record.MeasuredPower;
                    hasData[timeIndex] = pTheoretical[timeIndex] > 0;

                    recordIndex++;
                }

                // Get hourly rations
                var periodRatios = new double[recordsPerDay];
                var hourlySumTheoretical = new double[24];
                var hourlySumMeasured = new double[24];
                var hourlyRatios = new double[24];

                var blockSumTheoretical = new double[8];
                var blockSumMeasured = new double[8];
                var blockRatios = new double[8];

                for (var block = 0; block < 3; block++)
                {
                    var sumBlockTheoretical = 0.0;
                    var sumBlockMeasured = 0.0;
                    for (var blockHour = 0; blockHour < 24; blockHour++)
                    {
                        var hour = block * 3 + blockHour;
                        var sumHourTheoretical = 0.0;
                        var sumHourMeasured = 0.0;
                        for (var hIndex = 0; hIndex < recordsPerHour; hIndex++)
                        {
                            var timeIndex = hour * recordsPerHour + hIndex;
                            if (hasData[timeIndex])
                            {
                                var pTheor = pTheoretical[timeIndex];
                                var pMeas = pMeasured[timeIndex];

                                periodRatios[timeIndex] = pTheor > 0 ? pMeas / pTheor : 0.0;

                                sumHourTheoretical += pTheor;
                                sumHourMeasured += pMeas;
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
                var daySumRatios = daySumTheoretical > 0 ? daySumMeasured / daySumTheoretical : 0.0;



            }

            return validRecords;
        }
    }
}
