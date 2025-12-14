using LEG.MeteoSwiss.Abstractions.Models;
using LEG.PV.Core.Models;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.PV.Data.Processor
{
    internal class MeteoSimulator
    {
        // Physical constants
        const double maxIrradiance = 1361;                                              // [W/m^2] Solar constant
        const double KelvinZeroC = 273.15;                                              // [K]
        const double StefanBoltzmannConstant = 5.670E-8;                                // [Nm/sm^2K^4]
        const double specificHeatAir = 1005;                                            // [Nm/kgK]
        const double airDensity = 1.225;                                                // [kg/m^3]
        const double airPressure = 101325;                                              // [N/m^2]
        const double EarthtGravity = 9.81;                                              // [m/s^2]

        // Model parameters
        const double diffuseRadiationRatio = 0.3;
        const Double averagediffuseRadiation = maxIrradiance * diffuseRadiationRatio;
        const double maxDirectIrratiance = maxIrradiance - averagediffuseRadiation;
        const double weightPreviousIrradiance = 0.7;
        const double directRadiationCV = 0.1;
        const double minAlbedo = 0.2;
        const double maxAlbedo = 0.8;

        const double averageTemp = 5;                                                   // [°C]
        const double annualTempAmplitude = 10;                                          // [°C]
        const double diurnalTempAmplitude = 5;                                          // [°C]

        const double heightOfSurfaceLayer = 200.0;                                      // [m]
        const double greenHouseShift = 10.0;                                            // [K]
        const double diffusionTimeConstant = 3600;                                      // [s]
        const double airMassPerArea = airPressure / EarthtGravity;                      // [[N/m^2 / (m/s^2)] = [kg/m^2] 
        const double airMassSurfaceLayerPerArea = airDensity * heightOfSurfaceLayer;    // [kg/m^3 * m] = [kg/m^2]

        const double maxWindSpeed = 150;                                                // [km/h]
        const double maxNewWindVariation = 20;                                          // [km/h]
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
            int meteoType,
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

            var sinSunElevation = sunGeometry.SinSunElevation > 0.0 ? sunGeometry.SinSunElevation : 0.0;
            if (sinSunElevation > 0.0)
            {
                var clearSkyRatio = meteoType == 1 ? 0.95 : meteoType == 2 ? 0.05 : 0.05 + 0.9 * random.NextDouble();
                var randomDNI = initialize ? maxDirectIrratiance * clearSkyRatio :
                    priortMeteoParameters.DirectNormalIrradiance * weightPreviousIrradiance +
                    (1.0 - weightPreviousIrradiance) * maxDirectIrratiance * clearSkyRatio;                     // hypothetical irradiance as a function of cloudiness
                direcNormaltIrradiance = randomDNI ?? 0.0;
                sunshineDuration = direcNormaltIrradiance * minutesPerPeriod / maxDirectIrratiance;
                
                directRadiation = direcNormaltIrradiance * sinSunElevation;
                diffuseRadiation = averagediffuseRadiation * Math.Sqrt(sinSunElevation) * (clearSkyRatio + (1.0 - clearSkyRatio) * (1.0 + random.NextDouble()) / 2);
                weight = 1E-3 + Math.Pow(direcNormaltIrradiance / maxDirectIrratiance, 3);
            }
            var globalRadiation = directRadiation + diffuseRadiation;

            // Snow
            var newSnowDepth =
                !isSnowyDay ? priorSnowDepth * snowDegradationFactorPerPeriod :
                initialize ? newSnowPerDay :
                priorSnowDepth * snowDegradationFactorPerPeriod + newSnowPerPeriod;

            // Calculate ambient temperature
            var meanTemperature = averageTemp - annualTempAmplitude * cosOmegaYear - diurnalTempAmplitude * cosOmegaDay;
            var priorTemperature = initialize ? meanTemperature : priortMeteoParameters.Temperature.Value; // [°C]
            var temperature = GetNewTemperature(meanTemperature, priorTemperature, globalRadiation, meteoType, newSnowDepth > 1.0, minutesPerPeriod, random);

            // Update wind velocity with some randomness
            var deltaWindSpeed = random.NextDouble() * maxNewWindVariation;
            var newWindGustVelocity = (random.NextDouble() < windVariationProbability) ? deltaWindSpeed : 0.0;
            var windSpeed = initialize ? deltaWindSpeed : (priortMeteoParameters.WindSpeed * weightPreviousWindSpeed + newWindGustVelocity) ?? 0.0;
            windSpeed = Math.Max(0.0, Math.Min(maxWindSpeed, windSpeed));

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
                globalRadiation: Math.Round(globalRadiation, 0),
                temperature: Math.Round(temperature, 3),
                windSpeed: Math.Round(windSpeed, 2),
                windDirection: null,
                snowDepth: Math.Round(newSnowDepth, 1),
                relativeHumidity: Math.Round(relativeHumidity, 1),
                dewPoint: Math.Round(dewPoint, 1),
                radiationVariance: Math.Round(directRadiation * directRadiationCV, 0)
            );

            return (updatedMeteoParameters, newSnowDepth, weight);
        }

        private static double GetNewTemperature(
            double meanTemperature, double priorTemperature, 
            double globalRadiation, int meteoType, bool hasSnow, 
            double minutesPerPeriod, Random random)
        {

            var albedo = hasSnow ? maxAlbedo : minAlbedo;
            var timeSpan = minutesPerPeriod * 60;
            var localHeatGainPerArea = (1.0 - albedo) * globalRadiation * timeSpan;

            var blackbodyAsIfTemperature = KelvinZeroC + priorTemperature - greenHouseShift;                            // [K]
            var blackbodyRadiation = StefanBoltzmannConstant * Math.Pow(blackbodyAsIfTemperature, 4);                   // [Nm/sm^2K^4 * K^4] = [W/m^2]
            var localHeatLossPerArea = blackbodyRadiation * timeSpan;
            var blackbodyEffectiveAirMass = 
                meteoType == 1 ? airMassSurfaceLayerPerArea : 
                meteoType == 2 ? airMassPerArea : 
                Math.Sqrt(airMassSurfaceLayerPerArea * airMassPerArea);

            var localTemperatureGain = localHeatGainPerArea / (specificHeatAir * airMassSurfaceLayerPerArea);
            var localTemperatureLoss = localHeatLossPerArea / (specificHeatAir * blackbodyEffectiveAirMass);
            var temperatureDiffusion = (priorTemperature - meanTemperature) / diffusionTimeConstant * timeSpan;

            return priorTemperature + localTemperatureGain - localTemperatureLoss - temperatureDiffusion + (1.0 - random.NextDouble());
        }
    }
}
