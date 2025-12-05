
namespace LEG.PV.Core.Models
{
    public record PvModelParams
    {
        public PvModelParams(double etha, double gamma, double u0, double u1, double lDegr)
        {
            this.Etha = etha;
            this.Gamma = gamma;
            this.U0 = u0;
            this.U1 = u1;
            this.LDegr = lDegr;
        }

        public double Etha { get; init; }
        public double Gamma { get; init; }
        public double U0 { get; init; }
        public double U1 { get; init; }
        public double LDegr { get; init; }
    }
}
   
