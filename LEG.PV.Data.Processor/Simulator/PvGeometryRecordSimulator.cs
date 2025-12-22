using LEG.PV.Core.Models;
using static LEG.PV.Data.Processor.Simulator.SolarGeometryRecordSimulator;
using static LEG.PV.Data.Processor.Simulator.SimulatorParameters;

namespace LEG.PV.Data.Processor.Simulator
{
    internal class PvGeometryRecordSimulator
    {
        internal static (PvSolarGeometry sunGeometry, double cosOmegaYear, double omegaDay) GetPvSolarGeometry(
            int startYear, DateTime timeStamp,
            double siteLatitude, double siteLongitude,
            double roofAzimuth, double sinRoofElevation, double cosRoofElevation)
        {
            var (sunAzimuth, sunElevation, cosOmegaYear, cosOmegaDay) = GetSolarGeometry(startYear, timeStamp, siteLatitude, siteLongitude);

            var sinSunElevation = Math.Sin(sunElevation * radPerDeg);

            var directGeometryFactor = Math.Cos(sunElevation * radPerDeg) * cosRoofElevation * Math.Cos((sunAzimuth - roofAzimuth) * radPerDeg)  // theta = 90 - elevation => Cos() <-> Sin()
                + Math.Sin(sunElevation * radPerDeg) * sinRoofElevation;
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