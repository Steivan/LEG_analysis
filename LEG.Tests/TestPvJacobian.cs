
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LEG.PV.Core.Models.PvJacobian;
using static LEG.PV.Core.Models.PvPriorConfig;

namespace LEG.Tests
{
    [TestClass]
    public class TestPvJacobian
    {
        [TestMethod]
        public void TestJacobian()
        {
            var installedPower = 10.0;      // [kWp]
            var directGeometryFactor = 0.7;
            var diffuseGeometryFactor =1.0;
            var cosSunElevation = 1.0;       // [unitless]
            var directIrradiance = 800.0;        // [W/m^2]
            var sunshineDuration = 5.0;       // [h]
            var diffuseIrradiance = 0.0;       // [W/m^2]
            var ambientTemp = 35.0;         // [°C]
            var windSpeed = 100;         // [km/h]
            var snowDepth = 0.0;         // [m]
            var age = 5.0;                  // [y]

            var (meanEtha, sigmaEtha, minEtha, maxEtha) = GetPriorsEtha();
            var (meanGamma, sigmaGamma, minGamma, maxGamma) = GetPriorsGamma();
            var (meanU0, sigmaU0, minU0, maxU0) = GetPriorsU0();
            var (meanU1, sigmaU1, minU1, maxU1) = GetPriorsU1();
            var (meanLDegr, sigmaLDegr, minLDegr, maxLDegr) = GetPriorsLDegr();

            // Calculate effective power
            var effectivePower = EffectiveCellPower(installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                    directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                    ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);

            // Calculate analytical derivatives
            var derEtha = DerEthaSys(installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                    directIrradiance, sunshineDuration,diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                    ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
            var derGamma = DerGamma(installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                   directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                    ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
            var derU0 = DerU0(installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                    directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                    ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
            var derU1 = DerU1(installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                    directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                    ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
            var derLDegr = DerLDegr(installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                    directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                    ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);

            // Calculate Jacobian derivatives
            var (effectivePowerJac, derEthaJac, derGammaJac, derU0Jac, derU1Jac, derLDegrJac) = PvJacobianFunc(
                    installedPower, directGeometryFactor, 
                    sunshineDuration, diffuseGeometryFactor, cosSunElevation, directIrradiance, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                    ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);

            // Calculate numerical derivatives
            var derEthaNum = GetNumericalDerivative(0, installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr,
                sigmaEtha, sigmaGamma, sigmaU0, sigmaU1, sigmaLDegr);
            var derGammaNum = GetNumericalDerivative(1, installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
               directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr,
                sigmaEtha, sigmaGamma, sigmaU0, sigmaU1, sigmaLDegr);
            var derU0Num = GetNumericalDerivative(2, installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr,
                sigmaEtha, sigmaGamma, sigmaU0, sigmaU1, sigmaLDegr);
            var derU1Num = GetNumericalDerivative(3, installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
                directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr,
                sigmaEtha, sigmaGamma, sigmaU0, sigmaU1, sigmaLDegr);
            var derLDegrNum = GetNumericalDerivative(4, installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
               directIrradiance, sunshineDuration, diffuseIrradiance, ambientTemp, windSpeed, snowDepth, age,
                ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr,
                sigmaEtha, sigmaGamma, sigmaU0, sigmaU1, sigmaLDegr);

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
