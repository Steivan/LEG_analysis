using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using SolarProductionTestApp;
using LEG.HorizonProfiles.Client;
using LEG.CoreLib.SampleData;
using LEG.CoreLib.SampleData.SampleData;

const int pythonReferenceYear = 2022;
const int pythonEvaluationStartHour = 5;  // -> to match python script
const int pythonEvaluationEndHour = 22;
const int pythonMinutesPerPeriod = 10;

// Use the getter from the SampleData project: Bagnera, Bos_cha, Clozza, Ftan, Fuorcla, Guldenen, Liuns, Lotz, Senn, SennV, TestSite, Tof, "Manual"
var siteOptions = PvSiteModelGetters.GetSitesList();

var sampleId = siteOptions[8];

const int evaluationYear = pythonReferenceYear;
const int evaluationStartHour = 4;
const int evaluationEndHour = 22;
const int minutesPerPeriod = 10;

var siteModel = await PvSiteModelGetters.GetSiteDataModelAsync(sampleId);

// Instantiate the HorizonProfileClient and the new data providers
var apiKey = Environment.GetEnvironmentVariable("GOOGLE_ELEVATION_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Google Elevation API key is not set. Please set the 'GOOGLE_ELEVATION_API_KEY' environment variable.");
    return;
}
var horizonClient = new HorizonProfileClient(googleApiKey: apiKey!);
var coordinateProvider = new SampleSiteCoordinateProvider();
var horizonControlProvider = new SampleSiteHorizonControlProvider();

Console.WriteLine($"Testing the Solar Project: PvSiteModel {siteModel.PvSite.SystemName} with {siteModel.PvSite.IndicativeNrOfRoofs} roof(s) and {siteModel.Inverters.Count} inverter(s).");
Console.WriteLine();

SolarProductionAggregateResults? productionResults = null;
for (var year = evaluationYear - 4; year <= evaluationYear; year++)
{
    Console.WriteLine($"Evaluation evaluationYear: {year}:");
    var results = await siteModel.ComputePvSiteAggregateProductionPerRoof(
        horizonClient,
        coordinateProvider,
        horizonControlProvider,
        evaluationYear: year,
        evaluationStartHour: evaluationStartHour,
        evaluationEndHour: evaluationEndHour,
        minutesPerPeriod: minutesPerPeriod,
        print: false
        );

    productionResults = results;

    Console.WriteLine($"    Total : Theoretical = {(int)Math.Round(results.TheoreticalYear[0])} [kWh], Effective = {(int)Math.Round(results.EffectiveYear[0])} [kWh]");
    Console.WriteLine();
}

if (sampleId == "TestSite")
{
    Console.WriteLine("Comparison with PYTHON");
    var pythonComparisonResults = await siteModel.ComputePvSiteAggregateProductionPerRoof(
        horizonClient,
        coordinateProvider,
        horizonControlProvider,
        evaluationYear: pythonReferenceYear,
        evaluationStartHour: pythonEvaluationStartHour,
        evaluationEndHour: pythonEvaluationEndHour,
        minutesPerPeriod: pythonMinutesPerPeriod,
        print: false
        );
    Console.WriteLine(
        $"Calculation performed for site in {pythonComparisonResults.Town} with {pythonComparisonResults.DimensionRoofs} roof(s):");
    for (var roof = 1; roof <= pythonComparisonResults.DimensionRoofs; roof++)
    {
        Console.WriteLine(
            $"  - PvRoof {roof}: Theoretical = {(int)Math.Round(pythonComparisonResults.TheoreticalYear[roof])} [kWh], Effective = {(int)Math.Round(pythonComparisonResults.EffectiveYear[roof])} [kWh].");
    }

    Console.WriteLine(
        $"    Total : Theoretical = {(int)Math.Round(pythonComparisonResults.TheoreticalYear[0])} [kWh], Effective = {(int)Math.Round(pythonComparisonResults.EffectiveYear[0])} [kWh] (Python: 34762).");
}

if (productionResults != null)
    PlotProductionProfiles.ProductionProfilePlot(productionResults);
else
    Console.WriteLine("No production results to plot.");

await PlotHorizonAndSunProfiles.HorizonAndSunProfilesPlot(siteModel, evaluationYear, coordinateProvider, horizonControlProvider);
