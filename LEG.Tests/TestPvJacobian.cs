
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LEG.PV.Core.Models;
using static LEG.PV.Core.Models.PvPriorConfig;
using static LEG.PV.Core.Models.PvPowerJacobian;
using LEG.MeteoSwiss.Abstractions.Models;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.Tests
{
    [TestClass]
    public class TestPvJacobian
    {
        // Test model parameters priors
        const double meanEtha = 0.75;
        const double meanGamma = -0.005;                   // [/°C]
        const double meanU0 = 25;                          // [W/m^2 K]
        const double meanU1 = 0.5;                         // [W/m^2 K per km/h]
        const double meanLDegr = 0.01;                     // [/year]
        const double cvGRTW = 0.1;
        // Snow and fog priors
        const double meanDSnow = 25.0;
        const double meanLambdaAFog = 2.0;
        const double meanBFog = 1.0;                       // [/°C]  
        const double meanLambdaKFog = 2.0;
        const double cvSF = 0.5;

        // Test input parameters
        const double installedPower = 10000.0;       // [Wp]
        const int periodsPerHour = 4;                // [1/h]
        const double directGeometryFactor = 0.7;     // [unitless]
        const double diffuseGeometryFactor = 0.9;    // [unitless]
        const double sinSunElevation = 0.8;          // [unitless]
        const double shortWaveRadiation = 1200.0;    // [W/m^2]
        const double sunshineDuration = 12.0;        // [m / 15 m]
        const double diffuseRadiation = 200.0;       // [W/m^2]
        const double ambientTemp = 30.0;             // [°C]
        const double windSpeed = 20;                 // [km/h]
        const double snowDepth = 0.0;                // [cm]
        const double relativeHumidity = 95;          // [%] 
        const double dewPoint = ambientTemp - (1 - relativeHumidity / 100) * (ambientTemp - 14); 
        const double age = 5.0;                      // [y]

        [TestMethod]
        public void TestJacobian()
        {
            // Define model parameters and their sigmas
            var modelParams = new PvModelParams(etha: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr,
                dSnow: meanDSnow, lambdaAFog: meanLambdaAFog, bFog: meanBFog, lambdaKFog: meanLambdaKFog);

            var modelSigmas = new PvModelParams(etha: cvGRTW * meanEtha, gamma: cvGRTW * meanGamma, u0: cvGRTW * meanU0, u1: cvGRTW * meanU1, lDegr: cvGRTW * meanLDegr,
                dSnow: cvSF * meanDSnow, lambdaAFog: cvSF * meanLambdaAFog, bFog: cvSF * meanBFog, lambdaKFog: cvSF * meanLambdaKFog);

            var geometryFactors = new PvSolarGeometry
            (
                directGeometryFactor, 
                diffuseGeometryFactor, 
                sinSunElevation
            );

            var meteoParameters = new MeteoParameters
            (
                time: DateTime.UtcNow,
                interval: TimeSpan.FromMinutes(15),
                sunshineDuration: sunshineDuration,
                directRadiation: null,
                directNormalIrradiance: null,
                globalRadiation: shortWaveRadiation,
                diffuseRadiation: diffuseRadiation,
                temperature: ambientTemp,
                windSpeed: windSpeed,
                windDirection: 0,
                snowDepth: snowDepth,
                relativeHumidity: relativeHumidity,
                dewPoint: dewPoint,
                radiationVariance: shortWaveRadiation * shortWaveRadiation * 0.01
            );

            // Calculate effective power
            var powerRecord = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);

            // Calculate analytical derivatives
            var derEtha = DerEthaSys(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derGamma = DerGamma(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derU0 = DerU0(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derU1 = DerU1(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derLDegr = DerLDegr(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            // Snow: derivative is a delta function and cannot be tested with numerical derivatives
            var derDSnow = DerDSnow(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            // Fog: d (PowerGRTWSF / PowerGRTW) / d param_i 
            var derLambdaAFog = DerLambdaAFog(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derBFog = DerBFog(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derLambdaKFog = DerLambdaKFog(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);

            // Calculate Jacobian derivatives
            var (jacobianPowerRecord, derivativesRecord) = PvJacobianFunc(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);

            // Calculate numerical derivatives: d PowerGRTW / d param_i
            int paramIndex = 0;
            var derEthaNum = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            paramIndex++;
            var derGammaNum = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            paramIndex++;
            var derU0Num = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            paramIndex++;
            var derU1Num = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            paramIndex++;
            var derLDegrNum = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            paramIndex++;
            // Snow: derivative is a delta function and cannot be tested with numerical derivatives
            var derDSnowNum = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            paramIndex++;
            // Fog: d (PowerGRTWS / PowerGRTW) / d param_i
            var derLambdaAFogNum = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            paramIndex++;
            var derBFogNum = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            paramIndex++;
            var derLambdaKFogNum = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);

            Assert.AreEqual(powerRecord.PowerG, jacobianPowerRecord.PowerG, 1e-6);
            Assert.AreEqual(powerRecord.PowerGR, jacobianPowerRecord.PowerGR, 1e-6);
            Assert.AreEqual(powerRecord.PowerGRT, jacobianPowerRecord.PowerGRT, 1e-6);
            Assert.AreEqual(powerRecord.PowerGRTW, jacobianPowerRecord.PowerGRTW, 1e-6);
            Assert.AreEqual(powerRecord.PowerGRTWS, jacobianPowerRecord.PowerGRTWS, 1e-6);
            Assert.AreEqual(powerRecord.PowerGRTWSF, jacobianPowerRecord.PowerGRTWSF, 1e-6);

            Assert.AreEqual(derivativesRecord.Etha / derEtha, 1, 1e-6);
            Assert.AreEqual(derEthaNum / derEtha, 1, 1e-4);

            Assert.AreEqual(derivativesRecord.Gamma / derGamma, 1, 1e-6);
            Assert.AreEqual(derGammaNum / derGamma, 1, 1e-4);

            Assert.AreEqual(derivativesRecord.U0 / derU0, 1, 1e-6);
            Assert.AreEqual(derU0Num / derU0, 1, 1e-3);

            Assert.AreEqual(derivativesRecord.U1 / derU1, 1, 1e-6);
            Assert.AreEqual(derU1Num / derU1, 1, 1e-3);

            Assert.AreEqual(derivativesRecord.LDegr / derLDegr, 1, 1e-6);
            Assert.AreEqual(derLDegrNum / derLDegr, 1, 1e-4);

            Assert.AreEqual(derivativesRecord.DSnow, derDSnow, 1e-6);
            Assert.AreEqual(derivativesRecord.LambdaAFog / derLambdaAFog, 1, 1e-6);
            Assert.AreEqual(derivativesRecord.BFog / derBFog, 1, 1e-6);
            Assert.AreEqual(derivativesRecord.LambdaKFog / derLambdaKFog, 1, 1e-6);

            Assert.AreEqual(derDSnowNum, derDSnow, 2e-2);
            Assert.AreEqual(derLambdaAFogNum / derLambdaAFog, 1, 2e-2);
            Assert.AreEqual(derBFogNum / derBFog, 1, 2e-2);
            Assert.AreEqual(derLambdaKFogNum / derLambdaKFog, 1, 2e-2);

            Console.WriteLine($"Effective Power: {powerRecord,10:F5} {jacobianPowerRecord.PowerGRTW / powerRecord.PowerGRTW - 1,12:F8}");
            Console.WriteLine($"Der EthaSys    : {derEtha,10:F5} {derivativesRecord.Etha / derEtha - 1,12:F8} {derEthaNum / derEtha - 1,12:F8}");
            Console.WriteLine($"Der Gamma      : {derGamma,10:F5} {derivativesRecord.Gamma / derGamma - 1,12:F8} {derGammaNum / derGamma - 1,12:F8}");
            Console.WriteLine($"Der U0         : {derU0,10:F5} {derivativesRecord.U0 / derU0 - 1,12:F8} {derU0Num / derU0 - 1,12:F8}");
            Console.WriteLine($"Der U1         : {derU1,10:F5} {derivativesRecord.U1 / derU1 - 1,12:F8} {derU1Num / derU1 - 1,12:F8}");
            Console.WriteLine($"Der LDegr      : {derLDegr,10:F5} {derivativesRecord.LDegr / derLDegr - 1,12:F8} {derLDegrNum / derLDegr - 1,12:F8}");
        }
    }
}
