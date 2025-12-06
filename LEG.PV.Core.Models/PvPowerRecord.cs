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
            PowerGRTWS = power;
            PowerGRTWSF = power;
        }
        public PvPowerRecord(double powerG, double powerGRTWSF)  // Simplified constructor for geometry only (G) and geometry + weather
        {
            PowerG = powerG;
            PowerGR = powerGRTWSF;
            PowerGRT = powerGRTWSF;
            PowerGRTW = powerGRTWSF;
            PowerGRTWS = powerGRTWSF;
            PowerGRTWSF = powerGRTWSF;
        }
        public PvPowerRecord(double pG, double pGR, double pGRT, double pGRTW, double pGRTWS, double pRTGWSF) // Full constructor
        {
            PowerG = pG;
            PowerGR = pGR;
            PowerGRT = pGRT;
            PowerGRTW = pGRTW;
            PowerGRTWS = pGRTWS;
            PowerGRTWSF = pRTGWSF;

        }
        public double PowerG { get; init; }                                                       // [W] Geometry
        public double PowerGR { get; init; }                                                       // [W] G + Radiation
        public double PowerGRT { get; init; }                                                      // [W] GR + Temperature
        public double PowerGRTW { get; init; }                                                     // [W] GRT + Wind
        public double PowerGRTWS { get; init; }                                                    // [W] GRTW + Snow
        public double PowerGRTWSF { get; init; }                                                   // [W] GRTWS + Fog
    }
}
