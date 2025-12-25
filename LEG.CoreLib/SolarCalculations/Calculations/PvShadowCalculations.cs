using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using static LEG.Common.Utils.ShadowCalculator;

namespace LEG.CoreLib.SolarCalculations.Calculations
{
    internal class PvShadowCalculations
    {
        public static double GetRoofPanelsArea(PvPanelsPolygons? panelsPolygonsList, double estimatedArea = 0.0)
        {
            if (panelsPolygonsList == null || panelsPolygonsList.PanelPolygons.Count == 0)
                return estimatedArea;

            var roofArea = 0.0;
            foreach (var panelPolygon in panelsPolygonsList.PanelPolygons)
            {
                roofArea += CalculatePolygonArea(panelPolygon);
            }

            return roofArea;
        }

        public static double GetRoofShadowArea(
            PvPanelsPolygons? panelsPolygonsList, 
            PvHorizontalObstacles? horizontalObstaclesList,
            double roofAzimuth, double roofElevation,
            double sunAzimuth, double sunElevation)
        {
            if (panelsPolygonsList == null || panelsPolygonsList.PanelPolygons.Count == 0 ||
                horizontalObstaclesList == null || horizontalObstaclesList.HorizontalObstacles.Count == 0)
                return 0.0;

            var roofShadowArea = 0.0;
            foreach (var panelPolygon in panelsPolygonsList.PanelPolygons)
            {
                var panelsShadowArea = 0.0;
                foreach (var (lineOrigin, lineLength) in horizontalObstaclesList.HorizontalObstacles)
                {
                    var (_, lineShadowArea) = CalculateCompleteShadowAnalysis(
                            panelPolygon,
                            roofAzimuth,
                            roofElevation,
                            sunAzimuth,
                            sunElevation,
                            lineOrigin,
                            lineLength);
                    panelsShadowArea = Math.Max(panelsShadowArea, lineShadowArea);
                }
                roofShadowArea += panelsShadowArea;
            }

            return roofShadowArea;
        }
    }
}
