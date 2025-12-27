using System.ComponentModel.DataAnnotations;
using static LEG.Common.Utils.ShadowCalculator;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain
{    public record PvRoofObstacles(
        [property: Key] string SystemName,
        List<(RoofPoint2D LineOrigin, double LineHorizontalLength, double LineElevation)> HorizontalObstacles
        );
}
