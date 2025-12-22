using LEG.PV.Core.Models;
using LEG.PV.Data.Processor;
using LEG.PV.Data.Processor.Interfaces;
using MathNet.Numerics.Distributions;
using PV.Calibration.Tool;
using static LEG.PV.Core.Models.PvDataClass;
using static LEG.PV.Core.Models.PvModelParamsMetaData;
using static LEG.PV.Core.Models.PvPriorConfig;
using static PV.Calibration.Tool.BayesianCalibrator;

//ProcessSyntheticModelData(
//    applyRandomNoise: true,
//    applySnowDays: true,
//    applyFoggyDays: true,
//    applyOutliers: !true);

await CalibrateE3DcData(1, "Senn");
await CalibrateE3DcData(2, "SennV");

//ProcessSyntheticModelData();

async Task CalibrateE3DcData(int folder, string label)
{
    var dataImporter = new DataImporter();
    var (siteId, pvRecords, modelValidRecords, installedKwP, periodsPerHour) = await dataImporter.ImportE3DcHistory(folder); // meteoDataLag in multiples of 5 minutes
    var installedPower = installedKwP; // / periodsPerHour;

    var defaultPriors = new PvPriors();
    var defaultModelParams = GetAllPriorsMeans();

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

void ProcessSyntheticModelData(
    int simulationsPeriod = 5,
    bool applyRandomNoise = true,
    bool applySnowDays = true,
    bool applyFoggyDays = true,
    bool applyOutliers = true
    )
{
    var thetaModel = new PvModelParams(
        etha: 0.9,
        gamma: -0.005,
        u0: 25,
        u1: 0.4,
        lDegr: 0.01,
        dSnow: 15.0,
        lambdaAFog: 0.1,
        bFog: 0.5,
        lambdaKFog: 2.0
        );
    var siteId = "SyntheticModelSite";
    var installedKwP = 10.0;      // [kWp]
    var installedPower = installedKwP * 1000;

    var minutesPerPeriod = 15;
    var periodsPerHour = 60 / minutesPerPeriod;
    var now = DateTime.UtcNow;
    var (pvRecords, modelValidRecords) = PvRandomRecordGenerator.GetPvSimulatedRecordsList(
        now.AddYears(-simulationsPeriod),
        now,
        minutesPerPeriod: minutesPerPeriod,
        pvParams: thetaModel,
        siteLatitude: 46,
        siteLongitude: 10,
        installedPower: installedPower,
        roofAzimuth: -30,
        roofElevation: 20,
        applyRandomNoise: applyRandomNoise,
        applySnowDays: applySnowDays,
        applyFoggyDays: applyFoggyDays,
        applyOutliers: applyOutliers
        );

    var defaultPriors = new PvPriors();
    var defaultModelParams = defaultPriors.PriorMeans;

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
        modelValidRecords,
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
    var priorMeans = GetAllPriorsMeans();
    var priorSigmas = GetAllPriorsSigmas();

    var hasModelValidRecords = modelValidRecords != null && modelValidRecords.Any(v => v);

    var (ethaHull, LDegHull, ethaHullUncertainty, LDegHullUncertainty) = HullCalibrator.CalibrateTrend(pvRecords, installedPower, periodsPerHour, priorMeans);

    var hullPriors = new PvPriors
    {
        PriorMeans = priorMeans with 
        { 
            Etha = ethaHull,
            LDegr = LDegHull
        },
        PriorSigmas = priorSigmas with
        {
            Etha = ethaHullUncertainty,
            LDegr = LDegHullUncertainty
        }
    };

    Console.WriteLine();
    Console.WriteLine($"PV Site: {siteId} with {installedPower / 1000:F2} kWp");
    Console.WriteLine("Bayesian Calibration: default priors / no filter");
    var (thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
        pvRecords: pvRecords,
        defaultPriors,
        validRecords: pvRecords.Select(v => true).ToList(),
        installedPower: installedPower,
        periodsPerHour: periodsPerHour,
        tolerance: tolerance,
        maxIterations: maxIterations);

    PrintCalibrationResults(defaultPriors, thetaModel, thetaCalibratedList, iterations, maxIterations, meanError, initialMeanSquaredError);

    var (minError, maxError, meanError0, binSize, binCenters, binCounts) = (0.0, 0.0, 0.0, 0.0, new double[] { }, new int[] { });
    if (hasModelValidRecords)
    {
        Console.WriteLine("Bayesian Calibration: default priors / model filter");
        (thetaCalibratedList, iterations, meanError) = BayesianCalibrator.Calibrate(
            pvRecords: pvRecords,
            defaultPriors,
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
    double meanSquaredError, double initialMeanSquaredError, bool useLambda = true)
{
    void PrintModelParameters(int parameterIndex)
    {
        string name;
        double prior, model, firstIt, calibrated;

        (name, prior) = pvPriors.PriorMeans.GetNameAndValue(parameterIndex, useLambda: useLambda);
        (_, model) = thetaModel.GetNameAndValue(parameterIndex, useLambda: useLambda);
        (_, firstIt) = thetaCalibratedList[0].GetNameAndValue(parameterIndex, useLambda: useLambda);
        (_, calibrated) = thetaCalibratedList[^1].GetNameAndValue(parameterIndex, useLambda: useLambda  );

        Console.WriteLine($"{name,12}{prior,10:F5}{model,10:F5}{firstIt,10:F5} ... {calibrated,10:F5}{(calibrated / model - 1) * 100,10:F3}");
    }

    var thetaFirst = thetaCalibratedList[0];
    var thetaCalibrated = thetaCalibratedList[^1];
    Console.WriteLine($"Calibration Results ({iterations} / {maxIterations} iterations):");
    Console.WriteLine($"Parameter   {"prior",10}{"model",10}{"1st it.",10}{"calibrated",15}{"delta %",10}");
    for (int i = 0; i < PvModelParamsCount; i++)
    {
        PrintModelParameters(i);
    }

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