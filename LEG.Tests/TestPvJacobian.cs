
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LEG.PV.Core.Models.DataRecords;
using static LEG.PV.Core.Models.PvPriorConfig;
using static LEG.PV.Core.Models.PvRTWAJacobian;
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

            var geometryFactors = new GeometryFactors
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

            var (meanEtha, sigmaEtha, minEtha, maxEtha) = GetPriorsEtha();
            var (meanGamma, sigmaGamma, minGamma, maxGamma) = GetPriorsGamma();
            var (meanU0, sigmaU0, minU0, maxU0) = GetPriorsU0();
            var (meanU1, sigmaU1, minU1, maxU1) = GetPriorsU1();
            var (meanLDegr, sigmaLDegr, minLDegr, maxLDegr) = GetPriorsLDegr();
            var modelParams = new PvModelParams(meanEtha, meanGamma, meanU0, meanU1, meanLDegr);
            var modelSigmas = new PvModelParams(sigmaEtha, sigmaGamma, sigmaU0, sigmaU1, sigmaLDegr);

            // Calculate effective power
            var effectivePower = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);

            // Calculate analytical derivatives
            var derEtha = DerEthaSys(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derGamma = DerGamma(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derU0 = DerU0(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derU1 = DerU1(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);
            var derLDegr = DerLDegr(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);

            // Calculate Jacobian derivatives
            var (effectivePowerJac, derEthaJac, derGammaJac, derU0Jac, derU1Jac, derLDegrJac) = PvJacobianFunc(
                    installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams);

            // Calculate numerical derivatives
            var derEthaNum = GetNumericalDerivative(0, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            var derGammaNum = GetNumericalDerivative(1, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            var derU0Num = GetNumericalDerivative(2, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            var derU1Num = GetNumericalDerivative(3, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);
            var derLDegrNum = GetNumericalDerivative(4, installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams, modelSigmas);

            Assert.AreEqual(effectivePower, effectivePowerJac, 1e-6);

            Assert.AreEqual(derEthaJac / derEtha, 1, 1e-6);
            Assert.AreEqual(derEthaNum / derEtha, 1, 1e-4);

            Assert.AreEqual(derGammaJac / derGamma, 1, 1e-6);
            Assert.AreEqual(derGammaNum / derGamma, 1, 1e-4);

            Assert.AreEqual(derU0Jac / derU0, 1, 1e-6);
            Assert.AreEqual(derU0Num / derU0, 1, 1e-3);

            Assert.AreEqual(derU1Jac / derU1, 1, 1e-6);
            Assert.AreEqual(derU1Num / derU1, 1, 1e-3);

            Assert.AreEqual(derLDegrJac / derLDegr, 1, 1e-6);
            Assert.AreEqual(derLDegrNum / derLDegr, 1, 1e-4);

            Console.WriteLine($"Effective Power: {effectivePower,10:F5} {effectivePowerJac / effectivePower - 1,12:F8}");
            Console.WriteLine($"Der EthaSys    : {derEtha,10:F5} {derEthaJac / derEtha - 1,12:F8} {derEthaNum / derEtha - 1,12:F8}");
            Console.WriteLine($"Der Gamma      : {derGamma,10:F5} {derGammaJac / derGamma - 1,12:F8} {derGammaNum / derGamma - 1,12:F8}");
            Console.WriteLine($"Der U0         : {derU0,10:F5} {derU0Jac / derU0 - 1,12:F8} {derU0Num / derU0 - 1,12:F8}");
            Console.WriteLine($"Der U1         : {derU1,10:F5} {derU1Jac / derU1 - 1,12:F8} {derU1Num / derU1 - 1,12:F8}");
            Console.WriteLine($"Der LDegr      : {derLDegr,10:F5} {derLDegrJac / derLDegr - 1,12:F8} {derLDegrNum / derLDegr - 1,12:F8}");
        }
    }
}
