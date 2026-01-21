using LEG.PV.Core.Models.Structures;

namespace LEG.PV.Core.Models.ConsumptionModel
{
    public class ConsumptionSimulator
    {
        public static Dictionary<DateTime, ConsumptionRecord> SimulateConsumption(string siteId, DateTime simulationStartDateTime, DateTime simulationEndDateTime, int minutesPerPeriod = 15)
        {
            const double omega = 2 * Math.PI / 365.2422;

            double FourierValue(double angle0, double[] a, double[] b)
            {
                double result = 0.0;
                for (int n = 0; n < a.Length; n++)
                {
                    var angle = angle0 * n;
                    result += a[n] * Math.Cos(angle) + b[n] * Math.Sin(angle);
                }
                return result;
            }

            double GetDailyMeanConsumption(
                DateTime dateTime, DateTime yearStart,
                double[] peakHours, double[] variancePeaks,
                List<double[]> aList, List<double[]> bList,     // 0: Baseline, 1..: Peaks
                Dictionary<DayOfWeek, double> weekdayFactors)
            {
                var lagInDays = (dateTime - yearStart).TotalDays + 0.5;
                var dayOfWeek = dateTime.DayOfWeek;
                var hoursOfDay = (double)dateTime.Hour + dateTime.Minute / 60.0;

                var angle0 = omega * lagInDays;
                var baselineConsumption = FourierValue(angle0, aList[0], bList[0]) * weekdayFactors[dayOfWeek];

                var variableConsumption = 0.0;
                for (int i = 0; i < peakHours.Length; i++)
                {
                    var hourDiff = hoursOfDay - peakHours[i];
                    var amplitude = FourierValue(angle0, aList[1 + i], bList[1 + i]);
                    variableConsumption += amplitude * Math.Exp(-(hourDiff * hourDiff) / 2 / variancePeaks[i]);
                }
                variableConsumption *= weekdayFactors[dayOfWeek];

                return baselineConsumption + variableConsumption;
            }

            double GetRandomBetaComponent(double mean, double cv, double upperBound)
            {
                var mu = mean / upperBound;
                var nu = (1.0 / mu - 1.0) / (cv * cv) - 1.0;
                if (nu > 0)
                {
                    var random = new MathNet.Numerics.Distributions.Beta(nu * mu, nu * (1 - mu));

                    return random.Sample() * upperBound;
                }

                return mean;
            }

            // Initialize site-specific parameters
            double maxConsumption;
            double cvConsumption;
            double weightPrior;
            double[] peakHours;
            double[] variancePeaks;
            List<double[]> aList;
            List<double[]> bList;
            Dictionary<DayOfWeek, double> weekdayFactors;

            var simulatedConsumptionDictionary = new Dictionary<DateTime, ConsumptionRecord>();
            ComsuptionModelParameters.GetSitePatameters(siteId,
                out maxConsumption,
                out cvConsumption,
                out weightPrior,
                out peakHours,
                out variancePeaks,
                out aList,
                out bList,
                out weekdayFactors);

            var yearStart = new DateTime(simulationStartDateTime.Year, 1, 1);
            var dateTime = simulationStartDateTime;
            var priorConsumption = 0.0;
            while (dateTime <= simulationEndDateTime)
            {
                var currentMeanConsumption = GetDailyMeanConsumption(dateTime, yearStart, peakHours, variancePeaks, aList, bList, weekdayFactors);
                var currentRandomCosumption = GetRandomBetaComponent(currentMeanConsumption, cvConsumption, maxConsumption);

                priorConsumption = priorConsumption == 0.0 ? currentMeanConsumption : priorConsumption;
                var newConsumption = weightPrior * priorConsumption + (1.0 - weightPrior) * currentRandomCosumption;

                simulatedConsumptionDictionary[dateTime] = new ConsumptionRecord(
                    Solar: 0.0,
                    Consumers: -newConsumption,
                    WallBox: 0.0,
                    Battery: 0.0,
                    Grid: 0.0,
                    Residual: 0.0
                    );

                priorConsumption = newConsumption;
                dateTime = dateTime.AddMinutes(minutesPerPeriod);
            }

            return simulatedConsumptionDictionary;
        }
    }
}