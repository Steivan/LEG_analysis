
using LEG.MeteoSwiss.Abstractions.Models;
using LEG.PV.Core.Models;
using static LEG.PV.Core.Models.PvDataClass;
using static LEG.PV.Core.Models.PvPowerJacobian;

namespace LEG.PV.Data.Processor;

public class DataSimulator
{
    public static (List<PvRecord> dataRecords, List<bool> validRecords, int periodsPerHour) GetPvSimulatedRecords(
        PvModelParams pvParams, 
        double installedPower = 10000, 
        double siteLatitude = 46,
        double roofAzimuth = -30,
        double roofElevation = 20,
        double simulationsPeriod = 5,
        bool applyRandomNoise = false,
        bool applySnowDays = false,
        bool applyFoggyDays = false,
        bool applyOutliers = false)
    {
        double siteLongitude = 0;

        const double daysPerYears = 365.2422;
        const int hoursPerDay = 24;
        const int minutesPerHour = 60;
        const int periodsPerHour = 4;
        const int hoursPerBlock = 3;
        const int blocksPerDay = hoursPerDay / hoursPerBlock;
        const int minutesPerPeriod = minutesPerHour / periodsPerHour;
        const Double minutesPerYear = daysPerYears * hoursPerDay * minutesPerHour;

        // General model parameters => see also: SunGeometrySimulator.GetSolarGeometry and MeteoSimulator.UpdatedMeteoParameters
        const int startHour = 12;
        const int startMinute = 0;
        const double randomNoiseVariation = 0.1;
        var daysPerMonth = new List<int> { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        var averageSnowyowDaysPerMonth = new List<int> { 10, 10, 0, 0, 0, 0, 0, 0, 0, 0, 5, 10 };
        var minNewSnow = 1;
        var maxNewSnow = 20;
        var maxNewSnowRandom = 1 + maxNewSnow - minNewSnow;
        var averageFoggyDaysPerMonth = new List<int> { 10, 5, 0, 0, 0, 0, 0, 0, 0, 5, 10, 10 };
        var fogDissolveStartLo = 6;
        var fogDissolveStartHi = 8;
        var fogDissolveEndLo = 10;
        var fogDissolveEndHi = 14;
        var probabilityPeriodOutlier = 0.001;
        var probabilityHourOutlier = 0.001;
        var probabilityBlockOutlier = 0.001;

        var startBlock = startHour / hoursPerBlock; 
        var startBlockHour = startHour % hoursPerBlock; 
        var startPeriod = startMinute / minutesPerPeriod; // start at first period

        var sinRoofElevation = Math.Sin(roofElevation * Math.PI / 180.0);
        var cosRoofElevation = Math.Cos(roofElevation * Math.PI / 180.0);

        // Simulation period
        var now = DateTime.Now;
        var tomorrow = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
        var endDate = new DateTime(tomorrow.Year, tomorrow.Month, 1, startBlock * hoursPerBlock + startBlockHour, startPeriod * 15, 0).AddDays(-1); // last day of previous month
        var simulationDays= (int)Math.Ceiling(daysPerYears * simulationsPeriod);
        var startDate = now.AddDays(-simulationDays);
        var startYear = startDate.Year;
        if (startDate.Month == 12 && startDate.Day > 1)
        {
            startDate = new DateTime(startYear + 1, 1, 1, 0, 0, 0);
        }
        else if (startDate.Day > 1)
        {
            startDate = new DateTime(startYear, startDate.Month + 1, 1, 0, 0, 0); // first of first month post simulationsPeriod years ago
        }
        // Update
        startYear = startDate.Year;
        simulationDays = (endDate - startDate).Days + 1;

        // Initial values
        var random = new Random();
        var pvRecords = new List<PvRecord>();
        var validRecords = new List<bool>();
        PvSolarGeometry? solarGeometry = null;
        double cosOmegaYear, cosOmegaDay;
        MeteoParameters? meteoParameters = null;
        double weight;
        var firstSnowDay = -1;
        var lastSnowDay = -1;

        var initialize = true;
        for (int day = 0; day < simulationDays; day++)
        {
            var currentDate = startDate.AddDays(day);
            var monthIndex = currentDate.Month - 1;

            // Random snow period
            var dayOfMonth = currentDate.Day;
            if (dayOfMonth == 1)
            {
                firstSnowDay = -1;
                lastSnowDay = -1;
                if (averageSnowyowDaysPerMonth[monthIndex] > 0)
                {
                    var durationSnowDays = random.Next(0, 2 * averageSnowyowDaysPerMonth[monthIndex] + 1);
                    firstSnowDay = random.Next(1, daysPerMonth[monthIndex] - durationSnowDays + 2);
                    lastSnowDay = firstSnowDay + durationSnowDays - 1;
                }
            }
            var isSnowyDay = (firstSnowDay <= dayOfMonth && dayOfMonth <= lastSnowDay);
            var newSnow = applySnowDays ? (isSnowyDay ? minNewSnow + random.NextInt64(maxNewSnowRandom) : 0) : 0; // [cm]

            // Random foggy day
            var randomDayOfMonth = random.Next(1, daysPerMonth[monthIndex] + 1);
            var isFoggyDay = randomDayOfMonth <= averageFoggyDaysPerMonth[monthIndex];
            double fogDissolveStartHour = random.Next(fogDissolveStartLo, fogDissolveStartHi + 1);
            double fogDissolveEndHour = random.Next(fogDissolveEndLo, fogDissolveEndHi + 1);

            for (int block = startBlock; block < blocksPerDay; block++)
            {                   
                // Block outliers
                var blockOutlier = random.NextDouble() < probabilityBlockOutlier;

                for (var blockHour = startBlockHour; blockHour < hoursPerBlock; blockHour++)
                {  
                    // Hour outliers
                    var hourOutlier = random.NextDouble() < probabilityHourOutlier;

                    var hour = hoursPerBlock * block + blockHour;
                    var currentHour = currentDate.AddHours(hour);
                    for (int period = startPeriod; period < periodsPerHour; period++)
                    {
                        // Period outliers
                        var periodOutlier = random.NextDouble() < probabilityPeriodOutlier;
                        var isOutlier = blockOutlier || hourOutlier || periodOutlier;

                        var timeStamp = currentHour.AddMinutes(period * minutesPerPeriod);
                        var age = (double)(timeStamp - startDate).TotalMinutes / minutesPerYear;
                        var timeOfDay = hour + (double)period / periodsPerHour;

                        if (initialize)
                        {
                            var priorPeriodDateTime = timeStamp - TimeSpan.FromMinutes(minutesPerPeriod);
                            (solarGeometry, cosOmegaYear, cosOmegaDay) = SunGeometrySimulator.GetSolarGeometry(
                                startYear, priorPeriodDateTime,
                                siteLatitude, siteLongitude,
                                roofAzimuth, sinRoofElevation, cosRoofElevation);
                            (meteoParameters, weight) = MeteoSimulator.UpdatedMeteoParameters(  // meteo parameters for prior interval
                                priorPeriodDateTime, minutesPerPeriod,
                                meteoParameters,
                                solarGeometry, cosOmegaYear, cosOmegaDay,
                                false, 0,
                                false, 0, 24,
                                initialize: true);

                            initialize = false;
                        }

                        // Solar position
                        (solarGeometry, cosOmegaYear, cosOmegaDay) = SunGeometrySimulator.GetSolarGeometry(startYear, timeStamp, siteLatitude, siteLongitude, roofAzimuth, sinRoofElevation, cosRoofElevation);
                        var sinSunElevation = solarGeometry.SinSunElevation;

                        (meteoParameters, weight) = MeteoSimulator.UpdatedMeteoParameters(
                            timeStamp, minutesPerPeriod, 
                            meteoParameters,
                            solarGeometry, cosOmegaYear, cosOmegaDay,
                            applySnowDays && isSnowyDay, newSnow,
                            applyFoggyDays && isFoggyDay, fogDissolveStartHour, fogDissolveEndHour);

                        var calculatedPower = EffectiveCellPower(installedPower, periodsPerHour, solarGeometry, meteoParameters, age, pvParams);

                        // If applcable, add some noise to the measured power and apply outlier factor
                        var noiseFactor = 1.0 + (applyRandomNoise ? randomNoiseVariation * (random.NextDouble() - 0.5) : 0.0);
                        var outlierFactor = (applyOutliers && isOutlier) ? 1.5 : 1.0;
                        var measuredPower = (calculatedPower.PowerGRTWSF > 0 ? calculatedPower.PowerGRTWSF : 0.0) * noiseFactor * outlierFactor;

                        if (!isSnowyDay || outlierFactor > 1)
                        {
                            var DEBUG = 1;
                        }

                        pvRecords.Add(
                            new PvRecord(
                                timeStamp, 
                                pvRecords.Count,
                                solarGeometry,
                                meteoParameters,
                                weight: weight,  
                                age, measuredPower)
                            );
                        var checkedComputedPower = pvRecords.Last().ComputedPower(pvParams, installedPower, periodsPerHour);

                        var isValidRecord = (weight > 0) &&(!applySnowDays || !isSnowyDay) && (!applyFoggyDays || !isFoggyDay) && (!applyOutliers || !isOutlier);
                        validRecords.Add(isValidRecord);
                    }
                    startPeriod = 0; // after first hour, start at first period
                }
                startBlockHour = 0; // after first block, start at first block hour
            }
            startBlock = 0; // after first day, start at midnight
        }
        // Check period start date and period end date
        var firstRecordDate = pvRecords.First().Timestamp;
        var lastRecordDate = pvRecords.Last().Timestamp;

        var countFalse = validRecords.Count(v => v!=true);
        return (pvRecords, validRecords, periodsPerHour);
    }
}
