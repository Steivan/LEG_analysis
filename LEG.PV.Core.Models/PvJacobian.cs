using static LEG.PV.Core.Models.PvPriorConfig;
using static LEG.PV.Core.Models.PvParameters;

namespace LEG.PV.Core.Models;

public class PvJacobian
{
    public static (double gDirectPoa, double gDiffusePoa) GetDecomposedGpoa(double globalHorizontalIrradiance, double sunshineDuration, double diffuseHorizontalIrradiance, double cosSunElevation)
    {
        //return (globalHorizontalIrradiance, 0.0);       // TODO: Temporary fix until a proper decomposition method is implemented

        var directHorizontalIrradiance = Math.Max(0, globalHorizontalIrradiance - diffuseHorizontalIrradiance);
        var gDirectPoa = cosSunElevation > 0 ? directHorizontalIrradiance / cosSunElevation : 0.0;
        var gDiffusePoa = diffuseHorizontalIrradiance;

        return (gDirectPoa, gDiffusePoa);
    }
    // Effective Power
    public static double EffectiveCellPower(double installedPower, double directGeometryFactor, double diffuseGeometryFactor, double cosSunElevation,
        double globalHorizontalIrradiance, double sunshineDuration, double diffuseHorizontalIrradiance, double ambientTemp, double windSpeed, double snowDepth, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (directGeometryFactor <= 0 && cosSunElevation <= 0)
            return 0.0;
        var (gDirectPoa, gDiffusePoa) = GetDecomposedGpoa(globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, cosSunElevation);

        directGeometryFactor = Math.Max(directGeometryFactor, 0.0);
        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windSpeed);
        var tempFactor = (1 + gamma * (cellTemp - meanTempStc));
        var gCell = irradianceRatio * degradedPower * ethaSys * tempFactor;

        return gCell;
    }

    // Numerical Derivative
    public static double GetNumericalDerivative(int paramIndexinstalledPower, double installedPower, 
        double directGeometryFactor, double diffuseGeometryFactor, double cosSunElevation,
        double globalHorizontalIrradiance, double sunshineDuration, double diffuseHorizontalIrradiance, double ambientTemp, double windSpeed, double snowDepth, double age,
        double ethaSys, double gamma, double u0, double u1, double lDegr,
        double sigmaEtha, double sigmaGamma, double sigmaU0, double sigmaU1, double sigmaLDegr)
    {
        var ethaSys1 = ethaSys;
        var ethaSys2 = ethaSys;
        var gamma1 = gamma;
        var gamma2 = gamma;
        var u01 = u0;
        var u02 = u0;
        var u11 = u1;
        var u12 = u1;
        var lDegr1 = lDegr;
        var lDegr2 = lDegr;
        var delta = 1e-6;

        switch (paramIndexinstalledPower % 5)
        {
            case 0:
                ethaSys1 += sigmaEtha;
                ethaSys2 -= sigmaEtha;
                delta = 2 * sigmaEtha;
                break;
            case 1:
                gamma1 += sigmaGamma;
                gamma2 -= sigmaGamma;
                delta = 2 * sigmaGamma;
                break;
            case 2:
                u01 += sigmaU0 / 10;
                u02 -= sigmaU0 / 10;
                delta = 2 * sigmaU0 / 10;
                break;
            case 3:
                u11 += sigmaU1 / 10;
                u12 -= sigmaU1 / 10;
                delta = 2 * sigmaU1 / 10;
                break;
            case 4:
                lDegr1 += sigmaLDegr;
                lDegr2 -= sigmaLDegr;
                delta = 2 * sigmaLDegr;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(paramIndexinstalledPower), "Invalid parameter index");
        }
        var f1 = EffectiveCellPower(installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
            globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, ambientTemp, windSpeed, snowDepth, age,
            ethaSys: ethaSys1, gamma: gamma1, u0: u01, u1: u11, lDegr: lDegr1);
        var f2 = EffectiveCellPower(installedPower, directGeometryFactor, diffuseGeometryFactor, cosSunElevation,
            globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, ambientTemp, windSpeed, snowDepth, age,
            ethaSys: ethaSys2, gamma: gamma2, u0: u02, u1: u12, lDegr: lDegr2);

        return (f1 - f2) / delta;
    }

    // Derivativs for Jacobian
    public static double DerEthaSys(double installedPower, double directGeometryFactor, double diffuseGeometryFactor, double cosSunElevation,
        double globalHorizontalIrradiance, double sunshineDuration, double diffuseHorizontalIrradiance, double ambientTemp, double windSpeed, double snowDepth, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (directGeometryFactor <= 0 && cosSunElevation <= 0)
            return 0.0;
        var (gDirectPoa, gDiffusePoa) = GetDecomposedGpoa(globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, cosSunElevation);

        directGeometryFactor = Math.Max(directGeometryFactor, 0.0);
        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windSpeed);
        var tempFactor = (1 + gamma * (cellTemp - meanTempStc));
        var derEtha = irradianceRatio * degradedPower * tempFactor;

        return derEtha;
    }

    public static double DerGamma(double installedPower, double directGeometryFactor, double diffuseGeometryFactor, double cosSunElevation,
        double globalHorizontalIrradiance, double sunshineDuration, double diffuseHorizontalIrradiance, double ambientTemp, double windSpeed, double snowDepth, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (directGeometryFactor <= 0 && cosSunElevation <= 0)
            return 0.0;
        var (gDirectPoa, gDiffusePoa) = GetDecomposedGpoa(globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, cosSunElevation);

        directGeometryFactor = Math.Max(directGeometryFactor, 0.0);
        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windSpeed);
        var derGammaTempFactor = (cellTemp - meanTempStc);
        var derGamma = irradianceRatio * degradedPower * ethaSys * derGammaTempFactor;

        return derGamma;
    }
    public static double DerU0(double installedPower, double directGeometryFactor, double diffuseGeometryFactor, double cosSunElevation,
        double globalHorizontalIrradiance, double sunshineDuration, double diffuseHorizontalIrradiance, double ambientTemp, double windSpeed, double snowDepth, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (directGeometryFactor <= 0 && cosSunElevation <= 0)
            return 0.0;
        var (gDirectPoa, gDiffusePoa) = GetDecomposedGpoa(globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, cosSunElevation);

        directGeometryFactor = Math.Max(directGeometryFactor, 0.0);
        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windSpeed);
        var derU0TempFactor = (-gPoa / Math.Pow(u0 + u1 * windSpeed, 2));
        var derU0 = irradianceRatio * degradedPower * ethaSys * gamma * derU0TempFactor;

        return derU0;
    }
    public static double DerU1(double installedPower, double directGeometryFactor, double diffuseGeometryFactor, double cosSunElevation,
        double globalHorizontalIrradiance, double sunshineDuration, double diffuseHorizontalIrradiance, double ambientTemp, double windSpeed, double snowDepth, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (directGeometryFactor <= 0 && cosSunElevation <= 0)
            return 0.0;
        var (gDirectPoa, gDiffusePoa) = GetDecomposedGpoa(globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, cosSunElevation);

        directGeometryFactor = Math.Max(directGeometryFactor, 0.0);
        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windSpeed);
        var tempFactor = (1 + gamma * (cellTemp - meanTempStc));
        var derU1TempFactor = (-gPoa / Math.Pow(u0 + u1 * windSpeed, 2)) * windSpeed;
        var derU1 = irradianceRatio * degradedPower * ethaSys * gamma * derU1TempFactor;

        return derU1;
    }

    public static double DerLDegr(double installedPower, double directGeometryFactor, double diffuseGeometryFactor, double cosSunElevation,
        double globalHorizontalIrradiance, double sunshineDuration, double diffuseHorizontalIrradiance, double ambientTemp, double windSpeed, double snowDepth, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (directGeometryFactor <= 0 && cosSunElevation <= 0)
            return 0.0;
        var (gDirectPoa, gDiffusePoa) = GetDecomposedGpoa(globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, cosSunElevation);

        directGeometryFactor = Math.Max(directGeometryFactor, 0.0);
        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var derDegradedPower = installedPower * (-age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windSpeed);
        var tempFactor = 1 + gamma * (cellTemp - meanTempStc);
        var derLDegr = irradianceRatio * derDegradedPower * ethaSys * tempFactor;

        return derLDegr;
    }

    // EffectivePower and Jacobian
    public static (double effP, double derEtha, double derGamma, double derU0, double derU1, double derLDegr)
        PvJacobianFunc(double installedPower, double directGeometryFactor, double diffuseGeometryFactor, double cosSunElevation,
        double globalHorizontalIrradiance, double sunshineDuration, double diffuseHorizontalIrradiance, double ambientTemp, double windSpeed, double snowDepth, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (directGeometryFactor <= 0 && cosSunElevation <= 0)
            return (0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
        var (gDirectPoa, gDiffusePoa) = GetDecomposedGpoa(globalHorizontalIrradiance, sunshineDuration, diffuseHorizontalIrradiance, cosSunElevation);

        directGeometryFactor = Math.Max(directGeometryFactor, 0.0);
        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var derDegradedPower = - installedPower * age;
        var degradedPower = installedPower + derDegradedPower * lDegr;

        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windSpeed);
        var derGammaTempFactor = cellTemp - meanTempStc;
        var derU0TempFactor = - gPoa / Math.Pow(u0 + u1 * windSpeed, 2);
        var tempFactor = 1 + gamma * derGammaTempFactor;

        var referencePower = irradianceRatio * degradedPower;
        var systemPower = referencePower * ethaSys;
        var derEtha = referencePower * tempFactor;
        var gCell = derEtha * ethaSys;
        var derGamma = systemPower * derGammaTempFactor;
        var derU0 = systemPower * gamma * derU0TempFactor;
        var derU1 = derU0 * windSpeed;
        var derLDegr = irradianceRatio * derDegradedPower * ethaSys * tempFactor;

        return (gCell, derEtha, derGamma, derU0, derU1, derLDegr); ;
    }

}
