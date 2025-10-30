namespace LEG.SwissTopo.Abstractions
{
    public interface ICoordinateTransformation
    {
        Task<(double eastingLv95, double northingLv95)?> FromWgs84ToLv95(double wgs84Lon, double wgs84Lat);
    }
}