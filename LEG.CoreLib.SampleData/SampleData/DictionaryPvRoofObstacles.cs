using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using static LEG.Common.Utils.ShadowCalculator;
using static LEG.CoreLib.SampleData.SampleData.DictionaryPvRoofData;
using static LEG.CoreLib.SampleData.SampleData.ListSites;

namespace LEG.CoreLib.SampleData.SampleData
{
    public static class DictionaryPvRoofObstacles
    {
        internal static readonly Dictionary<string, PvRoofPanelsPolygons> PvRoofPanelPolygons =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Senn + "_1"] = new PvRoofPanelsPolygons(
                    SystemName: Senn + "_1",
                    PanelPolygons: new List<List<RoofPoint2D>>
                    {
                        GetRoofPoints2DList(new List<(double x, double y)>
                        {
                            (-5.83,  2.34),
                            (-5.83, -5.52),
                            (-0.73, -5.52),
                            (-0.73,  2.34)
                        }),
                        GetRoofPoints2DList(new List<(double x, double y)>
                        {
                            ( 5.48,  2.34),
                            ( 5.48, -5.52),
                            (10.58, -5.52),
                            (10.58,  2.34)
                        })
                    }),
                [Studenrain + "_1"] = new PvRoofPanelsPolygons(
                    SystemName: Studenrain + "_1",
                    PanelPolygons: new List<List<RoofPoint2D>>
                    {
                        GetRoofPoints2DList(new List<(double x, double y)>
                        {
                            (-1.84, -5.11),
                            (-5.12, -5.11),
                            (-5.12, 2.50),
                            (-1.84, 2.50),
                            (-1.84, 0.98),
                            (-0.20, 0.98),
                            (-0.20, -0.54),
                            (-1.84, -0.54),
                            (-1.84, -5.11)
                        }),
                        GetRoofPoints2DList(new List<(double x, double y)>
                        {
                            (1.84, -5.11),
                            (5.12, -5.11),
                            (5.12, 2.50),
                            (1.84, 2.50),
                            (1.84, 0.98),
                            (0.20, 0.98),
                            (0.20, -0.54),
                            (1.84, -0.54),
                            (1.84, -5.11)
                        })
                    })
            };

            internal static readonly Dictionary<string, PvRoofObstacles> PvRoofHorizontalObstacles =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Senn + "_1"] = new PvRoofObstacles(
                    SystemName: Senn + "_1",
                    HorizontalObstacles: new List<(RoofPoint2D, double, double)>
                    {
                        (new RoofPoint2D(0.00, 0.00), 2.36, 10.00),     // Western end of Lukarne: slope of 10° is a guess
                        (new RoofPoint2D(4.62, 0.00), 2.36, 10.00)      // Eastern end of Lukarne: slope of 10° is a guess
                    }),
                [Studenrain + "_1"] = new PvRoofObstacles(
                    SystemName: Studenrain + "_1",
                    HorizontalObstacles: new List<(RoofPoint2D, double, double)> 
                    {
                        (new RoofPoint2D( 0.00,  0.00), 4.22, 0.0),      // Top line: length = -(-5.15) * cos(35°) = 4.22
                        (new RoofPoint2D(-1.81, -2.15), 2.46, 0.0),      // West side line: length = ( -2.15 - -5.15) * cos(35°) = 2.46
                        (new RoofPoint2D( 1.81, -2.15), 2.46, 0.0)       // East side line: length = ( -2.15 - -5.15) * cos(35°) = 2.46
                    })
            };

        // ************************************************************************************************************


        public static bool HasRoofPanels(string roofId) => PvRoofPanelPolygons.ContainsKey(roofId);
        public static bool HasHorizontalObstacles(string roofId) => PvRoofHorizontalObstacles.ContainsKey(roofId);
        public static bool HasPanelsAndHorizontalObstacles(string roofId) => HasRoofPanels(roofId) && HasHorizontalObstacles(roofId);

        public static double GetRoofPanelsArea(string roofId)
        {
            var roofArea = PvRoofDataDict[roofId].Area;

            if (HasRoofPanels(roofId))
            {
                roofArea = 0;
                foreach (var panelPolygon in PvRoofPanelPolygons[roofId].PanelPolygons)
                {
                    roofArea += CalculatePolygonArea(panelPolygon);
                }
            }
            return roofArea;
        }

        public static double GetRoofShadowArea(string roofId, double sunAzimuth, double sunElevation)
        {
            var shadowArea = 0.0;

            if (HasPanelsAndHorizontalObstacles(roofId))
            {
                var roofAzimuth = PvRoofDataDict[roofId].Azi;
                var roofElevation = PvRoofDataDict[roofId].Elev;
                shadowArea = 0;
                foreach (var panelPolygon in PvRoofPanelPolygons[roofId].PanelPolygons)
                {
                    var panelShadowArea = 0.0;
                    foreach (var (lineOrigin, lineLength, lineElevation) in PvRoofHorizontalObstacles[roofId].HorizontalObstacles)
                    {
                        var (_, lineShadowArea) = CalculateCompleteShadowAnalysis(
                                panelPolygon,
                                roofAzimuth,
                                roofElevation,
                                sunAzimuth,
                                sunElevation,
                                lineOrigin,
                                lineLength,
                                lineElevation);
                        panelShadowArea = Math.Max(panelShadowArea, lineShadowArea);
                    }
                    shadowArea += panelShadowArea;
                }
            }

            return shadowArea;
        }
    }
}
