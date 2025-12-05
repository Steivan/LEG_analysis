using static LEG.PV.Core.Models.PvConstants;
using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.PV.Core.Models;

public class PvPowerJacobian                  // Base model: Radiation (direc, diffuse), Temperature, Windspeed, Age
{
    public static (double gDirectPoa, double gDiffusePoa, 
        double directGeometryFactor, double diffuseGeometryFactor, 
        bool hasValue) 
        PvModelFilter(MeteoParameters meteoParameters, PvGeometryFactors geometryFactors)
    {
        // TODO: use sunshineDuration as a reference to adjust the decomposition of Gpoa into direct and diffuse component
        // TODO: use direct normal irradiance if available

        var directGeometryFactor = Math.Max(geometryFactors.DirectGeometryFactor, 0.0);
        var diffuseGeometryFactor = Math.Max(geometryFactors.DiffuseGeometryFactor, 0.0);
        var sinSunElevation = Math.Max(geometryFactors.SinSunElevation, 0.0);

        var hasDirectRadiation = directGeometryFactor > 0;
        var hasDiffuseRadiation = diffuseGeometryFactor > 0 && sinSunElevation > 0;
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
        var (gDirectPoa, gDiffusePoa, 
            directGeometryFactor, diffuseGeometryFactor, 
            hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return new PvPowerRecord(0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

        var periodPower = installedPower / periodsPerHour;

        var degradedPower = periodPower * (1 - modelParams.LDegr * age);
        var effectivePower = degradedPower * modelParams.Etha;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;

        var cellTempT = meteoParameters.Temperature + gPoa / modelParams.U0;
        var tempFactorT = (1 + modelParams.Gamma * (cellTempT - meanTempStc));

        var cellTempTW = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var tempFactorTW = (1 + modelParams.Gamma * (cellTempTW - meanTempStc));

        var dpd = meteoParameters.DewPoint.HasValue ? meteoParameters.Temperature.Value - meteoParameters.DewPoint.Value : 5.0;
        var fogFactor = 1.0 - modelParams.AFog / (1.0 + Math.Exp(modelParams.KFog * (dpd - modelParams.BFog)));
        var snowFactor = meteoParameters.SnowDepth >= modelParams.DSnow ? 0.0 : 1.0;

        var pG = effectivePower * directGeometryFactor * solarConstantRatio;
        var pGR = effectivePower * irradianceRatio;
        var pGRT = pGR * tempFactorT;
        var pGRTW = pGR * tempFactorTW;
        var pGRTWF = pGRTW * fogFactor;
        var pGRTWFS = pGRTWF * snowFactor;

        return new PvPowerRecord(pG, pGR, pGRT.Value, pGRTW.Value, pGRTWF.Value, pGRTWFS.Value);
    }

    // Numerical Derivative
    public static double GetNumericalDerivative(
        int modelParameterIndex, 
        double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams, PvModelParams modelSigmas)
    {
        modelParameterIndex %= 9;

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
        // Fog and Snow
        var ldaAFog1 = modelParams.LambdaAFog;
        var ldaAFog2 = modelParams.LambdaAFog;
        var bFog1 = modelParams.BFog;
        var bFog2 = modelParams.BFog;
        var ldaKFog1 = modelParams.LambdaKFog;
        var ldaKFog2 = modelParams.LambdaKFog;
        var ldaDSnow1 = modelParams.LambdaDSnow;
        var ldaDSnow2 = modelParams.LambdaDSnow;

        var delta = 1e-6;

        switch (modelParameterIndex)
        {
            case 0:
                ethaSys1 += modelSigmas.Etha / 10.0;
                ethaSys2 -= modelSigmas.Etha / 10.0;
                delta = ethaSys1 - ethaSys2;
                break;
            case 1:
                gamma1 += modelSigmas.Gamma / 10.0;
                gamma2 -= modelSigmas.Gamma / 10.0;
                delta = gamma1 - gamma2;
                break;
            case 2:
                u01 += modelSigmas.U0 / 10.0;
                u02 -= modelSigmas.U0 / 10.0;
                delta = u01 - u02;
                break;
            case 3:
                u11 += modelSigmas.U1 / 10.0;
                u12 -= modelSigmas.U1 / 10.0;
                delta = u11 - u12;
                break;
            case 4:
                lDegr1 += modelSigmas.LDegr / 10.0;
                lDegr2 -= modelSigmas.LDegr / 10.0;
                delta = lDegr1 - lDegr2;
                break;
            // Fog and Snow
            case 5:
                ldaAFog1 += modelSigmas.LambdaAFog / 20.0;
                ldaAFog2 -= modelSigmas.LambdaAFog / 20.0;
                delta = ldaAFog1 - ldaAFog2;
                break;
            case 6:
                bFog1 += modelSigmas.BFog / 50.0;
                bFog2 -= modelSigmas.BFog / 50.0;
                delta = bFog1 - bFog2;
                break;
            case 7:
                ldaKFog1 += modelSigmas.LambdaKFog / 50.0;
                ldaKFog2 -= modelSigmas.LambdaKFog / 50.0;
                delta = ldaKFog1 - ldaKFog2;
                break;
            case 8:
                ldaDSnow1 += modelSigmas.LambdaDSnow / 50.0;
                ldaDSnow2 -= modelSigmas.LambdaDSnow / 50.0;
                delta = ldaDSnow1 - ldaDSnow2;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(modelParameterIndex), "Invalid parameter index");
        }
        var modelParams1 = new PvModelParams(ethaSys1, gamma1, u01, u11, lDegr1, ldaAFog1, bFog1, ldaKFog1, ldaDSnow1);
        var modelParams2 = new PvModelParams(ethaSys2, gamma2, u02, u12, lDegr2, ldaAFog2, bFog2, ldaKFog2, ldaDSnow2);

        var pGRTW1 = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams1).PowerGRTW;
        var pGRTW2 = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams2).PowerGRTW;
        var f1 = 0.0;
        var f2 = 0.0;
        if (modelParameterIndex < 5)
        {
            f1 = pGRTW1;
            f2 = pGRTW2;
        }
        else if(modelParameterIndex < 8) 
        {
            var pGRTW1FS = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams1).PowerGRTWFS;
            var pGRTW2FS = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams2).PowerGRTWFS;
            f1 = pGRTW1 > 0.0 ? pGRTW1FS / pGRTW1 : 0.0;
            f2 = pGRTW2 > 0.0 ? pGRTW2FS / pGRTW2 : 0.0;
        }
        // A Heavyside function is used for snow parameter => Delta function derivative   TODO: improve this

        return delta != 0 ? (f1 - f2) / delta : 0;
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

        var periodPower = installedPower / periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = periodPower * (1 - modelParams.LDegr * age);
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

        var periodPower = installedPower / periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = periodPower * (1 - modelParams.LDegr * age);
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

        var periodPower = installedPower / periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = periodPower * (1 - modelParams.LDegr * age);
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

        var periodPower = installedPower / periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var degradedPower = periodPower * (1 - modelParams.LDegr * age);
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

        var periodPower = installedPower / periodsPerHour;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;
        var derDegradedPower = periodPower * (-age);
        var cellTemp = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var tempFactor = 1 + modelParams.Gamma * (cellTemp - meanTempStc);
        var derLDegr = irradianceRatio * derDegradedPower * modelParams.Etha * tempFactor;

        return derLDegr.Value;
    }

    public static double DerLambdaAFog(double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        var dpd = meteoParameters.DewPoint.HasValue ? meteoParameters.Temperature.Value - meteoParameters.DewPoint.Value : 5.0;

        return - modelParams.PartialAFog / (1.0 + Math.Exp(modelParams.KFog * (dpd - modelParams.BFog)));
    }

    public static double DerBFog(double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        var dpd = meteoParameters.DewPoint.HasValue ? meteoParameters.Temperature.Value - meteoParameters.DewPoint.Value : 5.0;
        var eZ = Math.Exp(modelParams.KFog * (dpd - modelParams.BFog));
        var denom = 1.0 + eZ;

        return -modelParams.AFog * modelParams.KFog * eZ / (denom * denom);                // d fogLoss / d bFog
    }

    public static double DerLambdaKFog(double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        var dpd = meteoParameters.DewPoint.HasValue ? meteoParameters.Temperature.Value - meteoParameters.DewPoint.Value : 5.0;
        var eZ = Math.Exp(modelParams.KFog * (dpd - modelParams.BFog));
        var denom = 1.0 + eZ;

        return modelParams.AFog * (dpd - modelParams.BFog) * eZ / (denom * denom) * modelParams.PartialKFog;          // d fogLoss / d lambdaKFog
    }

    public static double DerLambdaDSnow(double installedPower, int periodsPerHour,
        PvGeometryFactors geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        var snowDeriv = 0.0;

        return snowDeriv;
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
        var (gDirectPoa, gDiffusePoa,
            directGeometryFactor, diffuseGeometryFactor,
            hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return (new PvPowerRecord(0, 0, 0, 0, 0, 0), new PvModelParams(0, 0, 0, 0, 0));

        // Degradation and system efficiency
        var periodPower = installedPower / periodsPerHour;

        var degradedPowerDeriv = -periodPower * age;
        var degradedPower = periodPower + degradedPowerDeriv * modelParams.LDegr;
        var effectivePower = degradedPower * modelParams.Etha;

        var gPoa = gDirectPoa * directGeometryFactor + gDiffusePoa * diffuseGeometryFactor;
        var irradianceRatio = gPoa / baselineIrradiance;

        // Geometry and radiation
        var pG = effectivePower * directGeometryFactor * solarConstantRatio;
        var pGR = effectivePower * irradianceRatio;

        // Temperature and windspeed
        var cellTempT = meteoParameters.Temperature + gPoa / modelParams.U0;
        var tempFactorT = (1 + modelParams.Gamma * (cellTempT - meanTempStc));

        var cellTempTW = meteoParameters.Temperature + gPoa / (modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed);
        var gammaTempFactorDeriv = cellTempTW - meanTempStc;
        var u0TempFactorDeriv = - gPoa / Math.Pow(modelParams.U0 + modelParams.U1 * meteoParameters.WindSpeed.Value, 2);
        var tempFactorTW = 1 + modelParams.Gamma * gammaTempFactorDeriv;

        var referencePower = irradianceRatio * degradedPower;
        var systemPower = referencePower * modelParams.Etha;
        var ethaDeriv = referencePower * tempFactorTW;

        var pGRT = pGR * tempFactorT;
        var pGRTW = ethaDeriv * modelParams.Etha;

        var gammaDer = systemPower * gammaTempFactorDeriv;
        var u0Deriv = systemPower * modelParams.Gamma * u0TempFactorDeriv;
        var u1Deriv = u0Deriv * meteoParameters.WindSpeed.Value;
        var lDegrDeriv = irradianceRatio * degradedPowerDeriv * modelParams.Etha * tempFactorTW;

        // Fog and Snow
        var dpd = meteoParameters.DewPoint.HasValue ? meteoParameters.Temperature.Value - meteoParameters.DewPoint.Value : 5.0;
        var eZ = Math.Exp(modelParams.KFog * (dpd - modelParams.BFog));
        var denom = 1.0 + eZ;
        var fogLoss = modelParams.AFog / denom;
        var aFogfZ = fogLoss * eZ / denom;

        var fogFactor = 1.0 - modelParams.AFog / denom;
        var lambdaAFogDeriv = -1.0 / denom * modelParams.PartialAFog;                                   // d fogLoss / d lambdaAFog
        var bFogDeriv = -modelParams.KFog * aFogfZ;                                                     // d fogLoss / d bFog
        var lambdaKFogDeriv = (dpd - modelParams.BFog) * aFogfZ * modelParams.PartialKFog;              // d fogLoss / d lambdaKFog

        var snowFactor = meteoParameters.SnowDepth >= modelParams.DSnow ? 0.0 : 1.0;
        var snowDeriv = 0.0;

        var pGRTWF = pGRTW * fogFactor; 
        var pGRTWFS = pGRTWF * snowFactor;

        var powerRecord = new PvPowerRecord(pG, pGR, pGRT.Value, pGRTW.Value, pGRTWF.Value, pGRTWFS.Value);
        var derivativesRecord = new PvModelParams(ethaDeriv.Value, gammaDer.Value, u0Deriv, u1Deriv, lDegrDeriv.Value, 
            lambdaAFogDeriv, bFogDeriv, lambdaKFogDeriv, snowDeriv);

        return (powerRecord, derivativesRecord);
    }

}