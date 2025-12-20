using LEG.PV.Core.Models;
using static LEG.PV.Data.Processor.Simulator.SolarGeometryRecordSimulator;

namespace LEG.PV.Data.Processor.Simulator
{
    internal class PvGeometryRecordSimulator
    {
        public static (PvSolarGeometry sunGeometry, double cosOmegaYear, double omegaDay) GetPvSolarGeometry(
            int startYear, DateTime timeStamp,
            double siteLatitude, double siteLongitude,
            double roofAzimuth, double sinRoofElevation, double cosRoofElevation)
        {
            var (sunAzimuth, sunElevation, cosOmegaYear, cosOmegaDay) = GetSolarGeometry(startYear, timeStamp, siteLatitude, siteLongitude);

            var sinSunElevation = Math.Sin(sunElevation * Math.PI / 180.0);

            var directGeometryFactor = Math.Cos(sunElevation * Math.PI / 180.0) * cosRoofElevation * Math.Cos((sunAzimuth - roofAzimuth) * Math.PI / 180.0)  // theta = 90 - elevation => Cos() <-> Sin()
                + Math.Sin(sunElevation * Math.PI / 180.0) * sinRoofElevation;
            var diffuseGeometryFactor = (1.0 + cosRoofElevation) / 2;

            return (
                new PvSolarGeometry(
                    Math.Round(directGeometryFactor, 4),
                    Math.Round(diffuseGeometryFactor, 4),
                    Math.Round(sinSunElevation, 4)
                   ),
                Math.Round(cosOmegaYear, 4),
                Math.Round(cosOmegaDay, 4)
                );
        }
    }
}