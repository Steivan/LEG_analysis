
namespace LEG.HorizonProfiles.Abstractions
{
    public interface IHorizonProfileClient
    {
        Task<List<double>> GetHorizonAnglesAsync(
            double lat,
            double lon,
            double? siteElev,
            double roofHeight = 10.0,
            List<double>? azimuths = null,
            double minDistKm = 0.5,
            double maxDistKm = 50.0,
            int numPoints = 50,
            double diameterKm = 0.01);
    }
}