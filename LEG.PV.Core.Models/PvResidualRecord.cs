
namespace LEG.PV.Core.Models
{
    public record PvResidualRecord
    {
        public bool HasCalculated { get; set; }
        public bool HasMeasured { get; set; }
        public PvPowerRecord ComputedPower { get; set; }
        public PvPowerRecord UnexplainedFractionLossRecord { get; set; }
    }
}
