
using static LEG.PV.Data.Processor.DataRecords;
using static LEG.PV.Core.Models.PvJacobian;

namespace LEG.PV.Data.Processor;

public class DataSimulator
{
    public static (List<PvRecord> dataRecords, List<bool> validRecords) GetPvSimulatedRecords(PvModelParams pvVarams, double installedPower = 10, double simulationsPeriod = 5)
    {
        const double daysPerYears = 365.2422;
        const int hoursPerDay = 24;
        const int periodsPerHour = 6;
        const int minutesPerPeriod = 60 / periodsPerHour;
        var now = DateTime.Now;

        var StartTime = new DateTime(now.Year - (int)Math.Ceiling(simulationsPeriod), 1, 1, 0, 0, 0);
        int daysTotal = (int)Math.Ceiling(daysPerYears * simulationsPeriod);

        const double omegaYear = 2 * Math.PI / daysPerYears;
        const double omegaDay = 2 * Math.PI / hoursPerDay;

        const double annualSolarAmplitude = 0.1;
        const double diurnalSolarAmplitude = 0.9;

        const double maxIrradiation = 1000; // [W/m^2]
        const double weightPreviousIrradiation = 0.7;

        const double averageTemp = 15; // [°C]
        const double annualTempAmplitude = 10; // [°C]
        const double diurnalTempAmplitude = 5; // [°C]

        const double maxWindVelocity = 150; // [km/h]
        const double maxNewWindGust = 10; // [km/h]
        const double windGustProbability = 0.1;
        const double weightPreviousWindVelocity = 0.95;

        const double randomNoiseStdDev = 0.1;

        var daysPerMonth    = new List<int> { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        var fogDaysPerMonth = new List<int> { 10,  5,  0,  0,  0,  0,  0,  0,  0,  5, 10, 10 };

        double etha = pvVarams.Etha;
        double gamma = pvVarams.Gamma;
        double u0 = pvVarams.U0;
        double u1 = pvVarams.U1;
        double lDegr = pvVarams.LDegr;

        // initial values
        double irradiation = 500;
        double windVelocity = 10;
        var random = new Random();
        var pvRecords = new List<PvRecord>();
        var validRecords = new List<bool>();
        for (int day = 0; day < daysTotal; day++)
        {
            var age = (double)day / daysPerYears;
            var currentDate = StartTime.AddDays(day);
            var monthIndex = currentDate.Month - 1;
            var randomDayOfMonth = random.Next(1, daysPerMonth[monthIndex] + 1);
            var isFoggyDay = randomDayOfMonth <= fogDaysPerMonth[monthIndex];
            var annualVariation = -Math.Cos(omegaYear * day);
            for (int hour = 0; hour < 24; hour++)
            {
                var currentHour = currentDate.AddHours(hour);
                for (int period = 0; period < periodsPerHour; period++)
                {

                    var timeStamp = currentHour.AddMinutes(period * minutesPerPeriod);
                    var timeOfDay = (double)hour + period / periodsPerHour;
                    var diurnalVariation = -Math.Cos(omegaDay * timeOfDay);

                    // Geometry factor combines annual and diurnal variations
                    var geometryFactor = Math.Max(0.0, annualSolarAmplitude * annualVariation + diurnalSolarAmplitude * diurnalVariation); // Simplified model

                    // Update irradiation with some randomness
                    var newRandomIrradiation = irradiation * weightPreviousIrradiation + (1.0 - weightPreviousIrradiation) * random.NextDouble() * maxIrradiation;
                    irradiation = Math.Max(0.0, Math.Min(maxIrradiation, newRandomIrradiation)); // Smooth changes

                    // Calculate ambient temperature
                    var ambientTemp = averageTemp + annualTempAmplitude * annualVariation + diurnalTempAmplitude * diurnalVariation; // [°C]

                    // Update wind velocity with some randomness
                    var newWindGustVelocity = (random.NextDouble() < windGustProbability) ? random.NextDouble() * maxNewWindGust : 0.0;
                    var newWindVelocity = windVelocity * weightPreviousWindVelocity + newWindGustVelocity;
                    windVelocity = Math.Max(0.0, Math.Min(maxWindVelocity, newWindVelocity));

                    // Calculate theoretical effective power
                    var calculatedPower = EffectiveCellPower(installedPower, geometryFactor,
                        irradiation, ambientTemp, windVelocity, age,
                        ethaSys: etha, gamma: gamma, u0: u0, u1: u1, lDegr: lDegr);

                    // Add some noise to the measured power
                    var noise = calculatedPower * randomNoiseStdDev * (random.NextDouble() - 0.5);

                    var measuredPower = calculatedPower + noise;
                    if (isFoggyDay && hour <= 12)
                    {
                        var fogFactor = hour < 7 ? 0.0 : hour >= 12 ? 1.0 : (hour - 6.0) / 6.0;
                        measuredPower *= fogFactor; // Reduced power in foggy mornings
                    }

                    pvRecords.Add(
                        new PvRecord
                        {
                            Timestamp = timeStamp,
                            Index = pvRecords.Count,
                            GeometryFactor = geometryFactor,
                            Irradiation = irradiation,
                            AmbientTemp = ambientTemp,
                            WindVelocity = windVelocity,
                            Age = age,
                            MeasuredPower = measuredPower
                        }
                        );

                    var isValidRecord = (!isFoggyDay || hour > 12 || geometryFactor <= 0);
                    validRecords.Add(isValidRecord);
                }
            }
        }

        var countFalse = validRecords.Count(v => v!=true);
        return (pvRecords, validRecords);
    }
}
