using LEG.PV.Data.Processor;
using static LEG.PV.Data.Processor.DataRecords;
using static LEG.PV.Core.Models.PvJacobian;
using static LEG.PV.Core.Models.PvPriorConfig;
using static PV.Calibration.Tool.BayesianCalibrator;
using PV.Calibration.Tool;

var installedPower = 10.0;      // [kWp]

var (meanEtha, sigmaEtha, minEtha, maxEtha) = GetPriorsEtha();
var (meanGamma, sigmaGamma, minGamma, maxGamma) = GetPriorsGamma();
var (meanU0, sigmaU0, minU0, maxU0) = GetPriorsU0();
var (meanU1, sigmaU1, minU1, maxU1) = GetPriorsU1();
var (meanLDegr, sigmaLDegr, minLDegr, maxLDegr) = GetPriorsLDegr();

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

var defaultPriors = new PvPriors();
var defaultModelParams = GetDefaultPriorModelParams();

var (ethaHull, LDegHull, ethaHullUncertainty, LDegHullUncertainty) = HullCalibrator.CalibrateTrend(pvRecords, installedPower, GetDefaultPriorModelParams());
var hullPriors = new PvPriors
{
    EthaSysMean = ethaHull,
    EthaSysStdDev = ethaHullUncertainty,
    LDegrMean = LDegHull,
    LDegrStdDev = LDegHullUncertainty,
    GammaMean = meanGamma,
    GammaStdDev = sigmaGamma,
    U0Mean = meanU0,
    U0StdDev = sigmaU0,
    U1Mean = meanU1,
    U1StdDev = sigmaU1
};

Console.WriteLine("Calibration without Filtering on Valid Records: default priors");
var (thetaCalibratedList, iterations) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    defaultPriors,
    PvJacobianFunc,
    validRecords: null,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);

PrintResults(defaultPriors, thetaModel, thetaCalibratedList, iterations);

Console.WriteLine("Calibration with Filtering on Valid Records (exclude mornings of foggy days): hull calibration");
(thetaCalibratedList, iterations) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    defaultPriors,
    PvJacobianFunc,
    validRecords: validRecords,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);

PrintResults(defaultPriors, thetaModel, thetaCalibratedList, iterations);

Console.WriteLine("Calibration without Filtering on Valid Records: hull calibration");
(thetaCalibratedList, iterations) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    hullPriors,
    PvJacobianFunc,
    validRecords: null,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);

PrintResults(hullPriors, thetaModel, thetaCalibratedList, iterations);

Console.WriteLine("Calibration with Filtering on Valid Records (exclude mornings of foggy days): hull calibration");
(thetaCalibratedList, iterations) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    hullPriors,
    PvJacobianFunc,
    validRecords: validRecords,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);

PrintResults(hullPriors, thetaModel, thetaCalibratedList, iterations);

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