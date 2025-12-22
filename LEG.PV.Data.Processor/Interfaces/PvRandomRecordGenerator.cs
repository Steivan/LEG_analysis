using LEG.PV.Core.Models;
using static LEG.PV.Core.Models.PvDataClass;
using static LEG.PV.Data.Processor.Simulator.PvProductionSimulator;

namespace LEG.PV.Data.Processor.Interfaces
{
    public class PvRandomRecordGenerator
    {
        public static (List<PvRecord> dataRecord, List<bool> validRecord) GetPvSimulatedRecordsList(
            DateTime startTime,
            DateTime endTime,
            int minutesPerPeriod,
            PvModelParams pvParams,
            double siteLatitude = 46,
            double siteLongitude = 10,
            double installedPower = 10000,
            double roofAzimuth = -30,
            double roofElevation = 20,
            bool applyRandomNoise = false,
            bool applySnowDays = false,
            bool applyFoggyDays = false,
            bool applyOutliers = false)
        {
            var (pvRecordsDictionary, pvValidReordDictionary) = GetPvSimulatedRecordsDictionary(
                startTime,
                endTime,
                minutesPerPeriod,
                pvParams,
                siteLatitude,
                siteLongitude,
                installedPower,
                roofAzimuth,
                roofElevation,
                applyRandomNoise,
                applySnowDays,
                applyFoggyDays,
                applyOutliers);

            return (
                pvRecordsDictionary.Select(kvp => kvp.Value).ToList(),
                pvValidReordDictionary.Select(kvp => kvp.Value).ToList()
                );
        }
    }
}
