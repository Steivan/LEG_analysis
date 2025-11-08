using LEG.PV.Data.Processor;
using static LEG.PV.Data.Processor.DataRecords;
using static LEG.PV.Core.Models.PvJacobian;
using static LEG.PV.Core.Models.PvPriorConfig;
using static PV.Calibration.Tool.BayesianCalibrator;
using PV.Calibration.Tool;

var installedPower = 10.0;      // [kWp]
var geometryFactor = 0.7;
var irradiation = 800.0;        // [W/m^2]
var ambientTemp = 35.0;         // [°C]
var windVelocity = 100;         // [km/h]
var age = 5.0;                  // [y]

var (meanEtha, sigmaEtha, minEtha, maxEtha) = GetPriorsEtha();
var (meanGamma, sigmaGamma, minGamma, maxGamma) = GetPriorsGamma();
var (meanU0, sigmaU0, minU0, maxU0) = GetPriorsU0();
var (meanU1, sigmaU1, minU1, maxU1) = GetPriorsU1();
var (meanLDegr, sigmaLDegr, minLDegr, maxLDegr) = GetPriorsLDegr();

double GetNumericalDerivative(int paramIndexinstalledPower, double installedPower, double geometryFactor,
        double irradiation, double ambientTemp, double windVelocity, double age,
        double ethaSys, double gamma, double u0, double u1, double lDegr)
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
    var f1 = EffectiveCellPower(installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: ethaSys1, gamma: gamma1, u0: u01, u1: u11, lDegr: lDegr1);
    var f2 = EffectiveCellPower(installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: ethaSys2, gamma: gamma2, u0: u02, u1: u12, lDegr: lDegr2);

    return (f1 - f2) / delta;
}

var effectivePower = EffectiveCellPower(installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);

var derEtha = DerEthaSys(installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
var derGamma = DerGamma(installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
var derU0 = DerU0(installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
var derU1 = DerU1(installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
var derLDegr = DerLDegr(installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);

var (effectivePowerJac, derEthaJac, derGammaJac, derU0Jac, derU1Jac, derLDegrJac) = PvJacobianFunc(
        installedPower, geometryFactor, irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);

var derEthaNum = GetNumericalDerivative(0, installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
var derGammaNum = GetNumericalDerivative(1, installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
var derU0Num = GetNumericalDerivative(2, installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
var derU1Num = GetNumericalDerivative(3, installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);
var derLDegrNum = GetNumericalDerivative(4, installedPower, geometryFactor,
        irradiation, ambientTemp, windVelocity, age,
        ethaSys: meanEtha, gamma: meanGamma, u0: meanU0, u1: meanU1, lDegr: meanLDegr);

Console.WriteLine($"Effective Power: {effectivePower:F5} {effectivePowerJac - effectivePower:F5}");
Console.WriteLine($"Der EthaSys    : {derEtha:F5} {derEthaJac - derEtha:F5} {derEthaNum - derEtha:F5}");
Console.WriteLine($"Der Gamma      : {derGamma:F5} {derGammaJac - derGamma:F5} {derGammaNum - derGamma:F5}");
Console.WriteLine($"Der U0         : {derU0:F5} {derU0Jac - derU0:F5} {derU0Num - derU0:F5}");
Console.WriteLine($"Der U1         : {derU1:F5} {derU1Jac - derU1:F5} {derU1Num - derU1:F5}");
Console.WriteLine($"Der LDegr      : {derLDegr:F5} {derLDegrJac - derLDegr:F5} {derLDegrNum - derLDegr:F5}");

var pvPriors = new PvPriors();

var thetaModel = new PvModelParams(
    etha: 0.9,
    gamma: -0.005,
    u0: 25,
    u1: 0.4,
    lDegr: 0.01
    );

double tolerance = 1e-6;
int maxIterations = 10;

var (pvRecords, validRecords) = DataSimulator.GetPvSimulatedRecords(thetaModel);

Console.WriteLine("Calibration without Filtering on Valid Records");
var (thetaCalibratedList, iterations) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    pvPriors,
    PvJacobianFunc,
    validRecords: null,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);

PrintResults(pvPriors, thetaModel, thetaCalibratedList, iterations);

Console.WriteLine("Calibration with Filtering on Valid Records (exclude mornings of foggy days)");
(thetaCalibratedList, iterations) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    pvPriors,
    PvJacobianFunc,
    validRecords: validRecords,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);

PrintResults(pvPriors, thetaModel, thetaCalibratedList, iterations);

void PrintResults(PvPriors pvPriors, PvModelParams thetaModel, List<PvModelParams> thetaCalibratedList, int iterations)
{
    var thetaFirst = thetaCalibratedList[0];
    var thetaCalibrated = thetaCalibratedList[^1];
    Console.WriteLine();
    Console.WriteLine($"Calibration Results ({iterations} / {maxIterations} iterations):");
    Console.WriteLine($"Parameter{"prior",10}{"model",10}{"1st it.",10}{"calibrated",15}{"delta %",10}");
    Console.WriteLine($"Etha     {pvPriors.EthaSysMean,10:F5}{thetaModel.Etha,10:F5}{thetaFirst.Etha,10:F5} ... {thetaCalibrated.Etha,10:F5}{(thetaCalibrated.Etha / thetaModel.Etha - 1) * 100,10:F3}");
    Console.WriteLine($"Gamma    {pvPriors.GammaMean,10:F5}{thetaModel.Gamma,10:F5}{thetaFirst.Gamma,10:F5} ... {thetaCalibrated.Gamma,10:F5}{(thetaCalibrated.Gamma / thetaModel.Gamma - 1) * 100,10:F3}");
    Console.WriteLine($"U0       {pvPriors.U0Mean,10:F5}{thetaModel.U0,10:F5}{thetaFirst.U0,10:F5} ... {thetaCalibrated.U0,10:F5}{(thetaCalibrated.U0 / thetaModel.U0 - 1) * 100,10:F3}");
    Console.WriteLine($"U1       {pvPriors.U1Mean,10:F5}{thetaModel.U1,10:F5}{thetaFirst.U1,10:F5} ... {thetaCalibrated.U1,10:F5}{(thetaCalibrated.U1 / thetaModel.U1 - 1) * 100,10:F3}");
    Console.WriteLine($"LDegr    {pvPriors.LDegrMean,10:F5}{thetaModel.LDegr,10:F5}{thetaFirst.LDegr,10:F5} ... {thetaCalibrated.LDegr,10:F5}{(thetaCalibrated.LDegr / thetaModel.LDegr - 1) * 100,10:F3}");
    Console.WriteLine();
}
