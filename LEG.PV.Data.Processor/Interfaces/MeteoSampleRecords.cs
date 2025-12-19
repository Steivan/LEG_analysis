using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.PV.Data.Processor.Interfaces
{
    public class MeteoSampleRecords
    {
        private const double omegaYear = 2.0 * Math.PI / 365.0;
        private const double omegaDay = 2.0 * Math.PI / 24.0;
        private const double DegToRad = Math.PI / 180.0;
        public static Dictionary<DateTime, MeteoParameters> GetMeteoSamples(DateTime startTime, TimeSpan interval, int countOfRecords, double amplitude = 1.0)
        {
            var timeZero = new DateTime(startTime.Year, 1, 1, 0, 0, 0);
            int minutesPerInterval = (int)interval.TotalMinutes;
            int intervalsPerHour = 60 / minutesPerInterval;

            var aDirectRadiation = 800.0 * amplitude;
            var aDiffuseRadiation = 200.0 * amplitude;
            var aTemperature = 15.0 * amplitude;
            var aWindSpeed = 10.0 * amplitude;
            var aWindDirection = 30.0 * amplitude;
            var aSnowDepth = 50.0 * amplitude;
            var aRelativeHumidity = 20.0;
            var aDewPoint = 2.0 * amplitude;


            var samples = new Dictionary<DateTime, MeteoParameters>();
            for (int i = 0; i < countOfRecords; i++)
            {
                var timestamp = startTime.Add(interval * i);
                var daysSinceYearStart = (double)(timestamp - timeZero).TotalDays;
                var timestampHour = timestamp.Hour;
                var timestampMinute = timestamp.Minute;
                int intervalIndex = timestampMinute / minutesPerInterval;
                var hoursSinceDayStart = (double)(timestampHour + timestampMinute / 60.0);

                var sinAnnual = Math.Sin(omegaYear * daysSinceYearStart);
                var cosAnnual = Math.Cos(omegaYear * daysSinceYearStart);
                var sinDiurnal = Math.Sin(omegaDay * hoursSinceDayStart);
                var cosDiurnal = Math.Cos(omegaDay * hoursSinceDayStart);
                var fractionInterval = (double)intervalIndex / intervalsPerHour;

                var annualSunElevation = 45.0 - 10.0 * cosAnnual;
                var diurnalSunElevation = -annualSunElevation * cosDiurnal;
                var sinSunElevation = Math.Sin(diurnalSunElevation * DegToRad);
                var hasRadiation = sinSunElevation > 0.0;
                var hasSnow = cosAnnual > 0.5;

                var sunshineDuration = hasRadiation ? minutesPerInterval * fractionInterval : 0;
                var directRadiation = hasRadiation ? aDirectRadiation * sinSunElevation : 0;
                var directNormalIrradiance = hasRadiation ? aDirectRadiation : 0;
                var diffuseRadiation = hasRadiation ? aDiffuseRadiation * sinSunElevation * sinSunElevation : 0;
                var temperature = 15.0 +aTemperature * sinSunElevation;
                var windSpeed = aWindSpeed * Math.Pow(cosAnnual * cosDiurnal, 2);
                var windDirection = 360.0 + aWindDirection * cosDiurnal;
                var snowDepth = hasSnow ? aSnowDepth * (cosAnnual -0.5) : 0.0;
                var relativeHumidity = 100.0 - aRelativeHumidity * (1.0 - cosAnnual * cosDiurnal);
                var dewPoint = temperature - (100 - relativeHumidity) / 5;
                var radiationVariance = Math.Pow(directRadiation / 50.0, 2);

                samples[timestamp] = new MeteoParameters(
                    time: timestamp,
                    interval: interval,
                    sunshineDuration: Math.Round(sunshineDuration, 0),
                    directRadiation: Math.Round(directRadiation, 0),
                    directNormalIrradiance: Math.Round(directNormalIrradiance, 0),
                    globalRadiation: Math.Round(directRadiation + diffuseRadiation, 0),
                    diffuseRadiation: Math.Round(diffuseRadiation, 0),
                    temperature: Math.Round(temperature, 2),
                    windSpeed: Math.Round(windSpeed, 1),
                    windDirection: Math.Round(windDirection, 0) % 360.0,
                    snowDepth: Math.Round(snowDepth, 1),
                    relativeHumidity: Math.Round(relativeHumidity, 0),
                    dewPoint: Math.Round(dewPoint, 2),
                    radiationVariance: radiationVariance
                    );

            }

            return samples;
        }
    }
}
