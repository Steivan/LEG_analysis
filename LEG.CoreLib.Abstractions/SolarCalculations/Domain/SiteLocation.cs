using LEG.Common.Utils;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain
{
    public readonly record struct SiteLocation(Dms Lat, Dms Lon, double Elev)
    {
        // Second constructor: takes lat and lon in decimal degrees
        public SiteLocation(double latDeg, double lonDeg, double elev)
            : this(GeoUtils.InverseDegConversion(latDeg), GeoUtils.InverseDegConversion(lonDeg), elev) { }
        public double GetLatitude() => GeoUtils.DegConversion(Lat);
        public double GetLongitude() => GeoUtils.DegConversion(Lon);
        public double GetElevation() => Elev;
        public double GetDistanceTo(double lat2, double lon2) => GeoUtils.HaversineDistance(GetLatitude(), GetLongitude(), lat2, lon2);
    }
}