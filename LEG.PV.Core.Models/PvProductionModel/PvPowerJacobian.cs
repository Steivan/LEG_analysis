using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;
using static LEG.PV.Core.Models.Structures.PvConstants;
using static LEG.PV.Core.Models.Structures.PvModelParamsMetaData;
using LEG.PV.Core.Models.Structures;

namespace LEG.PV.Core.Models.PvProductionModel;

public class PvPowerJacobian                  // Base model: Radiation (direc, diffuse), Temperature, Windspeed, Age
{
    public static double PositiveLogitComplement(double x, double a, double aMin = 1.0, double aMax = 100.0)
    {
        const double nominator = 1.0 + 1.0 / Math.E;        // => enforce f(x=0) = 1
        const double alpha = 1.551445;                      // alpha = ln(2+e) => enforce f(x=a) = 1/2

        x = Math.Max(0.0, Math.Min(aMax * 100.0, x));
        a = Math.Max(aMin, Math.Min(aMax, a));

        return nominator / (1.0 + Math.Exp(alpha * x / a - 1.0));
    }

    public static double PositiveLogit(double x, double a, double aMin = 1.0, double aMax = 100.0)
    {
        return 1.0 - PositiveLogitComplement(x, a, aMin: aMin, aMax: aMax);
    }

    private static (double gDirectPoa, double gDiffusePoa, 
        double directGeometryFactor, double diffuseGeometryFactor, double sinSunElevation,
        bool hasValue) 
        PvModelFilter(MeteoParameters meteoParameters, PvSolarGeometry geometryFactors)
    {
        var directGeometryFactor = geometryFactors.ConstrainedDirectGeometryFactor;
        var diffuseGeometryFactor = geometryFactors.ConstrainedDiffuseGeometryFactor;
        var sinSunElevation = geometryFactors.ConstrainedSinSunElevation;

        var gDirectPoa = meteoParameters.GetDirectPoa(geometryFactors.HasDirectIrradiance, sinSunElevation);
        var gDiffusePoa = meteoParameters.GetDiffusePoa(geometryFactors.HasDiffuseIrradiance);

        gDirectPoa = Math.Min(gDirectPoa, gDiffusePoa * maxDirectToDiffuseRatio);    // TODO: Limit direct POA to avoid extreme ratios if sinSunElevation is very low

        return (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, sinSunElevation, geometryFactors.HasIrradiance);
    }

    public static double GetSnowFactor(double? snowDepth, double dSnow)
    {
        return PositiveLogitComplement(snowDepth.Value, dSnow, aMin: 1.0, aMax: 100.0);
    }

    public static double GetFogFactor(double? dpd, double aFog, double bFog, double kFog)
    {
        return 1.0 - aFog / (1.0 + Math.Exp(kFog * ((dpd?? 2.0) - bFog)));
    }
    // Effective Power
    public static PvPowerRecord EffectiveCellPower(
        double installedPower, int periodsPerHour,
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, 
            directGeometryFactor, diffuseGeometryFactor, sinSunElevation, 
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

        var snowFactor = GetSnowFactor(meteoParameters.SnowDepth, modelParams.DSnow);

        var dpd = meteoParameters.GetDewPointDepression();
        var fogFactor = GetFogFactor(dpd, modelParams.AFog, modelParams.BFog, modelParams.KFog);

        var geometryFactor = Math.Max(directGeometryFactor, sinSunElevation * diffuseRatio);
        var pG = effectivePower * geometryFactor * solarConstantRatio;              // Reference: Geometry and direct irradiance
        var pGR = effectivePower * irradianceRatio;                                 // Baseline: actual direct and diffuse Radiation
        var pGRT = pGR * tempFactorT;                                               // Temperature corrections
        var pGRTW = pGR * tempFactorTW;                                             // Wind speed corrections
        var pGRTWS = pGRTW * snowFactor;                                            // Snow corrections
        var pGRTWSF = pGRTWS * fogFactor;                                           // Fog corrections

        return new PvPowerRecord(pG, pGR, pGRT.Value, pGRTW.Value, pGRTWS.Value, pGRTWSF.Value);
    }

    // Numerical Derivative
    public static double GetNumericalDerivative(
        int modelParameterIndex, 
        double installedPower, int periodsPerHour,
        PvSolarGeometry geometryFactors,
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
        // Snow and fog
        var dSnow1 = modelParams.DSnow;
        var dSnow2 = modelParams.DSnow;
        var ldaAFog1 = modelParams.LambdaAFog;
        var ldaAFog2 = modelParams.LambdaAFog;
        var bFog1 = modelParams.BFog;
        var bFog2 = modelParams.BFog;
        var ldaKFog1 = modelParams.LambdaKFog;
        var ldaKFog2 = modelParams.LambdaKFog;

        var delta = 1e-6;

        switch (modelParameterIndex %= PvModelParamsCount)
        {
            case IndexEtha:
                ethaSys1 += modelSigmas.Etha / 10.0;
                ethaSys2 -= modelSigmas.Etha / 10.0;
                delta = ethaSys1 - ethaSys2;
                break;
            case IndexGamma:
                gamma1 += modelSigmas.Gamma / 10.0;
                gamma2 -= modelSigmas.Gamma / 10.0;
                delta = gamma1 - gamma2;
                break;
            case IndexU0:
                u01 += modelSigmas.U0 / 10.0;
                u02 -= modelSigmas.U0 / 10.0;
                delta = u01 - u02;
                break;
            case IndexU1:
                u11 += modelSigmas.U1 / 10.0;
                u12 -= modelSigmas.U1 / 10.0;
                delta = u11 - u12;
                break;
            case IndexLDegr:
                lDegr1 += modelSigmas.LDegr / 10.0;
                lDegr2 -= modelSigmas.LDegr / 10.0;
                delta = lDegr1 - lDegr2;
                break;
            // Snow and Fog parameters
            case IndexDSnow:
                dSnow1 += modelSigmas.DSnow / 20.0;
                dSnow2 -= modelSigmas.DSnow / 20.0;
                delta = dSnow1 - dSnow2;
                break;
            case IndexLambdaAFog:
                ldaAFog1 += modelSigmas.LambdaAFog / 20.0;
                ldaAFog2 -= modelSigmas.LambdaAFog / 20.0;
                delta = ldaAFog1 - ldaAFog2;
                break;
            case IndexBFog:
                bFog1 += modelSigmas.BFog / 50.0;
                bFog2 -= modelSigmas.BFog / 50.0;
                delta = bFog1 - bFog2;
                break;
            case IndexLambdaKFog:
                ldaKFog1 += modelSigmas.LambdaKFog / 50.0;
                ldaKFog2 -= modelSigmas.LambdaKFog / 50.0;
                delta = ldaKFog1 - ldaKFog2;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(modelParameterIndex), "Invalid parameter index");
        }
        var modelParams1 = new PvModelParams(ethaSys1, gamma1, u01, u11, lDegr1, dSnow1, ldaAFog1, bFog1, ldaKFog1);
        var modelParams2 = new PvModelParams(ethaSys2, gamma2, u02, u12, lDegr2, dSnow2, ldaAFog2, bFog2, ldaKFog2);

        var effectiveCellPower1 = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams1);
        var effectiveCellPower2 = EffectiveCellPower(installedPower, periodsPerHour, geometryFactors, meteoParameters, age, modelParams2);

        // Default used for derDSnow
        var f1 = 0.0;
        var f2 = 0.0;
        if (modelParameterIndex < IndexDSnow)
        {
            f1 = effectiveCellPower1.PowerGRTW;
            f2 = effectiveCellPower2.PowerGRTW;
        }
        else if (modelParameterIndex == IndexDSnow)
        {
            // A Heavyside function is used for snow parameter => Delta function derivative   TODO: improve this
            f1 = 0;
            f2 = 0;
        }
        else
        {
            f1 = effectiveCellPower1.PowerGRTW > 0.0 ? effectiveCellPower1.PowerGRTWSF / effectiveCellPower1.PowerGRTW : 0.0;
            f2 = effectiveCellPower2.PowerGRTW > 0.0 ? effectiveCellPower2.PowerGRTWSF / effectiveCellPower2.PowerGRTW : 0.0;
        }

        return delta != 0 ? (f1 - f2) / delta : 0;
    }

    // Derivativs for Jacobian
    public static double DerEthaSys(double installedPower, int periodsPerHour, 
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, _, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
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
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, _, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
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
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, _, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
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
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, _, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
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
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa, directGeometryFactor, diffuseGeometryFactor, _, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
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

    public static double DerDSnow(double installedPower, int periodsPerHour,
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (_, _, _, _, _, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        return 0.0;
    }

    public static double DerLambdaAFog(double installedPower, int periodsPerHour,
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var(_, _, _, _, _,  hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        var dpd = meteoParameters.GetDewPointDepression();

        return - modelParams.PartialLambdaAFog / (1.0 + Math.Exp(modelParams.KFog * (dpd - modelParams.BFog)));
    }

    public static double DerBFog(double installedPower, int periodsPerHour,
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (_, _, _, _, _, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        var dpd = meteoParameters.GetDewPointDepression();
        var eZ = Math.Exp(modelParams.KFog * (dpd - modelParams.BFog));
        var denom = 1.0 + eZ;

        return -modelParams.AFog * modelParams.KFog * eZ / (denom * denom);                // d fogLoss / d bFog
    }

    public static double DerLambdaKFog(double installedPower, int periodsPerHour,
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (_, _, _, _, _, hasValue) = PvModelFilter(meteoParameters, geometryFactors);
        if (!hasValue)
            return 0.0;

        var dpd = meteoParameters.GetDewPointDepression();
        var eZ = Math.Exp(modelParams.KFog * (dpd - modelParams.BFog));
        var denom = 1.0 + eZ;

        return modelParams.AFog * (dpd - modelParams.BFog) * eZ / (denom * denom) * modelParams.PartialLambdaKFog;          // d fogLoss / d lambdaKFog
    }

    // EffectivePower and Jacobian PvModelParams paramDerivatives
    public static (PvPowerRecord powerRecord, PvModelParams paramDerivatives)
    //public static (PvPowerRecord effP, double derEtha, double derGamma, double derU0, double derU1, double derLDegr)
        PvJacobianFunc(double installedPower, int periodsPerHour,
        PvSolarGeometry geometryFactors,
        MeteoParameters meteoParameters,
        double age,
        PvModelParams modelParams)
    {
        var (gDirectPoa, gDiffusePoa,
            directGeometryFactor, diffuseGeometryFactor, sinSunElevation,
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
        var geometryFactor = Math.Max(directGeometryFactor, sinSunElevation * diffuseRatio);
        var pG = effectivePower * geometryFactor * solarConstantRatio;
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

        // Snow and fog
        var snowFactor = GetSnowFactor(meteoParameters.SnowDepth, modelParams.DSnow);
        var snowDeriv = 0.0;

        var dpd = meteoParameters.GetDewPointDepression();
        var eZ = Math.Exp(modelParams.KFog * (dpd - modelParams.BFog));
        var denom = 1.0 + eZ;
        var fogLoss = modelParams.AFog / denom;
        var aFogfZ = fogLoss * eZ / denom;

        var fogFactor = 1.0 - modelParams.AFog / denom;
        var lambdaAFogDeriv = -1.0 / denom * modelParams.PartialLambdaAFog;                                     // d fogLoss / d lambdaAFog
        var bFogDeriv = -modelParams.KFog * aFogfZ;                                                             // d fogLoss / d bFog
        var lambdaKFogDeriv = (dpd - modelParams.BFog) * aFogfZ * modelParams.PartialLambdaKFog;                // d fogLoss / d lambdaKFog

        var pGRTWS = pGRTW * snowFactor; 
        var pGRTWSF = pGRTWS * fogFactor;

        var powerRecord = new PvPowerRecord(pG, pGR, pGRT.Value, pGRTW.Value, pGRTWS.Value, pGRTWSF.Value);
        var derivativesRecord = new PvModelParams(ethaDeriv.Value, gammaDer.Value, u0Deriv, u1Deriv, lDegrDeriv.Value,
            snowDeriv, lambdaAFogDeriv, bFogDeriv, lambdaKFogDeriv);

        return (powerRecord, derivativesRecord);
    }
}