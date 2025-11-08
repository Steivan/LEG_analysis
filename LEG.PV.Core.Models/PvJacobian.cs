using static LEG.PV.Core.Models.PvPriorConfig;
using static LEG.PV.Core.Models.PvParameters;

namespace LEG.PV.Core.Models;

public class PvJacobian
{

    // Effective Power
    public static double EffectiveCellPower(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var irradianceRatio = gPoa / baselineIrradiation;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var tempFactor = (1 + gamma * (cellTemp - meanTempStc));
        var gCell = irradianceRatio * degradedPower * ethaSys * tempFactor;

        return gCell;
    }

    // Derivativs for Jacobian
    public static double DerEthaSys(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var irradianceRatio = gPoa / baselineIrradiation;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var tempFactor = (1 + gamma * (cellTemp - meanTempStc));
        var derEtha = irradianceRatio * degradedPower * tempFactor;

        return derEtha;
    }

    public static double DerGamma(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var irradianceRatio = gPoa / baselineIrradiation;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var derGammaTempFactor = (cellTemp - meanTempStc);
        var derGamma = irradianceRatio * degradedPower * ethaSys * derGammaTempFactor;

        return derGamma;
    }
    public static double DerU0(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var irradianceRatio = gPoa / baselineIrradiation;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var derU0TempFactor = (-gPoa / Math.Pow(u0 + u1 * windVelocity, 2));
        var derU0 = irradianceRatio * degradedPower * ethaSys * gamma * derU0TempFactor;

        return derU0;
    }
    public static double DerU1(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var irradianceRatio = gPoa / baselineIrradiation;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var tempFactor = (1 + gamma * (cellTemp - meanTempStc));
        var derU1TempFactor = (-gPoa / Math.Pow(u0 + u1 * windVelocity, 2)) * windVelocity;
        var derU1 = irradianceRatio * degradedPower * ethaSys * gamma * derU1TempFactor;

        return derU1;
    }

    public static double DerLDegr(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var irradianceRatio = gPoa / baselineIrradiation;
        var derDegradedPower = installedPower * (-age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var tempFactor = 1 + gamma * (cellTemp - meanTempStc);
        var derLDegr = irradianceRatio * derDegradedPower * ethaSys * tempFactor;

        return derLDegr;
    }

    // EffectivePower and Jacobian
    public static (double effP, double derEtha, double derGamma, double derU0, double derU1, double derLDegr)
        PvJacobianFunc(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return (0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

        var gPoa = geometryFactor * irradiation;
        var irradianceRatio = gPoa / baselineIrradiation;
        var derDegradedPower = - installedPower * age;
        var degradedPower = installedPower + derDegradedPower * lDegr;

        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var derGammaTempFactor = cellTemp - meanTempStc;
        var derU0TempFactor = - gPoa / Math.Pow(u0 + u1 * windVelocity, 2);
        var tempFactor = 1 + gamma * derGammaTempFactor;

        var referencePower = irradianceRatio * degradedPower;
        var systemPower = referencePower * ethaSys;
        var derEtha = referencePower * tempFactor;
        var gCell = derEtha * ethaSys;
        var derGamma = systemPower * derGammaTempFactor;
        var derU0 = systemPower * gamma * derU0TempFactor;
        var derU1 = derU0 * windVelocity;
        var derLDegr = irradianceRatio * derDegradedPower * ethaSys * tempFactor;

        return (gCell, derEtha, derGamma, derU0, derU1, derLDegr); ;
    }

}
