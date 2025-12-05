
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
        bool applyFoggyDays = false,
        bool applySnowDays = false,
        bool applyOutliers = false)
    {
        const double earthTilt = 23.4; // [degrees]
        const double daysPerYears = 365.2422;
        const int hoursPerDay = 24;
        const int minutesPerHour = 60;
        const int periodsPerHour = 4;
        const int minutesPerPeriod = minutesPerHour / periodsPerHour;
        const Double minutesPerYear = daysPerYears * hoursPerDay * minutesPerHour;

        const int startHour = 12;
        const int startMinute = 0;
        var startBlock = startHour / 3; // start at 6am
        var startBlockHour = startHour % 3;  // start at 6am
        var startPeriod = startMinute / minutesPerPeriod; // start at first period

        var sinRoofElevation = Math.Sin(roofElevation * Math.PI / 180.0);
        var cosRoofElevation = Math.Cos(roofElevation * Math.PI / 180.0);
        var diffuseGeometryFactor = (1.0 + cosRoofElevation) / 2;

        var now = DateTime.Now;
        var tomorrow = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
        var endDate = new DateTime(tomorrow.Year, tomorrow.Month, 1, startBlock * 3 + startBlockHour, startPeriod * 15, 0).AddDays(-1); // last day of previous month
        var startDate = now.AddDays(-(int)Math.Ceiling(daysPerYears * simulationsPeriod)); // first of first month post simulationsPeriod years ago
        if (startDate.Month == 12 && startDate.Day > 1)
        {
            startDate = new DateTime(startDate.Year + 1, 1, 1, 0, 0, 0);
        }
        else if (startDate.Day > 1)
        {
            startDate = new DateTime(startDate.Year, startDate.Month + 1, 1, 0, 0, 0);
        }
        var daysTotal = (endDate - startDate).Days + 1;
        var time0 = new DateTime(startDate.Year, 1, 1, 0, 0, 0);
        var startLag = (startDate - time0).Days;

        const double omegaYear = 2 * Math.PI / daysPerYears;
        const double omegaDay = 2 * Math.PI / hoursPerDay;

        var annualSolarAmplitude = earthTilt;
        var diurnalSolarAmplitude = 90.0 - siteLatitude;

        const double maxIrradiance = 1361;     // [W/m^2] Solar constant
        const double diffuseRadiationRatio = 0.3;
        const Double averagediffuseRadiation = maxIrradiance * diffuseRadiationRatio;
        const double maxDirectIrratiance = maxIrradiance - averagediffuseRadiation;
        const double weightPreviousIrradiance = 0.7;

        const double averageTemp = 15;          // [°C]
        const double annualTempAmplitude = 10;  // [°C]
        const double diurnalTempAmplitude = 5;  // [°C]

        const double maxWindSpeed = 150;     // [km/h]
        const double maxNewWindGust = 10;       // [km/h]
        const double windGustProbability = 0.1;
        const double weightPreviousWindSpeed = 0.95;

        const double randomNoiseStdDev = 0.1;

        var daysPerMonth     = new List<int> { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        var fogDaysPerMonth  = new List<int> { 10,  5,  0,  0,  0,  0,  0,  0,  0,  5, 10, 10 };
        var snowDaysPerMonth = new List<int> { 10, 10,  0,  0,  0,  0,  0,  0,  0,  0,  5, 10 };
        var pPeriodOutlier = 0.001;
        var pHourOutlier   = 0.001;
        var pBlockOutlier  = 0.001;

        // initial values
        double previousDirectIrradiance = maxDirectIrratiance / 2;
        double windSpeed = 10;
        var random = new Random();
        var pvRecords = new List<PvRecord>();
        var validRecords = new List<bool>();
        var firstSnowDay = -1;
        var lastSnowDay = -1;
        for (int day = 0; day < daysTotal; day++)
        {
            //var age = (double)day / daysPerYears;
            var currentDate = startDate.AddDays(day);
            var monthIndex = currentDate.Month - 1;

            // Random fog day
            var randomDayOfMonth = random.Next(1, daysPerMonth[monthIndex] + 1);
            var isFoggyDay = randomDayOfMonth <= fogDaysPerMonth[monthIndex];
            double fogDissolveStartHour = random.Next(6, 8 + 1);
            double fogDissolveEndHour = random.Next(10, 14 + 1);

            // Random snow period
            var dayOfMonth = currentDate.Day;
            if (dayOfMonth == 1)
            {
                firstSnowDay = -1;
                lastSnowDay = -1;
                if (snowDaysPerMonth[monthIndex] > 0)
                {
                    var durationSnowDays = random.Next(0, 2 * snowDaysPerMonth[monthIndex] + 1);
                    firstSnowDay = random.Next(1, daysPerMonth[monthIndex] - durationSnowDays + 2);
                    lastSnowDay = firstSnowDay + durationSnowDays - 1;
                }
            }
            var isSnowyDay = (firstSnowDay <= dayOfMonth && dayOfMonth <= lastSnowDay);
            var snowDepth = applySnowDays ? (isSnowyDay ? 20 : 0) : 0; // [cm]

            var cosOmegaYear = Math.Cos(omegaYear * (startLag + day));
            var annualZenithangle = 90 + annualSolarAmplitude * cosOmegaYear;      // zenith angle of the sun is largest in winter
            for (int block = startBlock; block < 8; block++)
            {                   
                // Block outliers
                var blockOutlier = random.NextDouble() < pBlockOutlier;
                for (var blockHour = startBlockHour; blockHour < 3; blockHour++)
                {  
                    // Hour outliers
                    var hourOutlier = random.NextDouble() < pHourOutlier;

                    var hour = 3 * block + blockHour;
                    var currentHour = currentDate.AddHours(hour);
                    for (int period = startPeriod; period < periodsPerHour; period++)
                    {
                        // Period outliers
                        var periodOutlier = random.NextDouble() < pPeriodOutlier;
                        var isOutlier = blockOutlier || hourOutlier || periodOutlier;

                        var timeStamp = currentHour.AddMinutes(period * minutesPerPeriod);
                        var age = (timeStamp - startDate).TotalMinutes / minutesPerYear;
                        var timeOfDay = hour + (double)period / periodsPerHour;

                        var cosOmegaDay = Math.Cos(omegaDay * timeOfDay);
                        var diurnalZenithAngle = (90 - siteLatitude) * cosOmegaDay; // zenith angle of the sun is largest at night

                        // Geometry factor combines annual and diurnal variations
                        var sunAzimuth = (timeOfDay - 12.0) * 15.0;
                        var sunZenithAngle = annualZenithangle + diurnalZenithAngle; 
                        var sunElevation = 90 - sunZenithAngle;
                        var sinSunElevation = Math.Cos(sunZenithAngle * Math.PI / 180.0);
                        var directGeometryFactor = Math.Cos(sunElevation * Math.PI / 180.0) * cosRoofElevation * Math.Cos((sunAzimuth - roofAzimuth) * Math.PI / 180.0)  // theta = 90 - elevation => Cos() <-> Sin()
                            + Math.Sin(sunElevation * Math.PI / 180.0) * sinRoofElevation;

                        // Update radiation with some randomness
                        var r = random.NextDouble();
                        var newRandomDirectIrradiance = previousDirectIrradiance * weightPreviousIrradiance + (1.0 - weightPreviousIrradiance) * maxDirectIrratiance * r;  // hypothetical irradiance as a function of cloudiness
                        previousDirectIrradiance = newRandomDirectIrradiance;
                        var diffuseRadiation = averagediffuseRadiation + (maxDirectIrratiance - newRandomDirectIrradiance) * 0.1;
                        var globalHorizontalRadiation = sinSunElevation > 0.0 ? newRandomDirectIrradiance * sinSunElevation + diffuseRadiation : 0.0;
                        var diffuseHorizontalRadiation = sinSunElevation > 0.0 ? diffuseRadiation : 0.0;
                        var sunshineDuration = sinSunElevation > 0 ? (int) (newRandomDirectIrradiance / maxDirectIrratiance * minutesPerPeriod) : 0; // [min]
                        var weight = sinSunElevation > 0 ? 1E-3 + Math.Pow(newRandomDirectIrradiance / maxDirectIrratiance, 3) : 0.0;

                        // Calculate ambient temperature
                        var ambientTemp = averageTemp - annualTempAmplitude * cosOmegaYear - diurnalTempAmplitude * cosOmegaDay; // [°C]

                        // Update wind velocity with some randomness
                        var newWindGustVelocity = (random.NextDouble() < windGustProbability) ? random.NextDouble() * maxNewWindGust : 0.0;
                        var newWindSpeed = windSpeed * weightPreviousWindSpeed + newWindGustVelocity;
                        windSpeed = Math.Max(0.0, Math.Min(maxWindSpeed, newWindSpeed));

                        // Calculate theoretical effective power
                        var meteoParameters = new MeteoParameters(
                            Time: timeStamp,
                            Interval: TimeSpan.FromMinutes(minutesPerPeriod),
                            SunshineDuration: sunshineDuration,
                            DirectRadiation: null,
                            DirectNormalIrradiance: null,
                            GlobalRadiation: globalHorizontalRadiation,
                            DiffuseRadiation: diffuseHorizontalRadiation,
                            Temperature: ambientTemp,
                            WindSpeed: windSpeed,
                            WindDirection: null,
                            SnowDepth: snowDepth,
                            RelativeHumidity: null,
                            DewPoint: null,
                            DirectRadiationVariance: null
                            );
                        var calculatedPower = EffectiveCellPower(installedPower, periodsPerHour, 
                            new PvGeometryFactors(
                                directGeometryFactor, 
                                diffuseGeometryFactor, 
                                sinSunElevation),
                            meteoParameters, age, pvParams);

                        // Add some noise to the measured power
                        var noise = calculatedPower.PowerGRTW * randomNoiseStdDev * (random.NextDouble() - 0.5);

                        var measuredPower = calculatedPower.PowerGRTW + (applyRandomNoise ? noise : 0);

                        // Apply weather and outlier effects
                        var isFoggyPeriod = isFoggyDay && hour < fogDissolveEndHour;
                        if (applyFoggyDays)
                        {
                            if (isFoggyPeriod)
                            {
                                var fogFactor = hour <= fogDissolveStartHour ? 0.0 : hour >= fogDissolveEndHour ? 1.0 : (hour - fogDissolveStartHour) / (fogDissolveEndHour - fogDissolveStartHour);
                                measuredPower *= fogFactor; // Reduced power in foggy mornings
                            }
                        }

                        if (applySnowDays)
                        {
                            if (isSnowyDay)
                            {
                                measuredPower = 0.0; // No power generation on snowy days
                            }
                        }

                        if (applyOutliers)
                        {
                            if (isOutlier)
                            {
                                measuredPower *= 1.5; // Distorted power for outliers
                            }
                        }

                        var directHorizontalRadiation = globalHorizontalRadiation - diffuseHorizontalRadiation;
                        var directNormalIrradiance = sinSunElevation > 0 ? directHorizontalRadiation / sinSunElevation : 0.0; // used in forecasting models

                        pvRecords.Add(
                            new PvRecord(
                                timeStamp, 
                                pvRecords.Count, 
                                new PvGeometryFactors(
                                    directGeometryFactor,
                                    diffuseGeometryFactor,
                                    sinSunElevation
                                    ),
                                new MeteoParameters(
                                    Time: timeStamp,
                                    Interval: TimeSpan.FromMinutes(minutesPerPeriod),
                                    SunshineDuration: sunshineDuration,
                                    DirectRadiation: directHorizontalRadiation,
                                    DirectNormalIrradiance: directNormalIrradiance,
                                    GlobalRadiation: globalHorizontalRadiation,
                                    DiffuseRadiation: diffuseHorizontalRadiation,
                                    Temperature: ambientTemp,
                                    WindSpeed: windSpeed,
                                    WindDirection: null,
                                    SnowDepth: snowDepth,
                                    RelativeHumidity: null,
                                    DewPoint: null,
                                    DirectRadiationVariance: null
                                    ),
                                weight: weight,  
                                age, measuredPower)
                            );
                        var checkedComputedPower = pvRecords.Last().ComputedPower(pvParams, installedPower, periodsPerHour);

                        var isValidRecord = (weight > 0) &&(!applySnowDays || !isSnowyDay) && (!applyFoggyDays || !isFoggyPeriod) && (!applyOutliers || !isOutlier);
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
