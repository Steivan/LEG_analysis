
using static LEG.PV.Data.Processor.DataRecords;
using static LEG.PV.Core.Models.PvJacobian;

namespace LEG.PV.Data.Processor;

public class DataSimulator
{
    public static (List<PvRecord> dataRecords, List<bool> validRecords) GetPvSimulatedRecords(PvModelParams pvParams, double installedPower, double simulationsPeriod = 5)
    {
        const double daysPerYears = 365.2422;
        const int hoursPerDay = 24;
        const int periodsPerHour = 6;
        const int minutesPerPeriod = 60 / periodsPerHour;

        var now = DateTime.Now;
        var tomorrow = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
        var endDate = new DateTime(tomorrow.Year, tomorrow.Month, 1, 0, 0, 0).AddDays(-1); // last day of previous month
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

        const double annualSolarAmplitude = 0.1;
        const double diurnalSolarAmplitude = 0.9;

        const double maxIrradiance = 1361;     // [W/m^2] Solar constant
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
        var snowDaysPerMonth = new List<int> { 10, 10, 0, 0, 0, 0, 0, 0, 0, 0, 5, 10 };
        var pPeriodOutlier = 0.001;
        var pHourOutlier   = 0.001;
        var pBlockOutlier  = 0.001;

        double etha = pvParams.Etha;
        double gamma = pvParams.Gamma;
        double u0 = pvParams.U0;
        double u1 = pvParams.U1;
        double lDegr = pvParams.LDegr;

        // initial values
        var globalHorizontalIrradiance = 500.0;
        var diffuseHorizontalIrradiance = 0.0;
        double windSpeed = 10;
        var random = new Random();
        var pvRecords = new List<PvRecord>();
        var validRecords = new List<bool>();
        var firstSnowDay = -1;
        var lastSnowDay = -1;
        for (int day = 0; day < daysTotal; day++)
        {
            var age = (double)day / daysPerYears;
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
            var snowDepth = isSnowyDay ? 20.0 : 0.0; // [cm]


            var annualVariation = -Math.Cos(omegaYear * (startLag + day));
            for (int block = 0; block < 8; block++)
            {                   
                // Block outliers
                var blockOutlier = random.NextDouble() < pBlockOutlier;
                for (var blockHour = 0; blockHour < 3; blockHour++)
                {  
                    // Hour outliers
                    var hourOutlier = random.NextDouble() < pHourOutlier;

                    var hour = 3 * block + blockHour;
                    var currentHour = currentDate.AddHours(hour);
                    for (int period = 0; period < periodsPerHour; period++)
                    {
                        // Period outliers
                        var periodOutlier = random.NextDouble() < pPeriodOutlier;
                        var isOutlier = blockOutlier || hourOutlier || periodOutlier;

                        var timeStamp = currentHour.AddMinutes(period * minutesPerPeriod);
                        var timeOfDay = (double)hour + period / periodsPerHour;

                        var diurnalVariation = -Math.Cos(omegaDay * timeOfDay);

                        // Geometry factor combines annual and diurnal variations
                        var directGeometryFactor = Math.Max(0.0, annualSolarAmplitude * annualVariation + diurnalSolarAmplitude * diurnalVariation); // Simplified model
                        var diffuseGeometryFactor = 1.0; // Simplified model        TODO: improve diffuse model
                        var cosSunElevation = 1.0; // Simplified model, assuming assuming direct irradiation is G_DNI and not G_DHI

                        // Update irradiation with some randomness
                        var newRandomIrradiance = globalHorizontalIrradiance * weightPreviousIrradiance + (1.0 - weightPreviousIrradiance) * random.NextDouble() * maxIrradiance;
                        globalHorizontalIrradiance = Math.Max(0.0, Math.Min(maxIrradiance, newRandomIrradiance)); // Smooth changes
                        var sunshineDuration = Math.Max(0.0, Math.Min(1.0, globalHorizontalIrradiance / cosSunElevation / maxIrradiance)) * minutesPerPeriod; // [min]

                        // Calculate ambient temperature
                        var ambientTemp = averageTemp + annualTempAmplitude * annualVariation + diurnalTempAmplitude * diurnalVariation; // [°C]

                        // Update wind velocity with some randomness
                        var newWindGustVelocity = (random.NextDouble() < windGustProbability) ? random.NextDouble() * maxNewWindGust : 0.0;
                        var newWindSpeed = windSpeed * weightPreviousWindSpeed + newWindGustVelocity;
                        windSpeed = Math.Max(0.0, Math.Min(maxWindSpeed, newWindSpeed));

                        // Calculate theoretical effective power
                        var calculatedPower = EffectiveCellPower(installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                            globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, ambientTemp, windSpeed, snowDepth, age,
                            ethaSys: etha, gamma: gamma, u0: u0, u1: u1, lDegr: lDegr);

                        // Add some noise to the measured power
                        var noise = calculatedPower * randomNoiseStdDev * (random.NextDouble() - 0.5);

                        var measuredPower = calculatedPower + noise;
                        var isFoggyPeriod = isFoggyDay && hour < fogDissolveEndHour;
                        if (isFoggyPeriod)
                        {
                            var fogFactor = hour <= fogDissolveStartHour ? 0.0 : hour >= fogDissolveEndHour ? 1.0 : (hour - fogDissolveStartHour) / (fogDissolveEndHour - fogDissolveStartHour);
                            measuredPower *= fogFactor; // Reduced power in foggy mornings
                        }
                        if (isSnowyDay)
                        {
                            measuredPower = 0.0; // No power generation on snowy days
                        }
                        if (isOutlier)
                        {
                            measuredPower *= 1.5; // Distorted power for outliers
                        }

                        pvRecords.Add(
                            new PvRecord(
                                timeStamp, 
                                pvRecords.Count, 
                                directGeometryFactor,
                                diffuseGeometryFactor,
                                cosSunElevation,
                                globalHorizontalIrradiance,               // Direct irradiation
                                sunshineDuration,                                       // Sunshine duration not modeled
                                diffuseHorizontalIrradiance,                        // Diffuse irradiation not modeled
                                ambientTemp, 
                                windSpeed, 
                                isSnowyDay ? 20.0 : 0.0,                 // Snow depth
                                weight: 1.0,                    // TODO: implement weighting
                                age, measuredPower)
                            );
                        var checkedComputedPower = pvRecords.Last().ComputedPower(pvParams, installedPower);

                        var isValidRecord = !isSnowyDay && !isFoggyPeriod && !isOutlier;
                        validRecords.Add(isValidRecord);
                    }
                }
            }
        }
        // Check period start date and period end date
        var firstRecordDate = pvRecords.First().Timestamp;
        var lastRecordDate = pvRecords.Last().Timestamp;

        var countFalse = validRecords.Count(v => v!=true);
        return (pvRecords, validRecords);
    }
}
