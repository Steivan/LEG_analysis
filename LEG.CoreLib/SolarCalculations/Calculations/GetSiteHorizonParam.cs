using LEG.Common.Utils;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace LEG.CoreLib.SolarCalculations.Calculations
{
    public class GetSiteHorizonParam
    {
        /// <summary>
        /// Returns the closest site in the dictionary to the given (lat, lon) and the distance in kilometers.
        /// </summary>
        public static (string siteName, double distanceKm) GetClosestSite(double lat, double lon, ISiteCoordinateProvider coordinateProvider, ISiteHorizonControlProvider horizonControlProvider)
        {
            var siteCoordinates = coordinateProvider.GetSiteCoordinates();
            var horizonControls = horizonControlProvider.GetSiteHorizonControls();

            var validSites = siteCoordinates
                .Where(kvp =>
                    horizonControls.TryGetValue(kvp.Key, out var dictEntry) &&
                    dictEntry.getHorizon)
                .Select(kvp => (siteName: kvp.Key, distanceKm: kvp.Value.GetDistanceTo(lat, lon)));

            var (siteName, distanceKm) = validSites.OrderBy(s => s.distanceKm).FirstOrDefault();

            return (siteName ?? string.Empty, distanceKm);
        }


        private static double DegreesToRadians(double deg) =>  GeoUtils.DegToRad(deg);  // deg * Math.PI / 180.0;

        private static double NormalizeAziPerStep(double aziPerStep) =>
            aziPerStep <= 3.5 ? 2.5 :
            aziPerStep <= 7.5 ? 5.0 :
            aziPerStep <= 20 ? 10.0 : 30.0;

        public static (double lat, double lon, double elev, List<double> azim, bool fetchElev) GetSiteParams(
            string site,
            ISiteCoordinateProvider coordinateProvider,
            ISiteHorizonControlProvider horizonControlProvider,
            double lat = 46.0, double lon = 10.0, double elev = 400.0, double nearbySitesLookupDistance = 100)
        {
            var siteCoordinates = coordinateProvider.GetSiteCoordinates();
            var horizonControls = horizonControlProvider.GetSiteHorizonControls();

            var fetchElevations = false;
            double aziPerStep = 30;

            if (!siteCoordinates.TryGetValue(site, out SiteLocation siteParams))
                siteParams = new SiteLocation(lat, lon, elev);

            (lat, lon, elev) = (siteParams.GetLatitude(), siteParams.GetLongitude(), siteParams.GetElevation());

            var (closestSiteName, distanceKm) = GetClosestSite(lat, lon, coordinateProvider, horizonControlProvider);

            if (distanceKm < nearbySitesLookupDistance / 1000 &&
                siteCoordinates.TryGetValue(closestSiteName, out var closestParams))
            {
                siteParams = closestParams;
                (fetchElevations, aziPerStep) = horizonControls[closestSiteName];
            }
            else
            {
                (fetchElevations, aziPerStep) = (false, 30);
            }

            aziPerStep = NormalizeAziPerStep(aziPerStep);
            const int halfRange = 150;
            var aziSteps = (int)(2 * halfRange / aziPerStep) + 1;
            var azimuths = Enumerable.Range(0, aziSteps).Select(i => -halfRange + i * aziPerStep).ToList();

            return (siteParams.GetLatitude(), siteParams.GetLongitude(), siteParams.GetElevation(), azimuths, fetchElevations);
        }
    }
}