using static LEG.Common.Utils.RoofGeometry;
using static LEG.Common.Utils.ShadowCalculator;

namespace LEG.Tests
{
    [TestClass]
    public class TestShadowCalculation
    {
        [TestMethod]
        public void TestShadowCalculationMethod()
        {

            var roofAzimuth = 16.5;
            var roofElevation = 35.0;
            var roofElRad = roofElevation * Math.PI / 180.0;
            var cosRoofEl = Math.Cos(roofElRad);

            var lukarnePolygon = GetRoofPoints2DList(new List<(double x, double y)>
            {
                ( 0.00,  0.00),
                (-1.81, -2.15),
                (-1.81, -5.15),
                ( 0.00, -5.15),
                ( 1.81, -5.15),
                ( 1.81, -2.15),
                ( 0.00,  0.00)
            });

            var topLineOrigin = lukarnePolygon[0];
            var westLineOrigin = lukarnePolygon[1];
            var eastLineOrigin = lukarnePolygon[5];
            var topLineLength = -lukarnePolygon[3].Y / cosRoofEl;
            var sideLineLength = (lukarnePolygon[1].Y - lukarnePolygon[2].Y) / cosRoofEl;

            var panelWestPolygon = GetRoofPoints2DList(new List<(double x, double y)>
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
                });

            var panelEastPolygon = GetRoofPoints2DList(new List<(double x, double y)>
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
                });


            for (var deltaSunAzimuth = - 85.0; deltaSunAzimuth <= 85.0; deltaSunAzimuth += 34.0)
            {
                var sunAzimuth = roofAzimuth + deltaSunAzimuth;
                for (var sunElevation = 15.0; sunElevation < 90.0; sunElevation += 20.0)
                {
                    var (totalWestArea, shadowedTopWestArea, shadowTopWestPercentage) = CalculateCompleteShadowAnalysis(
                            panelWestPolygon,
                            roofAzimuth,
                            roofElevation,
                            sunAzimuth,
                            sunElevation,
                            topLineOrigin,
                            topLineLength);
                    var (_, shadowedWestWestArea, shadowWestWestPercentage) = CalculateCompleteShadowAnalysis(
                            panelWestPolygon,
                            roofAzimuth,
                            roofElevation,
                            sunAzimuth,
                            sunElevation,
                            westLineOrigin,
                            sideLineLength);
                    var shadowAreaWest = Math.Max(shadowedTopWestArea, shadowedWestWestArea);

                    var (totalEastArea, shadowedTopEastArea, shadowTopEastPercentage) = CalculateCompleteShadowAnalysis(
                            panelEastPolygon,
                            roofAzimuth,
                            roofElevation,
                            sunAzimuth,
                            sunElevation,
                            topLineOrigin,
                            topLineLength);
                    var (_, shadowedEastEastArea, shadowEastEastPercentage) = CalculateCompleteShadowAnalysis(
                            panelEastPolygon,
                            roofAzimuth,
                            roofElevation,
                            sunAzimuth,
                            sunElevation,
                            eastLineOrigin,
                            sideLineLength);
                    var shadowAreaEast = Math.Max(shadowedTopEastArea, shadowedEastEastArea);

                    var totalArea = totalWestArea + totalEastArea;
                    var shadowedArea = shadowAreaWest + shadowAreaEast;
                    var shadowPercentage = totalArea > 0.0 ? shadowedArea / totalArea * 100.0 : 0.0;
                }
            }
        }
    }
}