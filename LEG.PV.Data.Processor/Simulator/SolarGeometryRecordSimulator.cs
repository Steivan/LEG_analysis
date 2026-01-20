using LEG.PV.Core.Models.PvProductionModel;
using static LEG.PV.Data.Processor.Simulator.SimulatorParameters;

namespace LEG.PV.Data.Processor.Simulator
{
    internal class SolarGeometryRecordSimulator
    {
        internal static (double sunAzimut, double sunElevation, double cosOmegaYear, double omegaDay) GetSolarGeometry(
            int startYear, DateTime timeStamp,
            double siteLatitude, double siteLongitude)
        {
            var time0 = new DateTime(startYear, 1, 1, 0, 0, 0);
            var timeLag = (timeStamp - time0).Days;

            var annualSolarAmplitude = earthTilt;
            var diurnalSolarAmplitude = 90.0 - siteLatitude;

            var cosOmegaYear = Math.Cos(omegaYear * timeLag);
            var annualZenithangle = 90 + annualSolarAmplitude * cosOmegaYear;      // zenith angle of the sun is largest in winter

            var timeOfDay = timeStamp.Hour + timeStamp.Minute / 60.0;

            var cosOmegaDay = Math.Cos(omegaDay * timeOfDay);
            var diurnalZenithAngle = (90 - siteLatitude) * cosOmegaDay; // zenith angle of the sun is largest at night

            // Geometry factor combines annual and diurnal variations
            var sunAzimuth = (timeOfDay - 12.0) * 15.0;
            var sunZenithAngle = annualZenithangle + diurnalZenithAngle;
            var sunElevation = 90 - sunZenithAngle;
            var sinSunElevation = Math.Cos(sunZenithAngle * radPerDeg);

            return (
                Math.Round(sunAzimuth, 4),
                Math.Round(sunElevation, 4),
                Math.Round(cosOmegaYear, 4),
                Math.Round(cosOmegaDay, 4));
        }

        internal static PvSolarGeometry GetSimulatedPvSolarGeometry(
            int startYear,
            DateTime timeStamp, 
            double siteLatitude, 
            double siteLongitude, 
            double roofAzimuth, 
            double roofElevation)
        {
            var roofElevationRad = roofElevation * radPerDeg;
            var (roundedSolarGeometry, _, _) = PvGeometryRecordSimulator.GetPvSolarGeometry(
                startYear, 
                timeStamp, 
                siteLatitude, 
                siteLongitude, 
                roofAzimuth, 
                Math.Sin(roofElevationRad), 
                Math.Cos(roofElevationRad)
                );

            return roundedSolarGeometry;
        }
    }
}
