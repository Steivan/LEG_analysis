namespace LEG.PV.Core.Models
{
    public record PvPowerRecord
    {
        public PvPowerRecord(double power)        // minimal constructor with no differentiation
        {
            PowerG = power;
            PowerGR = power;
            PowerGRT = power;
            PowerGRTW = power;
            PowerGRTWF = power;
            PowerGRTWFS = power;
        }
        public PvPowerRecord(double powerG, double powerGRTWFS)  // Simplified constructor for geometry only (G) and geometry + weather
        {
            PowerG = powerG;
            PowerGR = powerGRTWFS;
            PowerGRT = powerGRTWFS;
            PowerGRTW = powerGRTWFS;
            PowerGRTWF = powerGRTWFS;
            PowerGRTWFS = powerGRTWFS;
        }
        public PvPowerRecord(double pG, double pGR, double pGRT, double pGRTW, double pGRTWF, double pRTGWFS) // Full constructor
        {
            PowerG = pG;
            PowerGR = pGR;
            PowerGRT = pGRT;
            PowerGRTW = pGRTW;
            PowerGRTWF = pGRTWF;
            PowerGRTWFS = pRTGWFS;

        }
        public double PowerG { get; init; }                                                       // [W] Geometry
        public double PowerGR { get; init; }                                                       // [W] G + Radiation
        public double PowerGRT { get; init; }                                                      // [W] GR + Temperature
        public double PowerGRTW { get; init; }                                                     // [W] GRT + Wind
        public double PowerGRTWF { get; init; }                                                    // [W] GRTW + Fog
        public double PowerGRTWFS { get; init; }                                                   // [W] GRTWF + Snow
    }
}
