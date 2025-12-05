using static LEG.PV.Core.Models.PvConstants;
using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.PV.Core.Models;

public class PvPowerJacobian                  // Base model: Radiation (direc, diffuse), Temperature, Windspeed, Age
{
    public static (double gDirectPoa, double gDiffusePoa,  double directGeometryFactor, double diffuseGeometryFactor, bool hasValue) 
        PvModelFilter( MeteoParameters meteoParameters, PvGeometryFactors geometryFactors)
    {
        // TODO: use sunshineDuration as a reference to adjust the decomposition of Gpoa into direct and diffuse components

        var directGeometryFactor = Math.Max(geometryFactors.DirectGeometryFactor, 0.0);
        var diffuseGeometryFactor = Math.Max(geometryFactors.DiffuseGeometryFactor, 0.0);
        var sinSunElevation = Math.Max(geometryFactors.SinSunElevation, 0.0);

        var hasDirectRadiation = directGeometryFactor > 0 && sinSunElevation > 0;
        var hasDiffuseRadiation = diffuseGeometryFactor > 0;
        var hasValue = hasDirectRadiation || hasDiffuseRadiation;

        var directHorizontalRadiation = Math.Max(0, meteoParameters.GlobalRadiation.Value - meteoParameters.DiffuseRadiation.Value);

        var gDirectPoa = hasDirectRadiation ? directHorizontalRadiation / sinSunElevation : 0.0;
        var gDiffusePoa = hasDiffuseRadiation ? meteoParameters.DiffuseRadiation.Value : 0.0;

        return (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue);
    }
    // Effective Power
    public static PvPowerRecord EffectiveCellPower(
        double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return new PvPowerRecord(0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

        installedPower /= periodsPerHour;
        var degradedPower = installedPower * (1 - modelParams.LDegr * age);
        var effectivePower = degradedPower * modelParams.Etha;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;

        var cellTempT = meteoParameters.Temperature + gPoa / modelParams.U0;
        var tempFactorT = (1 + modelParams.Gamma * (cellTempT - meanTempStc));

        var cellTempTW = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var tempFactorTW = (1 + modelParams.Gamma * (cellTempTW - meanTempStc));

        var pG = effectivePower * (directRatio * directGeometryFactor + diffuseRatio * diffuseGeometryFactor) * solarConstantRatio;
        var pGR = effectivePower * irradianceRatio;
        var pGRT = pGR * tempFactorT;
        var pGRTW = pGR * tempFactorTW;
        var pGRTWF = pGRTW;                         // TODO: add fog factor here
        var pGRTWFS = pGRTWF;                       // TODO: add snow factor here

        return new PvPowerRecord(pG, pGR, pGRT.Value, pGRTW.Value, pGRTWF.Value, pGRTWFS.Value);
    }

    public static double EffectiveCellPowerGRTW(
        double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        return EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams).PowerGRTW;
    }

    // Numerical Derivative
    public static double GetNumericalDerivative(
        int paramIndexinstalledPower, 
        double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams, PvModelParams modelSigmas)
    {
        var ethaSys1 = modelParams.Etha;
        var ethaSys2 = modelParams.Etha;
        var gamma1 = modelParams.Gamma;
        var gamma2 = modelParams.Gamma;
        var u01 = modelParams.U0;
        var u02 = modelParams.U0;
        var u11 = modelParams.U1;
        var u12 = modelParams.U1;
        var lDegr1 = modelParams.LDegr;
        var lDegr2 = modelParams.LDegr;
        var delta = 1e-6;

        switch (paramIndexinstalledPower % 5)
        {
            case 0:
                ethaSys1 += modelSigmas.Etha;
                ethaSys2 -= modelSigmas.Etha;
                delta = 2 * modelSigmas.Etha;
                break;
            case 1:
                gamma1 += modelSigmas.Gamma;
                gamma2 -= modelSigmas.Gamma;
                delta = 2 * modelSigmas.Gamma;
                break;
            case 2:
                u01 += modelSigmas.U0 / 10;
                u02 -= modelSigmas.U0 / 10;
                delta = 2 * modelSigmas.U0 / 10;
                break;
            case 3:
                u11 += modelSigmas.U1 / 10;
                u12 -= modelSigmas.U1 / 10;
                delta = 2 * modelSigmas.U1 / 10;
                break;
            case 4:
                lDegr1 += modelSigmas.LDegr;
                lDegr2 -= modelSigmas.LDegr;
                delta = 2 * modelSigmas.LDegr;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(paramIndexinstalledPower), "Invalid parameter index");
        }
        var modelParams1 = new PvModelParams(ethaSys1,gamma1,u01,u11, lDegr1);
        var modelParams2 = new PvModelParams(ethaSys2, gamma2, u02, u12, lDegr2);
        var f1 = EffectiveCellPowerGRTW(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams1);
        var f2 = EffectiveCellPowerGRTW(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams2);

        return (f1 - f2) / delta;
    }

    // Derivativs for Jacobian
    public static double DerEthaSys(double installedPower, int periodsPerHour, 
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        installedPower /= periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - modelParams.LDegr * age);
        var cellTemp = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var tempFactor = (1 + modelParams.Gamma * (cellTemp - meanTempStc));
        var derEtha = irradianceRatio * degradedPower * tempFactor;

        return derEtha.Value;
    }

    public static double DerGamma(double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        installedPower /= periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - modelParams.LDegr * age);
        var cellTemp = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var derGammaTempFactor = (cellTemp - meanTempStc);
        var derGamma = irradianceRatio * degradedPower * modelParams.Etha * derGammaTempFactor;

        return derGamma.Value;
    }
    public static double DerU0(double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        installedPower /= periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - modelParams.LDegr * age);
        var cellTemp = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var derU0TempFactor = (-gPoa / Math.Pow(modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed.Value, 2));
        var derU0 = irradianceRatio * degradedPower * modelParams.Etha * modelParams.Gamma * derU0TempFactor;

        return derU0;
    }
    public static double DerU1(double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        installedPower /= periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - modelParams.LDegr * age);
        var cellTemp = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var tempFactor = (1 + modelParams.Gamma * (cellTemp - meanTempStc));
        var derU1TempFactor = (-gPoa / Math.Pow(modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed.Value, 2)) * meteoParameters.WindSpeed;
        var derU1 = irradianceRatio * degradedPower * modelParams.Etha * modelParams.Gamma * derU1TempFactor;
            
        return derU1.Value;
    }

    public static double DerLDegr(double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        installedPower /= periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var derDegradedPower = installedPower * (-age);
        var cellTemp = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var tempFactor = 1 + modelParams.Gamma * (cellTemp - meanTempStc);
        var derLDegr = irradianceRatio * derDegradedPower * modelParams.Etha * tempFactor;

        return derLDegr.Value;
    }

    // EffectivePower and Jacobian PvModelParams paramDerivatives
    public static (PvPowerRecord powerRecord, PvModelParams paramDerivatives)
    //public static (PvPowerRecord effP, double derEtha, double derGamma, double derU0, double derU1, double derLDegr)
        PvJacobianFunc(double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return (new PvPowerRecord(0, 0, 0, 0, 0, 0), new PvModelParams(0, 0, 0, 0, 0));

        installedPower /= periodsPerHour;
        //var degradedPower = installedPower * (1 - modelParams.LDegr * age);
        var derDegradedPower = -installedPower * age;
        var degradedPower = installedPower + derDegradedPower * modelParams.LDegr;
        var effectivePower = degradedPower * modelParams.Etha;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;

        var pG = effectivePower * (directRatio * directGeometryFactor + diffuseRatio * diffuseGeometryFactor) * solarConstantRatio;
        var pGR = effectivePower * irradianceRatio;

        var cellTempT = meteoParameters.Temperature + gPoa / modelParams.U0;
        var tempFactorT = (1 + modelParams.Gamma * (cellTempT - meanTempStc));

        var cellTempTW = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var derGammaTempFactor = cellTempTW - meanTempStc;
        var derU0TempFactor = - gPoa / Math.Pow(modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed.Value, 2);
        var tempFactorTW = 1 + modelParams.Gamma * derGammaTempFactor;

        var referencePower = irradianceRatio * degradedPower;
        var systemPower = referencePower * modelParams.Etha;
        var derEtha = referencePower * tempFactorTW;

        var pGRTW = derEtha * modelParams.Etha;

        var derGamma = systemPower * derGammaTempFactor;
        var derU0 = systemPower * modelParams.Gamma * derU0TempFactor;
        var derU1 = derU0 * meteoParameters.WindSpeed.Value;
        var derLDegr = irradianceRatio * derDegradedPower * modelParams.Etha * tempFactorTW;

        var pGRT = pGR * tempFactorT;
        var pGRTWF = pGRTW;                         // TODO: add fog factor here
        var pGRTWFS = pGRTWF;                       // TODO: add snow factor here

        var powerRecord = new PvPowerRecord(pG, pGR, pGRT.Value, pGRTW.Value, pGRTWF.Value, pGRTWFS.Value);
        var derivativesRecord = new PvModelParams(derEtha.Value, derGamma.Value, derU0, derU1, derLDegr.Value);

        return (powerRecord, derivativesRecord);
    }

}