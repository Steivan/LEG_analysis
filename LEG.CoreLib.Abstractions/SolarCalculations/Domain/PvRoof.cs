using System.ComponentModel.DataAnnotations;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record PvRoof(
    [property: Key] string SystemName,
    string EgrId,
    string Inverter,                            // ID of parent inverter
    double Azi,                                 // Orientation of roof in [deg] deviation from S ('+'=W, '-'=E)
    double Elev,                                // Elevation of roof in [deg] (0°=flat, 90°=vertical)
    double Elev2,                               // 2nd elevation -> currently not used
    double Area,                                // Area of roof in [m^2]
    double Peak,                                // Installed power in [kWp]
    PvRoofPanelsPolygons? PanelsPolygons,           // Polygons defining panel areas on roof in 2D roof coordinates
    PvRoofObstacles? HorizontalObstacles  // Horizontal obstacles defined by a line (origin point in 2D roof coordinates) and length [m]
)
{
    public bool HasPanelsAndHorizontalObstacles => 
        (PanelsPolygons != null && PanelsPolygons.PanelPolygons.Count > 0 &&
        HorizontalObstacles != null && HorizontalObstacles.HorizontalObstacles.Count > 0);
};