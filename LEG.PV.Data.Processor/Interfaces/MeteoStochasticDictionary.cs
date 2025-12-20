//using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;
//using static LEG.PV.Data.Processor.Simulator.MeteoRecordSimulator;

//namespace LEG.PV.Data.Processor.Interfaces
//{
//    internal class MeteoStochasticDictionary
//    {
//        const double daysPerYear = 365.2422;
//        const double omegaYear = 2 * Math.PI / daysPerYear;
//        const double omegaDay = 2 * Math.PI / 24.0;

//        public static Dictionary<DateTime, MeteoParameters> GetoSampleDictionary(
//            Dictionary<DateTime, double> sinSunElevationDict)
//        {
//            var support = sinSunElevationDict.Select(kvp => kvp.Key).ToList();
//            support.Sort();
//            var firstTimestamp = support[0];
//            var lastTimestamp = support[^1];
//            var interval = support[1] - support[0];
//            var minutesPerPeriod = (int)interval.TotalMinutes;
//            var timeStampZero = new DateTime(firstTimestamp.Year, 1, 1, 0, 0, 0);

//            MeteoParameters? priortMeteoParameters = null;
//            var meteoDictionary = new Dictionary<DateTime, MeteoParameters>();
//            foreach (var timeStamp in support)
//            {
//                var totalDays = (timeStamp - timeStampZero).TotalDays;
//                var hoursDay = (double)(timeStamp.Hour +timeStamp.Minute / 60.0);
//                var sinSunElevation = sinSunElevationDict[timeStamp];
//                var cosOmegaYear = Math.Cos(omegaYear * totalDays);
//                var cosOmegaDay = Math.Cos(omegaDay * hoursDay);

//                var meteoType = 0; // 0 - stochastic, 1 - clear sky, 2 - overcast sky
//                var isSnowyDay = false;
//                var priorSnowDepth = 0.0;
//                var newSnowPerDay = 0.0;
//                var isFoggyDay = false;
//                var fogDissolveStartHour = 0.0;
//                var fogDissolveEndHour = 0.0;

//                var (meteoParam, snowDepth, weight) = UpdatedMeteoParameters(
//                    timeStamp, 
//                    minutesPerPeriod,
//                    priortMeteoParameters,
//                    sinSunElevation,
//                    cosOmegaYear,
//                    cosOmegaDay,
//                    meteoType,
//                    isSnowyDay,
//                    priorSnowDepth,
//                    newSnowPerDay,
//                    isFoggyDay,
//                    fogDissolveStartHour,
//                    fogDissolveEndHour,
//                    false);
//            }

//            return meteoDictionary;
//        }
//    }
//}
