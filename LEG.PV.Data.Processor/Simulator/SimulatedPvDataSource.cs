using LEG.PV.Data.Processor.Abstractions;
using static LEG.PV.Core.Models.PvDataClass;

namespace LEG.PV.Data.Processor.Simulator
{
    public class SimulatedPvDataSource : IPvDataSource
    {
        public string SourceName => "Simulation";

        public async Task<IList<PvRecord>> LoadPvRecordsAsync(string siteId, DateTime start, DateTime end)
        {
            // You may need to adapt this to your simulation logic
            return await Task.Run(() =>
            {
                // Example: use your DataSimulator
                var modelParams = DataImporter.PvModelParamsDictionary[siteId];
                var (records, _, _) = DataSimulator.GetPvSimulatedRecords(
                    modelParams,
                    installedPower: 10000,
                    siteLatitude: 46,
                    roofAzimuth: -30,
                    roofElevation: 20,
                    simulationsPeriod: 5,
                    applyRandomNoise: true,
                    applySnowDays: true,
                    applyFoggyDays: true,
                    applyOutliers: true
                );
                return records
                    .Where(r => r.Timestamp >= start && r.Timestamp < end)
                    .ToList();
            });
        }
    }
}
