namespace LEG.SwissTopo.Abstractions
{
    public class RoofInfo
    {
        public double AreaM2 { get; set; }
        public double OrientationDeg { get; set; }
        public double SlopeDeg { get; set; }
        public string? Suitability { get; set; }
        public string? FeatureId { get; set; }
    }
}
