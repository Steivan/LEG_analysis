using MathNet.Numerics.Distributions;
using LEG.PV.Data.Processor;
using PV.Calibration.Tool;
using static LEG.PV.Core.Models.PvJacobian;
using static LEG.PV.Core.Models.PvPriorConfig;
using static LEG.PV.Data.Processor.DataRecords;
using static PV.Calibration.Tool.BayesianCalibrator;

//ProcessSyntheticModelData();

await CalibrateE3DcData(1, "Senn");
await CalibrateE3DcData(2, "SennV");

//ProcessSyntheticModelData();

async Task CalibrateE3DcData(int folder, string label)
{
    var dataImporter = new DataImporter();
    var (siteId, pvRecords, modelValidRecords, installedKwP, periodsPerHour) = await dataImporter.ImportE3DcHistory(folder); // meteoDataLag in multiples of 5 minutes
    var installedPower = installedKwP; // / periodsPerHour;

    var defaultPriors = new PvPriors();
    var defaultModelParams = GetDefaultPriorModelParams();

    var (filteredValidRecors, initialMeanSquaredError) = GetFilteredRecords(
            pvRecords,
            installedPower,
            periodsPerHour,
            defaultPriors,
            defaultModelParams,
            fogParams: (thresholdType: 2, loThreshold: 0.1, hiThreshold: 0.9),
            snowParams: (thresholdType: 2, loThreshold: 0.1, hiThreshold: 0.8),
            outlierParams: (periodThreshold: 2.5, hourlyThreshold: 2.0, blockThreshold: 1.5)
            );

    ProcessPvData(
        siteId,
        installedPower,
        periodsPerHour,
        pvRecords,
        modelValidRecords: modelValidRecords,
        filteredValidRecors,
        defaultModelParams,
        defaultPriors,
        tolerance: 1e-6,
        maxIterations: 10,
        initialMeanSquaredError
        );
}

void ProcessSyntheticModelData(int simulationsPeriod = 5)
{
    var thetaModel = new PvModelParams(
        etha: 0.9,
        gamma: -0.005,
        u0: 25,
        u1: 0.4,
        lDegr: 0.01
        );
    var siteId = "SyntheticModelSite";
    var installedKwP = 10.0;      // [kWp]
    var installedPower = installedKwP * 1000;

    var (pvRecords, modelValidRecords, periodsPerHour) = DataSimulator.GetPvSimulatedRecords(
        thetaModel, 
        installedPower: installedPower,
        siteLatitude: 46,
        roofAzimuth: -30,
        roofElevation: 20,
        simulationsPeriod: simulationsPeriod,
        applyRandomNoise: true,
        applyFoggyDays: true,
        applySnowDays: true,
        applyOutliers: true
        );

    var defaultPriors = new PvPriors();
    var defaultModelParams = GetDefaultPriorModelParams();

    var (filteredValidRecors, initialMeanSquaredError) = GetFilteredRecords(
            pvRecords,
            installedPower,
            periodsPerHour,
            defaultPriors,
            defaultModelParams,
            fogParams: (thresholdType: 2, loThreshold: 0.1, hiThreshold: 0.9),
            snowParams: (thresholdType: 2, loThreshold: 0.1, hiThreshold: 0.8),
            outlierParams: (periodThreshold: 1.5, hourlyThreshold: 1.5, blockThreshold: 1.5)
            );

    ProcessPvData(
        siteId,
        installedPower,
        periodsPerHour,
        pvRecords,
        modelValidRecords: null,
        filteredValidRecors,
        thetaModel,
        defaultPriors,
        tolerance: 1e-6,
        maxIterations: 10,
        initialMeanSquaredError
        );
}

(List<bool> filteredValidRecors, double initialMeanSquaredError0) 
    GetFilteredRecords(
    List<PvRecord> pvRecords,
    double installedPower,
    int periodsPerHour,
    PvPriors defaultPriors,
    PvModelParams defaultModelParams,
    (int thresholdType, double loThreshold, double hiThreshold) fogParams,
    (int thresholdType, double loThreshold, double hiThreshold) snowParams, 
    (double periodThreshold, double hourlyThreshold, double blockThreshold) outlierParams
    )
{
    var filteredValidRecors = DataFilter.ExcludeSubHorizonRecords(pvRecords);
    var countTrue = filteredValidRecors.Count(v => v == true);

    var initialMeanSquaredError0 = PvErrorStatistics.ComputeMeanError(
        pvRecords,
        filteredValidRecors,
        installedPower,
        periodsPerHour,
        defaultModelParams
        );

    filteredValidRecors = DataFilter.ExcludeFoggyRecords(
        pvRecords,
        filteredValidRecors,
        installedPower,
        periodsPerHour,
        defaultModelParams,
        patternType: 0,
        relativeThreshold: true,
        thresholdType: fogParams.thresholdType,
        loThreshold: fogParams.loThreshold,
        hiThreshold: fogParams.hiThreshold);
    countTrue = filteredValidRecors.Count(v => v == true);

    filteredValidRecors = DataFilter.ExcludeSnowyRecords(
        pvRecords,
        filteredValidRecors,
        installedPower,
        periodsPerHour,
        defaultModelParams,
        patternType: 0,
        relativeThreshold: false,
        thresholdType: snowParams.thresholdType,
        loThreshold: snowParams.loThreshold,
        hiThreshold: snowParams.hiThreshold);
    countTrue = filteredValidRecors.Count(v => v == true);

    filteredValidRecors = DataFilter.ExcludeOutlierRecords(
        pvRecords,
        filteredValidRecors,
        installedPower,
        periodsPerHour,
        defaultModelParams,
        periodThreshold: outlierParams.periodThreshold,
        hourlyThreshold: outlierParams.hourlyThreshold,
        blockThreshold: outlierParams.blockThreshold);
    countTrue = filteredValidRecors.Count(v => v == true);

    return (filteredValidRecors, initialMeanSquaredError0);
}

void ProcessPvData(
    string siteId,
    double installedPower,
    int periodsPerHour,
    List<PvRecord> pvRecords,
    List<bool>? modelValidRecords,
    List<bool> filteredValidRecors,
    PvModelParams thetaModel,
    PvPriors defaultPriors,
    double tolerance,
    int maxIterations,
    double initialMeanSquaredError
    )
{
    var (meanEtha, sigmaEtha, minEtha, maxEtha) = GetPriorsEtha();
    var (meanGamma, sigmaGamma, minGamma, maxGamma) = GetPriorsGamma();
    var (meanU0, sigmaU0, minU0, maxU0) = GetPriorsU0();
    var (meanU1, sigmaU1, minU1, maxU1) = GetPriorsU1();
    var (meanLDegr, sigmaLDegr, minLDegr, maxLDegr) = GetPriorsLDegr();

    var hasModelValidRecords = modelValidRecords != null && modelValidRecords.Any(v => v);

    var (ethaHull, LDegHull, ethaHullUncertainty, LDegHullUncertainty) = HullCalibrator.CalibrateTrend(pvRecords, installedPower, periodsPerHour, GetDefaultPriorModelParams());
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

    Console.WriteLine();
    Console.WriteLine($"PV Site: {siteId} with {installedPower / 1000:F2} kWp");
    Console.WriteLine("Bayesian Calibration: default priors / no filter");
    var (thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
        pvRecords: pvRecords,
        defaultPriors,
        PvJacobianFunc,
        validRecords: null,
        installedPower: installedPower,
        periodsPerHour: periodsPerHour,
        tolerance: tolerance,
        maxIterations: maxIterations);

    //public static (List<PvModelParams> thetaCalibrated, int iterations, double meanSquaredError) Calibrate(
    //    List<PvRecord> pvRecords,
    //    PvPriors pvPriors,
    //    JacobianFunc jacobianFunc,
    //    List<bool>? validRecords = null,
    //    double installedPower = 10.0,
    //    int periodsPerHour = 6,
    //    double tolerance = 1e-6,
    //    int maxIterations = 50)

    PrintCalibrationResults(defaultPriors, thetaModel, thetaCalibratedList, iterations, maxIterations, meanError, initialMeanSquaredError);

    var (minError, maxError, meanError0, binSize, binCenters, binCounts) = (0.0, 0.0, 0.0, 0.0, new double[] { }, new int[] { });
    if (hasModelValidRecords)
    {
        Console.WriteLine("Bayesian Calibration: default priors / model filter");
        (thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
            pvRecords: pvRecords,
            defaultPriors,
            PvJacobianFunc,
            validRecords: modelValidRecords,
            installedPower: installedPower,
            periodsPerHour: periodsPerHour,
            tolerance: tolerance,
            maxIterations: maxIterations);
        (minError, maxError, meanError0, binSize, binCenters, binCounts) = PvErrorStatistics.ComputeHistograms(
            pvRecords,
            modelValidRecords,
            installedPower,
            periodsPerHour,
            thetaCalibratedList[^1],
            countOfBins: 50);
        PrintCalibrationResults(defaultPriors, thetaModel, thetaCalibratedList, iterations, maxIterations, meanError, initialMeanSquaredError);
        Console.WriteLine($"Error Statistics: Min {minError:F5}, Max {maxError:F5} , SdtDev {meanError0:F5}  ");
        Console.WriteLine();

        Console.WriteLine("Bayesian Calibration: hull priors / model filter");
        (thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
            pvRecords: pvRecords,
            hullPriors,
            PvJacobianFunc,
            validRecords: modelValidRecords,
            installedPower: installedPower,
            periodsPerHour: periodsPerHour,
            tolerance: tolerance,
            maxIterations: maxIterations);
        PrintCalibrationResults(hullPriors, thetaModel, thetaCalibratedList, iterations, maxIterations, meanError, initialMeanSquaredError);
    }

    Console.WriteLine("Bayesian Calibration: default priors / Anomaly detector filters : Fog, Snow, Outliers)");
    (thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
        pvRecords: pvRecords,
        defaultPriors,
        PvJacobianFunc,
        validRecords: filteredValidRecors,
        installedPower: installedPower,
        periodsPerHour: periodsPerHour,
        tolerance: tolerance,
        maxIterations: maxIterations);
    PrintCalibrationResults(defaultPriors, thetaModel, thetaCalibratedList, iterations, maxIterations, meanError, initialMeanSquaredError);

    (minError, maxError, meanError, binSize, binCenters, binCounts) = PvErrorStatistics.ComputeHistograms(
        pvRecords,
        filteredValidRecors,
        installedPower,
        periodsPerHour,
        thetaCalibratedList[^1],
        countOfBins: 50);
    PrintStatistics(minError, maxError, meanError, binSize, binCenters, binCounts);

    var pCumulative = new List<double>() { 0.001, 0.01, 0.02, 0.05, 0.1, 0.2, 0.35, 0.5, 0.65, 0.8, 0.9, 0.95, 0.98, 0.99, 0.999 };
    var quantiles = PvErrorStatistics.ComputeQuantiles(
        pvRecords,
        filteredValidRecors,
        installedPower,
        periodsPerHour,
        thetaCalibratedList[^1],
        pCumulative);
    Console.WriteLine();
    PrintQuantiles(pCumulative, quantiles, 0, meanError);
}

// Helper functions for printing results

void PrintCalibrationResults(PvPriors pvPriors, PvModelParams thetaModel, List<PvModelParams> thetaCalibratedList, 
    int iterations, int maxIterations, 
    double meanSquaredError, double initialMeanSquaredError)
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
    Console.WriteLine($"Mean Squared Error: {meanSquaredError:F6} (initial: {initialMeanSquaredError:F6})");
    Console.WriteLine();
}

void PrintStatistics(double minError, double maxError, double meanError, double binSize, double[] binCenters, int[] binCounts)
{
    Console.WriteLine("Error Histogram");
    Console.WriteLine($"{"bin center",12} {"count",8}");
    for (int i = 0; i < binCenters.Length; i++)
    {
        Console.WriteLine($"{binCenters[i],12:F5} {binCounts[i],8}");
    }
    Console.WriteLine();
    Console.WriteLine($"Min Error : {minError:F5}");
    Console.WriteLine($"Max Error : {maxError:F5}");
    Console.WriteLine($"Mean Error: {meanError:F5}");
    Console.WriteLine($"Bin Size  : {binSize:F5}");
    Console.WriteLine();
}
void PrintQuantiles(List<double> pCumulative, List<double> quantiles, double mean, double stdDev)
{
    var normal = new Normal(mean, stdDev);
    Console.WriteLine("Quantiles");
    Console.WriteLine($"{"probability",12} {"quantile",12} {"inverse N",12}");
    for (int i = 0; i < pCumulative.Count; i++)
    {
        Console.WriteLine($"{pCumulative[i],12:P3} {quantiles[i],12:F5} {normal.InverseCumulativeDistribution(pCumulative[i]),12:F5}");
    }
    Console.WriteLine();
}