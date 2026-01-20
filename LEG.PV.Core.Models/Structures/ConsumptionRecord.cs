
namespace LEG.PV.Core.Models.Structures
{
    public record ConsumptionRecord(
        double Solar,
        double Consumers,
        double WallBox,
        double Battery,
        double Grid,
        double Residual
    );
}
