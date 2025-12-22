using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;
using static LEG.PV.Data.Processor.Simulator.MeteoRecordSimulator;
using static LEG.PV.Data.Processor.Simulator.SimulatorParameters;

namespace LEG.PV.Data.Processor.Simulator
{
    internal class MeteoSeriesSimulator
    {
        internal static Dictionary<DateTime, MeteoParameters> GetMeteoSampleDictionary(
            DateTime startDate, TimeSpan interval, int supportCount,
            double siteLatitude = 46,
            double siteLongitude = 10,
            bool applySnowDays = false,
            bool applyFoggyDays = false,
            bool applyOutliers = false)
        {
            var startYear = startDate.Year;
            var minutesPerPeriod = (int)interval.TotalMinutes;
            var periodsPerHour = 60 / minutesPerPeriod;
            var timeStampZero = new DateTime(startYear, 1, 1, 0, 0, 0);
            var endDate = startDate + interval * (supportCount - 1);

            // Initial values
            var random = new Random();
            var meteoTypePerDay = new int[32];
            var firstSnowDay = -1;
            var lastSnowDay = -1;
            var priorSnowDepth = 0.0;
            var newSnowPerDay = 0.0;

            var timeStamp = startDate - TimeSpan.FromMinutes(minutesPerPeriod);
            MeteoParameters? priortMeteoParameters = null;
            var (roundedMeteoParameters, snowDepth, weight) = UpdatedMeteoParameters(
                startYear,
                timeStamp,
                minutesPerPeriod,
                siteLatitude,
                siteLongitude,
                priortMeteoParameters,
                meteoType: 0,
                isSnowyDay: false,
                priorSnowDepth,
                newSnowPerDay,
                isFoggyDay: false,
                fogDissolveStartHour: 0,
                fogDissolveEndHour: 0,
                initialize: true);

            timeStamp = startDate;
            var hourOfDay = timeStamp.Hour;
            var block = hourOfDay / hoursPerBlock;
            var blockHour = hourOfDay % hoursPerBlock;
            var period = timeStamp.Minute / minutesPerPeriod;

            var monthIndex = timeStamp.Month - 1;
            var daysOfMonth = daysPerMonth[monthIndex];
            var isSnowyMonth = averageSnowyowDaysPerMonth[monthIndex] > 0;
            (firstSnowDay, lastSnowDay, meteoTypePerDay) = InitializeMeteoTypesPerDay(monthIndex, isSnowyMonth, random);

            var meteoSeriesDictionary = new Dictionary<DateTime, MeteoParameters>();
            while (timeStamp <= endDate)
            {
                // Diurnal and seasonal variations: Updated once per day
                var day = (int)(timeStamp - startDate).TotalDays;
                var dayOfMonth = timeStamp.Day;

                // Random snow period
                var randomBaselineSnowDepth = random.NextDouble() * 1.0;

                // Snowy days and type of clouds
                if (dayOfMonth == 1)
                {
                    monthIndex = timeStamp.Month - 1; 
                    daysOfMonth = daysPerMonth[monthIndex];
                    isSnowyMonth = averageSnowyowDaysPerMonth[monthIndex] > 0;
                    (firstSnowDay, lastSnowDay, meteoTypePerDay) = InitializeMeteoTypesPerDay(monthIndex, isSnowyMonth, random);
                }
                var isSnowyDay = isSnowyMonth && (firstSnowDay <= dayOfMonth && dayOfMonth <= lastSnowDay);
                newSnowPerDay = applySnowDays ? (isSnowyDay ? minNewSnow + random.NextDouble() * maxNewSnowRandom : randomBaselineSnowDepth) : 0.0; // [cm]

                // Random foggy day
                var randomDayOfMonth = random.Next(1, daysOfMonth + 1);
                var isFoggyDay = randomDayOfMonth <= averageFoggyDaysPerMonth[monthIndex];
                double fogDissolveStartHour = random.Next(fogDissolveStartLo, fogDissolveStartHi + 1);
                double fogDissolveEndHour = random.Next(fogDissolveEndLo, fogDissolveEndHi + 1);

                hourOfDay = timeStamp.Hour;
                block = hourOfDay / hoursPerBlock;
                while (block < blocksPerDay && timeStamp <= endDate)
                {
                    blockHour = hourOfDay % hoursPerBlock;
                    while (blockHour < hoursPerBlock && timeStamp <= endDate)
                    {
                        period = timeStamp.Minute / minutesPerPeriod;
                        while (period < periodsPerHour && timeStamp <= endDate)
                        {
                            // Short-term variations: Updated once per period
                            (roundedMeteoParameters, snowDepth, weight) = UpdatedMeteoParameters(
                                startYear,
                                timeStamp,
                                minutesPerPeriod,
                                siteLatitude,
                                siteLongitude,
                                priortMeteoParameters,
                                meteoType: meteoTypePerDay[dayOfMonth],
                                isSnowyDay: applySnowDays && isSnowyDay,
                                priorSnowDepth: priorSnowDepth,
                                newSnowPerDay: newSnowPerDay,
                                isFoggyDay: applyFoggyDays && isFoggyDay,
                                fogDissolveStartHour: fogDissolveStartHour,
                                fogDissolveEndHour: fogDissolveEndHour,
                                initialize: true);

                            meteoSeriesDictionary[timeStamp] = roundedMeteoParameters with { RadiationVariance = 1.0 / weight };

                            timeStamp += interval;
                            period++;
                        }
                        blockHour++;
                    }
                    block++;
                }                
            }

            return meteoSeriesDictionary;
        }

        private static (int firstSnowDay, int lastSnowDay, int[]meteoTypePerDay) InitializeMeteoTypesPerDay(int monthIndex, bool isSnowyMonth, Random random)
        {
            var daysOfMonth = daysPerMonth[monthIndex];
            var firstSnowDay = 32;
            var lastSnowDay = -1;
            if (isSnowyMonth)
            {
                var durationSnowDays = random.Next(0, 2 * averageSnowyowDaysPerMonth[monthIndex] + 1);
                firstSnowDay = random.Next(1, daysOfMonth - durationSnowDays + 2);
                lastSnowDay = firstSnowDay + durationSnowDays - 1;
            }

            var meteoTypePerDay = new int[32];
            var pClear = (double)averageClearDaysPerMonth[monthIndex] / daysOfMonth;
            var pCovered = (double)averageCoveredDaysPerMonth[monthIndex] / daysOfMonth;
            var pNonMixed = pClear + pCovered;
            if (pClear >= 0.0 && pCovered >= 0.0 && pNonMixed > 0.0)
            {
                var adjustment = pNonMixed > 1.0 ? 1.0 / pNonMixed : 1.0;
                pClear *= adjustment;
                pNonMixed *= adjustment;
                for (int d = 1; d <= daysOfMonth; d++)
                {
                    var r = random.NextDouble();
                    meteoTypePerDay[d] = r <= pClear ? 1 : r <= pNonMixed ? 2 : 0;
                }
            }

            return (firstSnowDay, lastSnowDay, meteoTypePerDay);
        }
    }
}