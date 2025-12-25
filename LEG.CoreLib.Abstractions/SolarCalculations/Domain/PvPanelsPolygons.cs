using System.ComponentModel.DataAnnotations;
using static LEG.Common.Utils.ShadowCalculator;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain
{    public record PvPanelsPolygons(
        [property: Key] string SystemName,
        List<List<RoofPoint2D>> PanelPolygons
        );
}
