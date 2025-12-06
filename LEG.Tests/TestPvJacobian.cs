
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LEG.PV.Core.Models;
using static LEG.PV.Core.Models.PvPriorConfig;
using static LEG.PV.Core.Models.PvPowerJacobian;
using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.Tests
{
    [TestClass]
    public class TestPvJacobian
    {
        [TestMethod]
        public void TestJacobian()
        {
            var installedPower = 10000.0;       // [Wp]
            var periodsPerHour = 4;             // [1/h]
            var directGeometryFactor = 0.7;     // [unitless]
            var diffuseGeometryFactor = 0.8;    // [unitless]
            var sinSunElevation = 0.8;          // [unitless]
            var shortWaveRadiation = 1175.0;    // [W/m^2]
            var sunshineDuration = 12.0;        // [m / 15 m]
            var diffuseRadiation = 375.0;       // [W/m^2]
            var ambientTemp = 35.0;             // [°C]
            var windSpeed = 22;                 // [km/h]
            var snowDepth = 0.0;                // [m]
            var age = 5.0;                      // [y]

            var geometryFactors = new PvSolarGeometry
            (
                directGeometryFactor, 
                diffuseGeometryFactor, 
                sinSunElevation
            );
            var meteoParameters = new MeteoParameters
            (
                Time: DateTime.UtcNow,
                Interval: TimeSpan.FromMinutes(15),
                SunshineDuration: sunshineDuration,
                DirectRadiation: null,
                DirectNormalIrradiance: null,
                GlobalRadiation: shortWaveRadiation,
                DiffuseRadiation: diffuseRadiation,
                Temperature: ambientTemp,
                WindSpeed: windSpeed,
                WindDirection: null,
                SnowDepth: snowDepth,
                RelativeHumidity: null,
                DewPoint: null,
                DirectRadiationVariance: null
            );

            var modelParams = GetAllPriorsMeans();
            var modelSigmas = GetAllPriorsSigmas();

            // Calculate effective power
            var powerRecord = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);

            // Calculate analytical derivatives
            var derEtha = DerEthaSys(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derGamma = DerGamma(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derU0 = DerU0(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derU1 = DerU1(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derLDegr = DerLDegr(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            // Snow: derivative is a delta function and cannot be tested with numerical derivatives
            var derLambdaDSnow = DerLambdaDSnow(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
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
            var derLambdaDSnowNum = GetNumericalDerivative(paramIndex, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
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

            Assert.AreEqual(derivativesRecord.LambdaDSnow, derLambdaDSnow, 1e-6);
            Assert.AreEqual(derivativesRecord.LambdaAFog / derLambdaAFog, 1, 1e-6);
            Assert.AreEqual(derivativesRecord.BFog / derBFog, 1, 1e-6);
            Assert.AreEqual(derivativesRecord.LambdaKFog / derLambdaKFog, 1, 1e-6);

            Assert.AreEqual(derLambdaDSnowNum, derLambdaDSnow, 2e-2);
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
