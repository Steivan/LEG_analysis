namespace LEG.PV.Core.Models;

public class PvJacobian
{
    private const double mpSPerKmh = 1000 / 3600; // 1 km / h = 1000 m / 3600 s  ]
    private const double baselineIrradiation = 1000; // [W/m^2]
    private const double meanTempStc = 25; // [°C]
    private const double meanEthaSys = 0.85;
    private const double meanGamma = -0.004; // [/°C]
    private const double meanU0 = 29; // [W/m^2 K]
    private const double meanU1 = 0.5; // [W/m^2 K per m/s]
    private const double meanLDegr = 0.008;  // [/year]

    public (double mean, double sigma, double min, double max) GetPriorsEtha()
    {
        const double sigmaEthaSys = 0.05;
        const double minEthaSys = 0.0;
        const double maxEthaSys = 1.0;

        return (meanEthaSys, sigmaEthaSys, minEthaSys, maxEthaSys);
    }

    public (double mean, double sigma, double min, double max) GetPriorsGamma()
    {
        const double sigmaGamma = 0.0005; 
        const double minGamma = double.MinValue;
        const double maxGamma = 0;

        return (meanGamma, sigmaGamma, minGamma, maxGamma);
    }

    public (double mean, double sigma, double min, double max) GetPriorsU0()
    {
        const double sigmaU0 = 4;
        const double minU0 = 0;
        const double maxU0 = double.MaxValue;

        return (meanU0, sigmaU0, minU0, maxU0);
    }

    public (double mean, double sigma, double min, double max) GetPriorsU1()
    {
        const double sigmaU1 = 0.1;
        const double minU1 = 0;
        const double maxU1 = double.MaxValue;

        return (meanU1, sigmaU1, minU1, maxU1);
    }

    public (double mean, double sigma, double min, double max) GetPriorsLDegr()
    {
        const double sigmaLDegr = 0.002;
        const double minLDegr = 0.0;
        const double maxLDegr = 0.03;

        return (meanLDegr, sigmaLDegr, minLDegr, maxLDegr);
    }

    public static double ConvertKmhToMpS (double vKmh)
    {
        return vKmh * mpSPerKmh;
    }

    // Effective Power
    public static double EffectiveCellPower(double installedPower, double geometryFactor, 
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0  + u1 * windVelocity);
        var tempFactor = (1 + gamma * (cellTemp - meanTempStc));
        var gCell = gPoa * degradedPower * ethaSys * tempFactor;

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
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var tempFactor = (1 + gamma * (cellTemp - meanTempStc));
        var derEtha = gPoa * degradedPower * tempFactor;

        return derEtha;
    }

    public static double DerGamma(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var derGammaTempFactor = (cellTemp - meanTempStc);
        var derGamma = gPoa * degradedPower * ethaSys * derGammaTempFactor;

        return derGamma;
    }
    public static double DerU0(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var derU0TempFactor = (-gPoa / Math.Pow(u0 + u1 * windVelocity, 2));
        var derU0 = gPoa * degradedPower * ethaSys * gamma * derU0TempFactor;

        return derU0;
    }
    public static double DerU1(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var degradedPower = installedPower * (1 - lDegr * age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var tempFactor = (1 + gamma * (cellTemp - meanTempStc));
        var derU1TempFactor = (-gPoa / Math.Pow(u0 + u1 * windVelocity, 2)) * windVelocity;
        var derU1 = gPoa * degradedPower * ethaSys * gamma * derU1TempFactor;

        return derU1;
    }

    public static double DerLDegr(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return 0.0;

        var gPoa = geometryFactor * irradiation;
        var derDegradedPower = installedPower * (- age);
        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var tempFactor = 1 + gamma * (cellTemp - meanTempStc);
        var derLDegr = gPoa * derDegradedPower * ethaSys * tempFactor;

        return derLDegr;
    }

    // EffectivePower and Jacobian
    public static (double effP, double derEtha, double derGamma, double derU0, double derU1, double derLDegr) 
        EffectiveCellPowerAndJacobian(double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys = meanEthaSys, double gamma = meanGamma, double u0 = meanU0, double u1 = meanU1, double lDegr = meanLDegr)
    {
        if (geometryFactor <= 0)
            return (0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

        var gPoa = geometryFactor * irradiation;
        var derDegradedPower = - installedPower * age;
        var degradedPower = installedPower + derDegradedPower * lDegr;

        var cellTemp = ambientTemp + gPoa / (u0 + u1 * windVelocity);
        var derGammaTempFactor = cellTemp - meanTempStc;
        var derU0TempFactor = - gPoa / Math.Pow(u0 + u1 * windVelocity, 2);
        var tempFactor = 1 + gamma * derGammaTempFactor;

        var referencePower = gPoa * degradedPower;
        var derEtha = referencePower * tempFactor;
        var gCell = derEtha * ethaSys;
        var derGamma = referencePower * ethaSys * derGammaTempFactor;
        var derU0 = referencePower * ethaSys * gamma * derU0TempFactor;
        var derU1 = derU0 * windVelocity;
        var derLDegr = gPoa * derDegradedPower * ethaSys * tempFactor;

        return (gCell, derEtha, derGamma, derU0, derU1, derLDegr); ;
    }

}
