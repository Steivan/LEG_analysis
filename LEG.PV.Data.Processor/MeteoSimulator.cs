using LEG.MeteoSwiss.Abstractions.Models;
using LEG.PV.Core.Models;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

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

        const double snowDegradationFactorPerDay = 0.8;

        const double meanRH = 60.0;
        const double fogHighRH = 100.0;
        const double fogLoRH = 80.0;
        const double fogDeltaRH = fogHighRH - fogLoRH;
        const Double deltaDewPoint = 0.1;

        public static (MeteoParameters meteoParam, double snowDepth, double weight) UpdatedMeteoParameters(
            DateTime timeStamp, int minutesPerPeriod, 
            MeteoParameters? priortMeteoParameters, 
            PvSolarGeometry sunGeometry, double cosOmegaYear, double cosOmegaDay,
            bool isSnowyDay, double priorSnowDepth, double newSnowPerDay,
            bool isFoggyDay, double fogDissolveStartHour, double fogDissolveEndHour,
            bool initialize =false)
        {
            var hour = timeStamp.Hour;
            var periodsPerDay = (24 * 60) / minutesPerPeriod;
            var random = new Random();
            var newSnowPerPeriod = newSnowPerDay / periodsPerDay;
            var snowDegradationFactorPerPeriod = Math.Exp(Math.Log(snowDegradationFactorPerDay) / periodsPerDay);

            // Update radiation with some randomness
            var sunshineDuration = 0.0;
            var directRadiation = 0.0;
            var direcNormaltIrradiance = 0.0;
            var diffuseRadiation = 0.0;
            var weight = 0.0;

            if (sunGeometry.SinSunElevation > 0.0)
            {
                var sinSunElevation = sunGeometry.SinSunElevation > 0.0 ? sunGeometry.SinSunElevation : 0.0;
                var r = random.NextDouble();
                var randomDNI = initialize ? maxDirectIrratiance * r :
                    priortMeteoParameters.DirectNormalIrradiance * weightPreviousIrradiance +
                    (1.0 - weightPreviousIrradiance) * maxDirectIrratiance * r;                     // hypothetical irradiance as a function of cloudiness
                direcNormaltIrradiance = randomDNI ?? 0.0;
                sunshineDuration = direcNormaltIrradiance * minutesPerPeriod / maxDirectIrratiance;
                directRadiation = direcNormaltIrradiance * sinSunElevation;
                diffuseRadiation = direcNormaltIrradiance / 4 * Math.Sqrt(sinSunElevation) * (1.0 + random.NextDouble()) / 2.0;
                weight = sunGeometry.SinSunElevation > 0 ? 1E-3 + Math.Pow(direcNormaltIrradiance / maxDirectIrratiance, 3) : 0.0;
            }

            // Calculate ambient temperature
            var temperature = averageTemp - annualTempAmplitude * cosOmegaYear - diurnalTempAmplitude * cosOmegaDay; // [°C]

            // Update wind velocity with some randomness
            var deltaWindSpeed = random.NextDouble() * maxNewWindVariation;
            var newWindGustVelocity = (random.NextDouble() < windVariationProbability) ? deltaWindSpeed : 0.0;
            var windSpeed = initialize ? deltaWindSpeed : (priortMeteoParameters.WindSpeed * weightPreviousWindSpeed + newWindGustVelocity) ?? 0.0;
            windSpeed = Math.Max(0.0, Math.Min(maxWindSpeed, windSpeed));

            // Snow
            var newSnowDepth =
                !isSnowyDay ? priorSnowDepth * snowDegradationFactorPerPeriod :
                initialize ? newSnowPerDay:
                priorSnowDepth * snowDegradationFactorPerPeriod + newSnowPerPeriod; 

            // Fog
            var relativeHumidity = 
                !isFoggyDay ? meanRH :
                hour <= fogDissolveStartHour ? fogHighRH :
                hour >= fogDissolveEndHour ? fogLoRH :
                fogHighRH - fogDeltaRH * (hour - fogDissolveStartHour) / (fogDissolveEndHour - fogDissolveStartHour);
            var dewPoint = MeteoParameterTypes.DPFromTAndRH(temperature, relativeHumidity);                             // Convert T and RH into DP
            dewPoint = temperature - (temperature - dewPoint) * (1.0 + (2.0 * random.NextDouble() - 1.0) * deltaDewPoint) ;

            var updatedMeteoParameters = new MeteoParameters(
                time:  timeStamp,
                interval: TimeSpan.FromMinutes(minutesPerPeriod),
                sunshineDuration: Math.Max(0, Math.Min(minutesPerPeriod, (int)sunshineDuration)),
                directRadiation: Math.Round(directRadiation, 0),
                directNormalIrradiance: Math.Round(direcNormaltIrradiance, 0),
                diffuseRadiation: Math.Round(diffuseRadiation, 0),
                globalRadiation: Math.Round(directRadiation + diffuseRadiation, 0),
                temperature: Math.Round(temperature, 1),
                windSpeed: Math.Round(windSpeed, 2),
                windDirection: null,
                snowDepth: Math.Round(newSnowDepth, 1),
                relativeHumidity: Math.Round(relativeHumidity, 1),
                dewPoint: Math.Round(dewPoint, 1),
                radiationVariance: Math.Round(directRadiation * directRadiationCV, 0)
            );

            return (updatedMeteoParameters, newSnowDepth, weight);
        }
    }
}
