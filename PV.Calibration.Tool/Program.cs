using LEG.PV.Data.Processor;
using PV.Calibration.Tool;
using static LEG.PV.Core.Models.PvJacobian;
using static LEG.PV.Core.Models.PvPriorConfig;
using static LEG.PV.Data.Processor.DataRecords;
using static PV.Calibration.Tool.BayesianCalibrator;

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

var (pvRecords, modelValidRecords) = DataSimulator.GetPvSimulatedRecords(thetaModel);

var defaultPriors = new PvPriors();
var defaultModelParams = GetDefaultPriorModelParams();

var filteredValidRecors = DataFilter.ExcludeSubHorizonRecords(pvRecords);

var initialMeanSquaredError0 = PvErrorStatistics.ComputeMeanError(
    pvRecords,
    filteredValidRecors,
    installedPower,
    defaultModelParams
    );

filteredValidRecors = DataFilter.ExcludeFoggyRecords(
    pvRecords,
    filteredValidRecors,
    installedPower,
    defaultModelParams,
    patternType: 0,
    relativeThreshold: true,
    thresholdType: 2, 
    loThreshold: 0.1, 
    hiThreshold: 0.9);

filteredValidRecors = DataFilter.ExcludeSnowyRecords(
    pvRecords,
    filteredValidRecors,
    installedPower,
    defaultModelParams,
    patternType: 0,
    relativeThreshold: false,
    thresholdType: 2,
    loThreshold: 0.1,
    hiThreshold: 0.8);

filteredValidRecors = DataFilter.ExcludeOutlierRecords(
    pvRecords,
    filteredValidRecors,
    installedPower,
    defaultModelParams,
    periodThreshold: 1.5,
    hourlyThreshold: 1.5,
    blockThreshold: 1.5);

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

Console.WriteLine("Bayesian Calibration: default priors / no filter");
var (thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    defaultPriors,
    PvJacobianFunc,
    validRecords: null,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);
PrintResults(defaultPriors, thetaModel, thetaCalibratedList, iterations, meanError);

Console.WriteLine("Bayesian Calibration: default priors / model filter");
(thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    defaultPriors,
    PvJacobianFunc,
    validRecords: modelValidRecords,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);
var (minError, maxError, meanError0, binSize, binCenters, binCounts) = PvErrorStatistics.ComputeHistograms(
    pvRecords,
    modelValidRecords,
    installedPower,
    thetaCalibratedList[^1],
    countOfBins: 50);
PrintResults(defaultPriors, thetaModel, thetaCalibratedList, iterations, meanError);
Console.WriteLine($"Error Statistics: Min {minError:F5}, Max {maxError:F5} , MeanSquared {meanError0:F5}  ");
Console.WriteLine();

Console.WriteLine("Bayesian Calibration: hull priors / model filter");
(thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    hullPriors,
    PvJacobianFunc,
    validRecords: modelValidRecords,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);
PrintResults(hullPriors, thetaModel, thetaCalibratedList, iterations, meanError);


Console.WriteLine("Bayesian Calibration: default priors / Anomaly detector filters : Fog, Snow, Outliers)");
(thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
    pvRecords: pvRecords,
    defaultPriors,
    PvJacobianFunc,
    validRecords: filteredValidRecors,
    installedPower: installedPower,
    tolerance: tolerance,
    maxIterations: maxIterations);
(minError, maxError, meanError, binSize, binCenters, binCounts) = PvErrorStatistics.ComputeHistograms(
    pvRecords,
    filteredValidRecors,
    installedPower,
    thetaCalibratedList[^1],
    countOfBins: 50);
PrintResults(defaultPriors, thetaModel, thetaCalibratedList, iterations, meanError);
Console.WriteLine($"Error Statistics: Min {minError:F5}, Max {maxError:F5} , MeanSquared {meanError:F5}  ");
Console.WriteLine();

void PrintResults(PvPriors pvPriors, PvModelParams thetaModel, List<PvModelParams> thetaCalibratedList, int iterations, double meanSquaredError)
{
    var thetaFirst = thetaCalibratedList[0];
    var thetaCalibrated = thetaCalibratedList[^1];
    Console.WriteLine($"Calibration Results ({iterations} / {maxIterations} iterations):");
    Console.WriteLine($"Parameter{"prior",10}{"model",10}{"1st it.",10}{"calibrated",15}{"delta %",10}");
    Console.WriteLine($"Etha     {pvPriors.EthaSysMean,10:F5}{thetaModel.Etha,10:F5}{thetaFirst.Etha,10:F5} ... {thetaCalibrated.Etha,10:F5}{(thetaCalibrated.Etha / thetaModel.Etha - 1) * 100,10:F3}");
    Console.WriteLine($"Gamma    {pvPriors.GammaMean,10:F5}{thetaModel.Gamma,10:F5}{thetaFirst.Gamma,10:F5} ... {thetaCalibrated.Gamma,10:F5}{(thetaCalibrated.Gamma / thetaModel.Gamma - 1) * 100,10:F3}");
    Console.WriteLine($"U0       {pvPriors.U0Mean,10:F5}{thetaModel.U0,10:F5}{thetaFirst.U0,10:F5} ... {thetaCalibrated.U0,10:F5}{(thetaCalibrated.U0 / thetaModel.U0 - 1) * 100,10:F3}");
    Console.WriteLine($"U1       {pvPriors.U1Mean,10:F5}{thetaModel.U1,10:F5}{thetaFirst.U1,10:F5} ... {thetaCalibrated.U1,10:F5}{(thetaCalibrated.U1 / thetaModel.U1 - 1) * 100,10:F3}");
    Console.WriteLine($"LDegr    {pvPriors.LDegrMean,10:F5}{thetaModel.LDegr,10:F5}{thetaFirst.LDegr,10:F5} ... {thetaCalibrated.LDegr,10:F5}{(thetaCalibrated.LDegr / thetaModel.LDegr - 1) * 100,10:F3}");
    Console.WriteLine($"Mean Squared Error: {meanSquaredError:F6} (initial: {initialMeanSquaredError0:F6})");
    Console.WriteLine();
}