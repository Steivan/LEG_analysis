using LEG.MeteoSwiss.Abstractions.Models;
using LEG.PV.Core.Models;

namespace LEG.PV.Data.Processor
{
    internal class MeteoSimulator
    {
        const double maxIrradiance = 1361;              // [W/m^2] Solar constant
        const double diffuseRadiationRatio = 0.3;
        const Double averagediffuseRadiation = maxIrradiance * diffuseRadiationRatio;
        const double maxDirectIrratiance = maxIrradiance - averagediffuseRadiation;
        const double weightPreviousIrradiance = 0.7;
        const double directRadiationCV = 0.1;

        const double averageTemp = 15;                  // [°C]
        const double annualTempAmplitude = 15;          // [°C]
        const double diurnalTempAmplitude = 5;          // [°C]

        const double maxWindSpeed = 150;                // [km/h]
        const double maxNewWindVariation = 20;          // [km/h]
        const double windVariationProbability = 0.1;
        const double weightPreviousWindSpeed = 0.95;

        const double snowDegradationFactor = 0.8;

        const double meanRH = 60.0;
        const double fogHighRH = 100.0;
        const double fogLoRH = 80.0;
        const double fogDeltaRH = fogHighRH - fogLoRH;
        const Double deltaDewPoint = 0.1;

        public static (MeteoParameters meteoParam, double weight) UpdatedMeteoParameters(
            DateTime timeStamp, int minutesPerPeriod, 
            MeteoParameters? priortMeteoParameters, 
            PvSolarGeometry sunGeometry, double cosOmegaYear, double cosOmegaDay,
            bool isSnowyDay, double newSnow,
            bool isFoggyDay, double fogDissolveStartHour, double fogDissolveEndHour,
            bool initialize =false)
        {
            var hour = timeStamp.Hour;
            var random = new Random();

            // Update radiation with some randomness
            var sunshineDuration = 0.0;
            var directRadiation = 0.0;
            var direcNormaltIrradiance = 0.0;
            var diffuseRadiation = 0.0;
            var weight = 0.0;

            if (sunGeometry.SinSunElevation > 0.0)
            {
                var r = random.NextDouble();
                var randomDNI = initialize ? maxDirectIrratiance * r :
                    priortMeteoParameters.DirectNormalIrradiance * weightPreviousIrradiance +
                    (1.0 - weightPreviousIrradiance) * maxDirectIrratiance * r;                     // hypothetical irradiance as a function of cloudiness
                direcNormaltIrradiance = randomDNI ?? 0.0;
                sunshineDuration = direcNormaltIrradiance * minutesPerPeriod / maxDirectIrratiance;
                directRadiation = direcNormaltIrradiance * sunGeometry.SinSunElevation;
                diffuseRadiation = averagediffuseRadiation + (maxDirectIrratiance - direcNormaltIrradiance) * 0.1;
                weight = sunGeometry.SinSunElevation > 0 ? 1E-3 + Math.Pow(direcNormaltIrradiance / maxDirectIrratiance, 3) : 0.0;
            }

            // Calculate ambient temperature
            var temperature = averageTemp - annualTempAmplitude * cosOmegaYear - diurnalTempAmplitude * cosOmegaDay; // [°C]

            // Update wind velocity with some randomness
            var deltaWindSpeed = random.NextDouble() * maxNewWindVariation;
            var newWindGustVelocity = (random.NextDouble() < windVariationProbability) ? deltaWindSpeed : 0.0;
            var windSpeed = initialize ? deltaWindSpeed :
                (priortMeteoParameters.WindSpeed * weightPreviousWindSpeed + newWindGustVelocity) ?? 0.0;
            windSpeed = Math.Max(0.0, Math.Min(maxWindSpeed, windSpeed));

            // Snow
            var snowDepth =
                !isSnowyDay ? 0.0 :
                initialize ? newSnow:
                priortMeteoParameters.SnowDepth * snowDegradationFactor + newSnow; 

            // Fog
            var relativeHumidity = 
                !isFoggyDay ? meanRH :
                hour <= fogDissolveStartHour ? fogHighRH :
                hour >= fogDissolveEndHour ? fogLoRH :
                fogHighRH - fogDeltaRH * (hour - fogDissolveStartHour) / (fogDissolveEndHour - fogDissolveStartHour);
            var dewPoint =
                initialize ? temperature - 0.1 * (100.0 - relativeHumidity) :
                priortMeteoParameters.DewPointFromRH(temperature, relativeHumidity);                             // Convert T and RH into DP
            dewPoint = temperature - (temperature - dewPoint) * (1.0 + (2.0 * random.NextDouble() - 1.0) * deltaDewPoint) ;

            var updatedMeteoParameters = new MeteoParameters(
                Time:  timeStamp,
                Interval: TimeSpan.FromMinutes(minutesPerPeriod),
                SunshineDuration: Math.Max(0,Math.Min(minutesPerPeriod, (int)sunshineDuration)),
                DirectRadiation: directRadiation,
                DirectNormalIrradiance: direcNormaltIrradiance,
                DiffuseRadiation: diffuseRadiation,
                GlobalRadiation: directRadiation + diffuseRadiation,
                Temperature: temperature,
                WindSpeed: windSpeed,
                WindDirection: null,
                SnowDepth: snowDepth,
                RelativeHumidity: relativeHumidity,
                DewPoint: dewPoint,
                DirectRadiationVariance: directRadiation * directRadiationCV
            );

            return (updatedMeteoParameters, weight);
        }
    }
}
