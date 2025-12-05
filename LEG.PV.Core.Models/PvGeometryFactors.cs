
namespace LEG.PV.Core.Models
{
    public record PvGeometryFactors
    {
        public PvGeometryFactors(double directGeometryFactor, double diffuseGeometryFactor, double sinSunElevation)
        {
            DirectGeometryFactor = directGeometryFactor;
            DiffuseGeometryFactor = diffuseGeometryFactor;
            SinSunElevation = sinSunElevation;
        }
        public double DirectGeometryFactor { get; init; }                                   // G_POA / G_ref [unitless]
        public double DiffuseGeometryFactor { get; init; }                                  // G_POA / G_ref [unitless]
        public double SinSunElevation { get; init; }                                        // G_GHI / G_DNI [unitless]
    }

}
