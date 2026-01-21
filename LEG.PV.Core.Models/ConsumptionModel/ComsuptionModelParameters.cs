
namespace LEG.PV.Core.Models.ConsumptionModel
{
    internal class ComsuptionModelParameters
    {
        internal static void GetSitePatameters(
            string siteId,
            out double maxConsumption,
            out double cvConsumption,
            out double weightPrior,
            out double[] peakHours,
            out double[] variancePeaks,
            out List<double[]> aList,
            out List<double[]> bList,
            out Dictionary<DayOfWeek, double> weekdayFactors)
        {
            switch (siteId)
            {
                case "Senn":
                    /*
                     Calibration Senn:
                        Fourier coefficients for Baseline
                         - a: 102.1, 9.8, 3.8, 4.6, 3.9
                         - b: 0.0, -1.7, 0.6, -12.2, 4.7
                        Fourier coefficients for Peak 1 at 8.0 , variance 5.0
                         - a: 86.7, 13.0, -15.3, 10.9, 3.0
                         - b: 0.0, 8.8, -10.5, 9.4, -8.0
                        Fourier coefficients for Peak 2 at 14.0 , variance 5.0
                         - a: 79.8, 23.2, 2.3, 6.2, 6.1
                         - b: 0.0, -7.8, -12.4, 0.4, -8.1
                        Fourier coefficients for Peak 3 at 20.0 , variance 5.0
                         - a: 97.3, 36.1, 1.4, 2.3, -2.9
                         - b: 0.0, -16.2, -6.1, -6.8, -14.0
                        Weight autoregression: 0.632

                        Weekday factors: 1.005, 1.007, 1.014, 1.078, 0.925, 0.978, 0.992
                    */
                    maxConsumption = 10000.0;
                    cvConsumption = 1.0;
                    weightPrior = 0.632;
                    peakHours = new double[] { 8.0, 14.0, 20.0 };
                    variancePeaks = new double[] { 5.0, 5.0, 5.0 };
                    aList = new List<double[]>()
                    {
                        new double[] { 102.1,  9.8,   3.8,   4.6, 3.9 },       // Baseline
                        new double[] {  86.7, 13.0, -15.3, 10.9,  3.0 },
                        new double[] {  79.8, 23.2,   2.3,  6.2,  6.1 },
                        new double[] {  97.3, 36.1,   1.4,  2.3, -2.9 }
                    };
                    bList = new List<double[]>()
                    {
                        new double[] { 0.0,  -1.7,   0.6, -12.2,   4.7 },       // Baseline
                        new double[] { 0.0,   8.8, -10.5 ,  9.4,  -8.0 },
                        new double[] { 0.0,  -7.8, -12.4,   0.4,  -8.1 },
                        new double[] { 0.0, -16.2,  -6.1,  -6.8, -14.0 }
                    };
                    weekdayFactors = new Dictionary<DayOfWeek, double>()
                                {
                                    { DayOfWeek.Sunday,    1.005 },
                                    { DayOfWeek.Monday,    1.007 },
                                    { DayOfWeek.Tuesday,   1.014 },
                                    { DayOfWeek.Wednesday, 1.078 },
                                    { DayOfWeek.Thursday,  0.925 },
                                    { DayOfWeek.Friday,    0.978 },
                                    { DayOfWeek.Saturday,  0.992 }
                                };
                    break;
                case "SennV":
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
                        Weight autoregression: 0.809

                        Weekday factors: 0.960, 0.976, 1.019, 1.076, 0.989, 1.003, 0.976

                    */
                    maxConsumption = 10000.0;
                    cvConsumption = 1.0;
                    weightPrior = 0.809;
                    peakHours = new double[] { 8.0, 14.0, 20.0 };
                    variancePeaks = new double[] { 5.0, 5.0, 5.0 };
                    aList = new List<double[]>()
                    {
                        new double[] { 518.7,  -9.9,  13.4, -12.3, -17.3 },       // Baseline
                        new double[] { 297.0, -15.9, -63.0,   8.4, -20.2 },
                        new double[] { 500.6,  68.7, -36.8,  42.1,  -7.8 },
                        new double[] { 441.8,  77.1, -53.4,  39.1,  -4.6 }
                    };
                    bList = new List<double[]>()
                    {
                        new double[] { 0.0,  38.2, 31.8, 10.5,   4.3 },       // Baseline
                        new double[] { 0.0,   7.1, 19.9, -2.5,   0.1 },
                        new double[] { 0.0, -10.8, 20.8, 10.7,  15.5 },
                        new double[] { 0.0,  -2.8,  6.8, 19.9, -15.7 }
                    };
                    weekdayFactors = new Dictionary<DayOfWeek, double>()
                                {
                                    { DayOfWeek.Sunday,    0.960 },
                                    { DayOfWeek.Monday,    0.976 },
                                    { DayOfWeek.Tuesday,   1.019 },
                                    { DayOfWeek.Wednesday, 1.076 },
                                    { DayOfWeek.Thursday,  0.989 },
                                    { DayOfWeek.Friday,    1.003 },
                                    { DayOfWeek.Saturday,  0.976 }
                                };
                    break;
                default:
                    throw new ArgumentException($"Site ID '{siteId}' not recognized for consumption simulation.");
            }
        }
    }
}
