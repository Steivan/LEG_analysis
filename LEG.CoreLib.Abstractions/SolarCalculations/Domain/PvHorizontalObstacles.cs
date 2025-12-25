using System.ComponentModel.DataAnnotations;
using static LEG.Common.Utils.ShadowCalculator;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain
{    public record PvHorizontalObstacles(
        [property: Key] string SystemName,
        List<(RoofPoint2D HorizontalLineOrigin, double HorizontalLineLength)> HorizontalObstacles
        );
}
