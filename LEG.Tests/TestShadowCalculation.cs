using static LEG.CoreLib.SampleData.SampleData.ListSites;
using static LEG.Common.Utils.ShadowCalculator;
using static LEG.CoreLib.SampleData.SampleData.DictionaryPvRoofObstacles;

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
            var topLineLength = 4.22; // -lukarnePolygon[3].Y * cosRoofEl;
            var sideLineLength = 2.46; // (lukarnePolygon[1].Y - lukarnePolygon[2].Y) * cosRoofEl;

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

            var roofId = Studenrain + "_1";
            var roofPanelsArea = GetRoofPanelsArea(roofId);

            for (var deltaSunAzimuth = - 85.0; deltaSunAzimuth <= 85.0; deltaSunAzimuth += 34.0)
            {
                var sunAzimuth = roofAzimuth + deltaSunAzimuth;
                for (var sunElevation = 15.0; sunElevation < 90.0; sunElevation += 20.0)
                {
                    var totalShadowArea = GetRoofShadowArea(roofId, sunAzimuth, sunElevation);

                    var (totalWestArea, shadowedTopWestArea) = CalculateCompleteShadowAnalysis(
                            panelWestPolygon,
                            roofAzimuth,
                            roofElevation,
                            sunAzimuth,
                            sunElevation,
                            topLineOrigin,
                            topLineLength,
                            true);
                    var (_, shadowedWestWestArea) = CalculateCompleteShadowAnalysis(
                            panelWestPolygon,
                            roofAzimuth,
                            roofElevation,
                            sunAzimuth,
                            sunElevation,
                            westLineOrigin,
                            sideLineLength);
                    var shadowAreaWest = Math.Max(shadowedTopWestArea, shadowedWestWestArea);

                    var (totalEastArea, shadowedTopEastArea) = CalculateCompleteShadowAnalysis(
                            panelEastPolygon,
                            roofAzimuth,
                            roofElevation,
                            sunAzimuth,
                            sunElevation,
                            topLineOrigin,
                            topLineLength,
                            true);
                    var (_, shadowedEastEastArea) = CalculateCompleteShadowAnalysis(
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

                    Assert.AreEqual(roofPanelsArea, totalArea,0.01);
                    Assert.AreEqual(totalShadowArea, shadowedArea, 0.01);
                }
            }
        }
    }
}