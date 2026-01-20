using LEG.PV.Core.Models.Structures;

namespace LEG.PV.Core.Models.ConsumptionModel
{
    public class ConsumptionSimulator
    {
        public static Dictionary<DateTime, ConsumptionRecord> SimulateConsumption(string siteId, DateTime simulationStartDateTime, DateTime simulationEndDateTime, int minutesPerPeriod = 15)
        {

            var simulatedConsumptionDictionary = new Dictionary<DateTime, ConsumptionRecord>();
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

            /*
             Calibration SennV:
                Fourier coefficients for Baseline
                 - a: 518.7, -9.9, 13.4, -12.3, -17.3
                 - b: 0.0, 38.2, 31.8, 10.5, 4.3
                Fourier coefficients for Peak 1 at 8.0 , variance 5.0
                 - a: 297.0, -15.9, -63.0, 8.4, -20.2
                 - b: 0.0, 7.1, 19.9, -2.5, 0.1
                Fourier coefficients for Peak 2 at 14.0 , variance 5.0
                 - a: 500.6, 68.7, -36.8, 42.1, -7.8
                 - b: 0.0, -10.8, 20.8, 10.7, 15.5
                Fourier coefficients for Peak 3 at 20.0 , variance 5.0
                 - a: 441.8, 77.1, -53.4, 39.1, -4.6
                 - b: 0.0, -2.8, 6.8, 19.9, -15.7

                Weekday factors: 0.960, 0.976, 1.019, 1.076, 0.989, 1.003, 0.976
            */
            var maxConsumption = 10000.0;
            var cvConsumption = 1.0;
            var weightPrior = 0.8;
            var aBaseline = new double[] { 518.7, -9.9, 13.4, -12.3, -17.3 };
            var bBaseline = new double[] {   0.0, 38.2, 31.8,  10.5,   4.3 };
            var peakHours = new double[] { 8.0, 14.0, 20.0 };
            var variancePeaks = 5.0;
            var aList = new List<double[]>()
                        { new double[] { 297.0, -15.9, -63.0,  8.4, -20.2 },
                          new double[] { 500.6,  68.7, -36.8, 42.1,  -7.8 },
                          new double[] { 441.8,  77.1, -53.4, 39.1,  -4.6 }
                        };
            var bList = new List<double[]>()
                        { new double[] { 0.0,   7.1, 19.9, -2.5,   0.1 },
                          new double[] { 0.0, -10.8, 20.8, 10.7,  15.5 },
                          new double[] { 0.0,  -2.8,  6.8, 19.9, -15.7 }
                        };
            var weekdayFactors = new Dictionary<DayOfWeek, double>()  
                        {
                            { DayOfWeek.Sunday,    0.960 },
                            { DayOfWeek.Monday,    0.976 },
                            { DayOfWeek.Tuesday,   1.019 },
                            { DayOfWeek.Wednesday, 1.076 },
                            { DayOfWeek.Thursday,  0.989 },
                            { DayOfWeek.Friday,    1.003 },
                            { DayOfWeek.Saturday,  0.976 }
                        };
            const double omega = 2 * Math.PI / 365.2422;
            var yearStart = new DateTime(simulationStartDateTime.Year, 1, 1);

            var dateTime = simulationStartDateTime;
            var priorConsumption = 0.0;
            while (dateTime <= simulationEndDateTime)
            {
                var dayOfYear = (dateTime - yearStart).TotalDays + 0.5;
                var dayOfWeek = dateTime.DayOfWeek;
                var hoursOfDay = (double)dateTime.Hour + dateTime.Minute / 60.0;

                var angle0 = omega * dayOfYear;
                var baselineConsumption = FourierValue(angle0, aBaseline, bBaseline) * weekdayFactors[dayOfWeek];
                var variableConsumption = FourierValue(angle0, aBaseline, bBaseline);
                for (int i = 0; i < peakHours.Length; i++)
                {
                    var hourDiff = hoursOfDay - peakHours[i];
                    var amplitude = FourierValue(angle0, aList[i], bList[i]);
                    variableConsumption += amplitude * Math.Exp(-(hourDiff * hourDiff) / 2 / variancePeaks);
                }
                variableConsumption *= weekdayFactors[dayOfWeek];

                var betaUpperBound = maxConsumption - baselineConsumption;
                var randomConsumption = baselineConsumption;
                if (betaUpperBound > 0)
                {
                    var muBeta = variableConsumption / betaUpperBound;
                    var sigmaBeta = muBeta * cvConsumption;
                    var nuBeta = muBeta * (1 - muBeta) / (sigmaBeta * sigmaBeta) - 1.0;
                    if (nuBeta > 0)
                    {
                        var alphaBeta = nuBeta * muBeta;
                        var betaBeta = nuBeta * (1 - muBeta);
                        var randomBeta = new MathNet.Numerics.Distributions.Beta(alphaBeta, betaBeta);
                        randomConsumption += randomBeta.Sample() * betaUpperBound;
                    }
                }
                    
                var newConsumption = priorConsumption == 0.0 ? randomConsumption : weightPrior * priorConsumption + (1 - weightPrior) * randomConsumption;

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
