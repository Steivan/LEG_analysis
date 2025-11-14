using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.CoreLib.SolarCalculations.Calculations;
using LEG.HorizonProfiles.Abstractions;

namespace LEG.CoreLib.HorizonProfiles
{
    public class HorizonInitializationService(IHorizonProfileClient horizonClient)
    {
        private readonly IHorizonProfileClient _horizonClient = horizonClient;

        public async Task<(double[] azimuths, double[] angles)> InitializeHorizonAzimuthsAndAngles(
            string siteName,
            ISiteCoordinateProvider coordinateProvider,
            ISiteHorizonControlProvider horizonControlProvider,
            double siteLat = 0,
            double siteLon = 0,
            double siteEl = 0,
            double nearbySitesLookupDistance = 250
        )
        {
            // Step 1: Get site coordinates and support for horizon profile
            var (lat, lon, siteElev, azimuths, fetchElev) = GetSiteHorizonParam.GetSiteParams(siteName,
                coordinateProvider, horizonControlProvider,
                lat: siteLat, lon: siteLon, elev: siteEl, nearbySitesLookupDistance: nearbySitesLookupDistance);

            // Step 2: Get horizon profile if required
            var angles = azimuths.Select(_ => 0.0).ToList(); // Initialize with zeros
            if (fetchElev)
            {
                angles = await _horizonClient.GetHorizonAnglesAsync(
                    lat: lat,
                    lon: lon,
                    siteElev: null, // Auto-query surface elevation
                    roofHeight: 10.0, // Default 10m
                    azimuths: azimuths
                );
            }

            return ([.. azimuths], [.. angles]);
        }
    }
}