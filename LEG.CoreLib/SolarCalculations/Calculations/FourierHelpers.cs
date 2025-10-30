using LEG.Common.Utils;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.CoreLib.SolarCalculations.Domain;
using LEG.CoreLib.SolarCalculations.Utilities;
using static LEG.CoreLib.Abstractions.ReferenceData.SiteStatus;

namespace LEG.CoreLib.SolarCalculations.Calculations
{
    internal class FourierHelpers
    {
        public static (double[] timeSupport, double[] functionValues) GetIntervalFourier(
            double[] aCoefficients, double[] bCoefficients, int nFourier, double p1, double p2)
        {
            var period = p2 - p1 + 1;
            var intPeriod = (int)period;
            var dp = (p2 - p1) / (intPeriod - 1);
            var timeSupport = Enumerable.Range(0, intPeriod).Select(i => p1 + dp * i).ToArray();
            var omega = GeoUtils.TwoPi / period;
            var omegaT = timeSupport.Select(tj => tj * omega).ToArray();

            var functionValues = Enumerable.Repeat(aCoefficients[0], intPeriod).ToArray();
            for (var i = 1; i <= nFourier; i++)
            {
                functionValues = [..functionValues.Zip(omegaT,
                    (yj, oj) => yj + aCoefficients[i] * Math.Cos(oj * i) + bCoefficients[i] * Math.Sin(oj * i))];
            }

            return (timeSupport, functionValues);
        }

        public static (double[] aCoefficients, double[] bCoefficients, double[] rCoefficients, double[] phiCoefficients)
            GetFourierCoefficients(double[] yEmpirical, double[] timeSupport, double period, int nFourier)
        {
            var aCoefficients = new double[nFourier + 1];
            var bCoefficients = new double[nFourier + 1];
            var rCoefficients = new double[nFourier + 1];
            var phiCoefficients = new double[nFourier + 1];

            var length = yEmpirical.Length;
            var omega = GeoUtils.TwoPi / period;
            var omegaT = timeSupport.Select(tj => tj * omega).ToArray();

            aCoefficients[0] = yEmpirical.Sum() / length;
            for (var i = 1; i <= nFourier; i++)
            {
                aCoefficients[i] = yEmpirical.Zip(omegaT, (yj, oj) => yj * Math.Cos(oj * i)).Sum() * 2 / length;
                bCoefficients[i] = yEmpirical.Zip(omegaT, (yj, oj) => yj * Math.Sin(oj * i)).Sum() * 2 / length;
                rCoefficients[i] = Math.Sqrt(aCoefficients[i] * aCoefficients[i] + bCoefficients[i] * bCoefficients[i]);
                phiCoefficients[i] = Math.Atan2(bCoefficients[i], aCoefficients[i]);
            }

            return (aCoefficients, bCoefficients, rCoefficients, phiCoefficients);
        }

        public static (double[] timeSupport, double[] factorEmpirical, double[] factorModel) GetMeteoFourier(
            double[] factorMeteoPerMonth, double[] daysPerMonth, int nFourier)
        {
            const double tFirstDay = 0.5;
            var daysYear = daysPerMonth.Sum();
            var tLastDay = daysYear + 0.5;
            var maxDays = (int)daysYear + 1;
            var deltaT = (tLastDay - tFirstDay) / (maxDays - 1);
            var t0 = Enumerable.Range(0, maxDays).Select(i => tFirstDay + deltaT * i).ToArray();
            var f0 = new double[maxDays];

            var periods = factorMeteoPerMonth.Length;
            var ti = 0;
            for (var month = 0; month < periods; month++)
            {
                var days = daysPerMonth[month];
                var fMeteo = factorMeteoPerMonth[month];
                for (var day = 0; day < days; day++)
                {
                    f0[ti] = fMeteo;
                    ti += 1;
                }
            }

            f0[^1] = f0[0];

            var (a, b, r, phi) = GetFourierCoefficients(f0, t0, maxDays, nFourier);

            (t0, var f1) = GetIntervalFourier(a, b, nFourier, tFirstDay, tLastDay);

            var time0 = tFirstDay - 1.0;
            var timeStep = (tLastDay - time0) / maxDays;
            var timeSupport = Enumerable.Range(0, maxDays + 1).Select(i => time0 + timeStep * i).ToArray();
            timeSupport[0] = 0.0;

            var factorEmpirical = new double[maxDays + 1];
            var factorModel = new double[maxDays + 1];
            Array.Copy(f0, 0, factorEmpirical, 1, f0.Length);
            Array.Copy(f1, 0, factorModel, 1, f1.Length);

            return (timeSupport, factorEmpirical, factorModel);
        }

        public static (double[] timeSupport, double[] factorEmpirical, double[] factorModel) GetFourierMeteo(
            MeteoProfile meteoProfile)
        {
            var daysPerMonthDouble = Array.ConvertAll(BasicParametersAndConstants.DaysPerMonth, item => (double)item);
            return GetMeteoFourier(meteoProfile.Profile, daysPerMonthDouble, meteoProfile.NFourier);
        }
    }
}