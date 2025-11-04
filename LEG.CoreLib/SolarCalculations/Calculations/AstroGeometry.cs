using LEG.Common.Utils;

namespace LEG.CoreLib.SolarCalculations.Calculations
{
    public class AstroGeometry
    {
        //public const double Pi = Math.PI;
        //public const double TwoPi = 2 * Pi;

        public static double DegModulo(double deg, bool pm180 = false) => GeoUtils.DegModulo(deg, pm180: pm180);

        public static (double aziDeg, double eleDeg) GetSolarAziElev(int y, int m, int d, int hh, int mm, int ss, int utcShift, double lon, double lat)
        {
            // Input:
            //   - y, m, d, hh, mm, ss: local time T_local (year, month, day, hour, minute, second)
            //   - utcShift: Time difference to UTC: UTC = T_local + utcShift => utcShift = -1 for CH standard time
            //   - lon, lat: longitude (+ = E) and latitude (+ = N) in °
            // Output:
            //   - aziDeg, elevDeg: azimuth (deviation from S) and elevation of the sun in °

            var dateTimeUtc0 = TimeConversionHelper.DateTimeUtc0(); // Julian time 0 (1.1.2000 12:00:00)
            var dateUtc = TimeConversionHelper.DateUtc(y, m, d, hh, mm, ss, utcShift); // date UTC
            var timeUtc = TimeConversionHelper.TimeUtc(y, m, d, hh, mm, ss, utcShift); // time UTC
            var dateTimeUtc = TimeConversionHelper.DateTimeUtc(y, m, d, hh, mm, ss, utcShift); // DateTime UTC
            var deltaDt0 = dateTimeUtc - dateTimeUtc0; // time span from UTC0 til Dy.Mt.Yr H:M:S    [days]
            var deltaD0 = dateUtc - TimeConversionHelper.DateTimeUtc0(); // time span from UTC0 til Dy.Mt.Yr 00:00:00 [days]

            var meanEclLenSun = 280.46 + 0.9856474 * deltaDt0; // mean ecliptic length of the sun
            var meanEclLenSunDeg = DegModulo(meanEclLenSun);

            var anomaly = 357.528 + 0.9856003 * deltaDt0; // mean anomaly
            var anomalyDeg = DegModulo(anomaly);
            var anomalyRad = GeoUtils.DegToRad(anomalyDeg);

            var deltaEclLenSun = 1.915 * Math.Sin(anomalyRad) + 0.02 * Math.Sin(2 * anomalyRad);
            var eclLenSunDeg = DegModulo(meanEclLenSunDeg + deltaEclLenSun); // ecliptic length of the sun
            var eclLenSunRad = GeoUtils.DegToRad(eclLenSunDeg);
            var cosEclLenSun = Math.Cos(eclLenSunRad);

            var eps = 23.439 - 0.0000004 * deltaDt0; // obliquity of the ecliptic
            var epsDeg = DegModulo(eps);
            var epsRad = GeoUtils.DegToRad(epsDeg);

            var alphaRad = Math.Atan(Math.Cos(epsRad) * Math.Tan(eclLenSunRad)); // right ascension
            var alphaDeg = GeoUtils.RadToDeg(alphaRad);
            alphaDeg += cosEclLenSun < 0 ? 180 : 0;

            var deltaRad = Math.Asin(Math.Sin(epsRad) * Math.Sin(eclLenSunRad)); // declination
            var deltaDeg = GeoUtils.RadToDeg(deltaRad);
            deltaRad = GeoUtils.DegToRad(deltaDeg);

            var timeAstro = deltaD0 / 36525; // sidereal time

            var thetaHg = 6.697376 + 2400.05134 * timeAstro + 1.002738 * timeUtc * 24;
            var thetaHgHr = thetaHg % 24;
            var thetaG = thetaHgHr * 15;
            var theta = thetaG + lon; // hour angle of the vernal equinox

            var tau = theta - alphaDeg; // hour angle of the sun
            var tauDeg = DegModulo(tau);
            var tauRad = GeoUtils.DegToRad(tauDeg);

            var latRad = GeoUtils.DegToRad(lat);

            var aziRad = Math.Atan(Math.Sin(tauRad) /
                                   (Math.Cos(tauRad) * Math.Sin(latRad) -
                                    Math.Tan(deltaRad) * Math.Cos(latRad))); // azimuth (measured from the south)
            var aziDeg = GeoUtils.RadToDeg(aziRad);
            if (Math.Cos(tauRad) * Math.Sin(latRad) < Math.Tan(deltaRad) * Math.Cos(latRad))
            {
                aziDeg += 180;
            }

            var alt1Rad = Math.Asin(Math.Cos(deltaRad) * Math.Cos(tauRad) * Math.Cos(latRad) +
                                    Math.Sin(deltaRad) * Math.Sin(latRad)); // altitude angle
            var alt1Deg = GeoUtils.RadToDeg(alt1Rad);
            var alt2Deg = alt1Deg + 10.3 / (alt1Deg + 5.11);
            var alt2Rad = GeoUtils.DegToRad(alt2Deg);

            var meanRefraction = 1.02 / Math.Tan(alt2Rad); // mean refraction

            var elevDeg = alt1Deg + meanRefraction / 60; // refraction-affected height
            aziDeg = DegModulo(aziDeg, true); // confined to (-180, 180]

            return (aziDeg, elevDeg); // SunAzi_Deg, SunElev_Deg        }
        }

        public static double GetCosAngleToSun(double aziDeg, double elevDeg, double roofAzi, double roofElev, double elev2)
        {
            // Input:
            //   - y, m, d, hh, mm, ss: local time T_local (year, month, day, hour, minute, second)
            //   - utcShift: Time difference to UTC: UTC = T_local + utcShift
            //   - lon, lat: Longitude (+ = E) and latitude (+ = N) in °
            //   - roofAzi, roofElev: roof orientation, i.e., deviation from ss (+ = W) and deviation from flat in ° 
            //   - elev2: elevation of second roof plane (or other object) behind the roof (relevant only if Elev2 > RoofElev).
            // Output:
            //   - cosD: cos of the angle between the sun and the roof (if not in the shadow of the second plane) 

            //var (aziDeg, elevDeg) = GetSolarAziElev(y, m, d, hh, mm, ss, utcShift, lon, lat);

            var aziRad = GeoUtils.DegToRad(aziDeg);
            var elevRad = GeoUtils.DegToRad(elevDeg);

            var roofAziRad = GeoUtils.DegToRad(roofAzi);
            var roofElevComplementDeg = 90 - roofElev;
            var roofElevComplementRad = GeoUtils.DegToRad(roofElevComplementDeg);

            var cosElev = Math.Cos(elevRad);
            var sinElev = Math.Sin(elevRad);
            var cosAzi = Math.Cos(roofAziRad - aziRad);

            var cosD = cosElev * Math.Cos(roofElevComplementRad) * cosAzi + sinElev * Math.Sin(roofElevComplementRad);
            var dRad = Math.Acos(cosD);
            var dDeg = GeoUtils.RadToDeg(dRad);

            var dDeg2 = dDeg;
            if (elev2 > roofElev)
            {
                var elev2ComplementDeg = 90.0 - elev2;
                var elev2ComplementRad = GeoUtils.DegToRad(elev2ComplementDeg);
                var cosD2 = cosElev * Math.Cos(elev2ComplementRad) * cosAzi + sinElev * Math.Sin(elev2ComplementRad);
                var dRad2 = Math.Acos(cosD2);
                dDeg2 = GeoUtils.RadToDeg(dRad2);
            }

            if (!(elevDeg > 0 && dDeg < 90 && dDeg2 < 90)) return 0;

            return cosD; // sun above horizon and in front of panel and panel not in shadow of roof_2
        }

        public static (double[] time, double[] azimuth, double[] elevation) GetSolarPathForDay(int evaluationYear, int month, int day, int utcShift, double lon,
            double lat, int hourStart = 2, int hourEnd = 22, int startMinute = 5, int minutesPerPeriod = 10)
        {
            var periodsPerHour = 60 / minutesPerPeriod;
            if (minutesPerPeriod * periodsPerHour != 60) throw new ArgumentException("minutesPerPeriod * periodsPerHour must be 60");

            var periodsPerDay = (hourEnd - hourStart) * periodsPerHour;
            var time = new double[periodsPerDay];
            var sunAzi = new double[periodsPerDay];
            var sunElev = new double[periodsPerDay];
            for (var hour = hourStart; hour < hourEnd; hour++)
            {
                for (var period = 0; period < periodsPerHour; period++)
                {
                    var timeIndex = (hour - hourStart) * periodsPerHour + period;
                    var minute = startMinute + minutesPerPeriod * period;
                    time[timeIndex] = hour + (double)minute / 60;
                    (sunAzi[timeIndex], sunElev[timeIndex]) = AstroGeometry.GetSolarAziElev(evaluationYear,
                        month, day, hour, minute, 0, utcShift, lon, lat);
                }
            }

            return (time, sunAzi, sunElev);
        }
    }
}