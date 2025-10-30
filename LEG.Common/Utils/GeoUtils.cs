using System;

namespace LEG.Common.Utils
{
    public readonly record struct Dms(int Deg, int Min, double Sec);

    public static class GeoUtils
    {
        private const double RadPerDeg = Math.PI / 180;
        private const double DegPerRad = 180 / Math.PI;

        public const double TwoPi = 2 * Math.PI;

        public static double DegToRad(double deg) => deg * RadPerDeg;
        public static double RadToDeg(double rad) => rad * DegPerRad;

        public static double DegModulo(double deg, bool pm180 = false)
        {
            deg %= 360; // confined to [   0, 360)

            return !pm180 ? deg : deg > 180 ? deg - 360 : deg <= -180 ? deg + 360 : deg;
        }

        public static double DegConversion(Dms dms)
        {
            return dms.Deg + dms.Min / 60.0 + dms.Sec / 3600.0;
        }

        public static Dms InverseDegConversion(double deg)
        {
            var d = (int)deg;
            var m = (int)((deg - d) * 60);
            var s = (deg - d - m / 60.0) * 3600.0;
            return new Dms(d, m, s);
        }

        public static double RoundToSecDecimal(double deg, int secondsDecimals = 1)
        {
            var (d, m, s) = InverseDegConversion(deg);
            s = Math.Round(s, secondsDecimals);
            return DegConversion(new Dms(d, m, s));
        }

        public static string DegToString(double deg)
        {
            var (d, m, s) = InverseDegConversion(deg);
            var si = (int)s;
            var sf = (int)((s - si) * 10);
            return $"{d:D2}°{m:D2}'{si:D2}.{sf:D1}''";
        }

        /// <summary>
        /// Calculates the Haversine distance between two points (lat1, lon1) and (lat2, lon2) in kilometers.
        /// </summary>
        public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Earth's radius in km

            var dLat = DegToRad(lat2 - lat1);
            var dLon = DegToRad(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegToRad(lat1)) * Math.Cos(DegToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
        public static string ToDms(double value)
        {
            var deg = (int)value;
            value = (value - deg) * 60;
            var min = (int)value;
            value = (value - min) * 60;
            var sec = (int)Math.Round(value);
            return $"{deg}°{min}'{sec}\"";
        }

    }
}