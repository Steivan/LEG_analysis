
namespace LEG.PV.Core.Models
{
    public record PvSolarGeometry
    {
        public PvSolarGeometry(double directGeometryFactor, double diffuseGeometryFactor, double sinSunElevation)
        {
            DirectGeometryFactor = directGeometryFactor;
            DiffuseGeometryFactor = diffuseGeometryFactor;
            SinSunElevation = sinSunElevation;
        }
        public double DirectGeometryFactor { get; init; }                                   // G_POA / G_ref [unitless]
        public double DiffuseGeometryFactor { get; init; }                                  // G_POA / G_ref [unitless]
        public double SinSunElevation { get; init; }                                        // G_GHI / G_DNI [unitless]

        public double ConstrainedDirectGeometryFactor => Math.Max(DirectGeometryFactor, 0.0);
        public double ConstrainedDiffuseGeometryFactor => Math.Max(DiffuseGeometryFactor, 0.0);
        public double ConstrainedSinSunElevation => Math.Max(SinSunElevation, 0.0);
        public bool HasDirectIrradiance => DirectGeometryFactor > 0;
        public bool HasDiffuseIrradiance => DiffuseGeometryFactor > 0 && SinSunElevation > 0;
        public bool HasIrradiance => HasDirectIrradiance || HasDiffuseIrradiance;
    }

}
