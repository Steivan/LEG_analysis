


namespace LEG.CoreLib.SolarCalculations.Calculations
{
    public class SunRiseSetFromProfile
    {
        private static double LinearInterpolation(double x, double x0, double y0, double x1, double y1)
        {
            if (x1 == x0) return (y0 + y1) / 2; // avoid division by zero
            var slope = (y1 - y0) / (x1 - x0);

            return y0 + slope * (x - x0);
        }

        public static double HorizonElevation(double azi, double[] azimuthHorizon, double[] elevationHorizon)
        {
            var n = azimuthHorizon.Length;
            if (azi < azimuthHorizon[0])
                return elevationHorizon[0];

            if (azi > azimuthHorizon[^1])
                return elevationHorizon[^1];

            for (var i = 1; i < n; i++)
            {
                if (azi < azimuthHorizon[i])
                {
                    return LinearInterpolation(azi, azimuthHorizon[i - 1], elevationHorizon[i - 1], azimuthHorizon[i],
                        elevationHorizon[i]);
                }
            }

            return 0;
        }

        public static (double[] sunRise, double[] sunSet) GetSunRiseAndSetArrays(int evaluationYear,
            List<int> evaluationDays,
            int utcShift, double lon, double lat,
            double[] azimuthHorizon, double[] elevationHorizon)
        {
            const int hourStart = 2;
            const int hourEnd = 22;
            const int minutesPerPeriod = 10; // time resolution for solar path calculation: integer fraction of 60
            const int startMinute = minutesPerPeriod / 2;

            var daysPerMonth = evaluationDays.Count;
            var length = 12 * daysPerMonth;
            var sunRise = new double[length];
            var sunSet = new double[length];

            for (var month = 1; month <= 12; month++)
            {
                for (var dayIndex = 0; dayIndex < daysPerMonth; dayIndex++)
                {
                    var arrayIndex = (month - 1) * daysPerMonth + dayIndex;
                    var (sunRisePt, sunSetPt, _, _, _) = GetSunRiseAndSet(azimuthHorizon, elevationHorizon,
                        evaluationYear, month, evaluationDays[dayIndex], utcShift, lon, lat, 
                        hourStart: hourStart, hourEnd: hourEnd, startMinute: startMinute, minutesPerPeriod: minutesPerPeriod);
                    sunRise[arrayIndex] = sunRisePt.t;
                    sunSet[arrayIndex] = sunSetPt.t;
                }
            }

            return (sunRise, sunSet);
        }

        public static ((double t, double a, double e) sunRise, (double t, double a, double e) sunSet, double[] time, double[] azimuth, double[] elevation) GetSunRiseAndSet(double[] azimuthHorizon, double[] elevationHorizon, int evaluationYear, int evaluationMonth, int evaluationDay,
            int utcShift, double lon, double lat, int hourStart = 2, int hourEnd = 22, int startMinute = 5, int minutesPerPeriod = 10)
        {

            var (time, azimuthSun, elevationSun) = AstroGeometry.GetSolarPathForDay(evaluationYear, evaluationMonth, evaluationDay, utcShift,
                lon, lat, hourStart: hourStart, hourEnd: hourEnd, startMinute: startMinute, minutesPerPeriod: minutesPerPeriod);
            var (sunRise, sunSet) = GetSunRisSet(time, azimuthSun, elevationSun);

            return (sunRise, sunSet, time, azimuthSun, elevationSun);
            
            double GetHorizonElevation(double azi) => HorizonElevation(azi, azimuthHorizon, elevationHorizon);

            ((double t, double a, double e) sunRise, (double t, double a, double e) sunSet) GetSunRisSet(double[] time, double[] sunAzi, double[] sunElev)
            {
                var n = time.Length;

                var loIndex = 0;
                var loSunAzi = sunAzi[loIndex];
                var loSunElev = sunElev[loIndex];
                var loHorizonElev = GetHorizonElevation(sunAzi[loIndex]);
                var hiIndex = 1;
                var hiSunAzi = sunAzi[hiIndex];
                var hiSunElev = sunElev[hiIndex];
                var hiHorizonElev = GetHorizonElevation(sunAzi[hiIndex]);
                while (hiSunElev < hiHorizonElev && hiIndex < n - 1)
                {
                    loIndex = hiIndex;
                    loSunAzi = hiSunAzi;
                    loSunElev = hiSunElev;
                    loHorizonElev = hiHorizonElev;
                    hiIndex ++;
                    hiSunAzi = sunAzi[hiIndex];
                    hiSunElev = sunElev[hiIndex];
                    hiHorizonElev = GetHorizonElevation(sunAzi[hiIndex]);
                }
                var timeSunRise = hiSunElev < hiHorizonElev ? hourEnd : LinearInterpolation(0, loSunElev - loHorizonElev, time[loIndex], hiSunElev - hiHorizonElev, time[hiIndex]);
                var aziSunRise = LinearInterpolation(timeSunRise, time[loIndex], loSunAzi, time[hiIndex], hiSunAzi);
                var elevSunRise = LinearInterpolation(timeSunRise, time[loIndex], loSunElev, time[hiIndex], hiSunElev);

                hiIndex = n - 1; 
                hiSunAzi = sunAzi[hiIndex];
                hiSunElev = sunElev[hiIndex];
                hiHorizonElev = GetHorizonElevation(sunAzi[hiIndex]);
                loIndex = n - 2;
                loSunAzi = sunAzi[loIndex];
                loSunElev = sunElev[loIndex];
                loHorizonElev = GetHorizonElevation(sunAzi[loIndex]);
                while (loSunElev < loHorizonElev && loIndex > 0)
                {
                    hiIndex = loIndex;
                    hiSunAzi = loSunAzi;
                    hiSunElev = loSunElev;
                    hiHorizonElev = loHorizonElev;
                    loIndex --;
                    loSunAzi = sunAzi[loIndex];
                    loSunElev = sunElev[loIndex];
                    loHorizonElev = GetHorizonElevation(sunAzi[loIndex]);

                }
                var timeSunSet = loSunElev < loHorizonElev ? hourStart : LinearInterpolation(0, loSunElev - loHorizonElev, time[loIndex], hiSunElev - hiHorizonElev, time[hiIndex]);
                var aziSunSet = LinearInterpolation(timeSunSet, time[loIndex], loSunAzi, time[hiIndex], hiSunAzi);
                var elevSunSet = LinearInterpolation(timeSunSet, time[loIndex], loSunElev, time[hiIndex], hiSunElev);

                return ((timeSunRise, aziSunRise, elevSunRise), (timeSunSet, aziSunSet, elevSunSet));
            }
        }
    }
}
