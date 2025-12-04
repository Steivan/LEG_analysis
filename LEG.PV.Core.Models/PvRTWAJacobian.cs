using static LEG.PV.Core.Models.PvParameters;
using static LEG.PV.Core.Models.DataRecords;
using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.PV.Core.Models;

public class PvRTWAJacobian                  // Base model: Radiation (direc, diffuse), Temperature, Windspeed, Age
{
    public static (double gDirectPoa, double gDiffusePoa,  double directGeometryFactor, double diffuseGeometryFactor, bool hasValue) 
        GetDecomposedGpoa( MeteoParameters meteoParameters, GeometryFactors geometryFactors)
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
    public static double EffectiveCellPower(
        double installedPower, int periodsPerHour,
        GeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = GetDecomposedGpoa(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        installedPower /= periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = installedPower * (1 - modelParams.LDegr * age);
        var cellTemp = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var tempFactor = (1 + modelParams.Gamma * (cellTemp - meanTempStc));
        var gCell = irradianceRatio * degradedPower * modelParams.Etha * tempFactor;

        return gCell.Value;
    }

    // Numerical Derivative
    public static double GetNumericalDerivative(
        int paramIndexinstalledPower, 
        double installedPower, int periodsPerHour,
        GeometryFactors geometryFactors,
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
        var f1 = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams1);
        var f2 = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams2);

        return (f1 - f2) / delta;
    }

    // Derivativs for Jacobian
    public static double DerEthaSys(double installedPower, int periodsPerHour, 
        GeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = GetDecomposedGpoa(meteoParameters, geometryFactors);
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
        GeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = GetDecomposedGpoa(meteoParameters, geometryFactors);
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
        GeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = GetDecomposedGpoa(meteoParameters, geometryFactors);
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
        GeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = GetDecomposedGpoa(meteoParameters, geometryFactors);
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
        GeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = GetDecomposedGpoa(meteoParameters, geometryFactors);
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

    // EffectivePower and Jacobian
    public static (double effP, double derEtha, double derGamma, double derU0, double derU1, double derLDegr)
        PvJacobianFunc(double installedPower, int periodsPerHour,
        GeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = GetDecomposedGpoa(meteoParameters, geometryFactors);
        if (!hasValue)
            return (0, 0, 0, 0, 0, 0);

        installedPower /= periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var derDegradedPower = - installedPower * age;
        var degradedPower = installedPower + derDegradedPower * modelParams.LDegr;
        var cellTemp = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var derGammaTempFactor = cellTemp - meanTempStc;
        var derU0TempFactor = - gPoa / Math.Pow(modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed.Value, 2);
        var tempFactor = 1 + modelParams.Gamma * derGammaTempFactor;

        var referencePower = irradianceRatio * degradedPower;
        var systemPower = referencePower * modelParams.Etha;
        var derEtha = referencePower * tempFactor;
        var gCell = derEtha * modelParams.Etha;
        var derGamma = systemPower * derGammaTempFactor;
        var derU0 = systemPower * modelParams.Gamma * derU0TempFactor;
        var derU1 = derU0 * meteoParameters.WindSpeed.Value;
        var derLDegr = irradianceRatio * derDegradedPower * modelParams.Etha * tempFactor;

        return (gCell.Value, derEtha.Value, derGamma.Value, derU0, derU1, derLDegr.Value);
    }

}
