using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Numerics;
using static LEG.Common.Utils.RoofGeometry;

namespace LEG.Common.Utils
{
    public class ShadowCalculator
    {
        /// <summary>
        /// Represents a 2D point in the roof plane coordinate system
        /// </summary>
        public struct RoofPoint2D
        {
            public double X { get; set; }  // Parallel to horizontal lines (roof azimuth direction)
            public double Y { get; set; }  // Perpendicular to X, along roof surface

            public RoofPoint2D(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public static List<RoofPoint2D> GetRoofPoints2DList(List<(double x, double y)> points)
        {
            List<RoofPoint2D> roofPoints = new List<RoofPoint2D>();
            foreach (var (x, y) in points)
            {
                roofPoints.Add(new RoofPoint2D(x, y));
            }
            return roofPoints;
        }

        public static RoofPoint2D ConvertToRoofPoint2D(
            Vector3 vector3D,
            RoofPoint2D horizontalLineOrigin,
            double horizontalLineLength,
            double cosRoofAzi,
            double sinRoofAzi,
            double cosRoofEl)
        {
            // Scale the unit vector to the horizontal line length
            var x3D = vector3D.X * horizontalLineLength;
            var y3D = vector3D.Y * horizontalLineLength;

            // Convert 3D coordinates of a vector in the roof plane to 2D roof coordinates using roof azimuth and elevation
            // shift the vector to start from horizontalLineOrigin (given in 2D roof coordinates)
            double x = horizontalLineOrigin.X + x3D * cosRoofAzi - y3D * sinRoofAzi;
            double y = horizontalLineOrigin.Y + (x3D * sinRoofAzi + y3D * cosRoofAzi) / cosRoofEl;

            return new RoofPoint2D(x, y);
        }

        /// <summary>
        /// Calculates the area of a 2D polygon using the Shoelace formula
        /// </summary>
        /// <param name="polygon">Vertices in order (clockwise or counter-clockwise)</param>
        /// <returns>Absolute area of the polygon</returns>
        public static double CalculatePolygonArea(List<RoofPoint2D> polygon)
        {
            if (polygon.Count < 3)
            {
                throw new ArgumentException("Polygon must have at least 3 vertices");
            }

            double area = 0;
            int n = polygon.Count;

            // Shoelace formula: A = 0.5 * |Σ(x_i * y_(i+1) - x_(i+1) * y_i)|
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += polygon[i].X * polygon[j].Y;
                area -= polygon[j].X * polygon[i].Y;
            }

            return Math.Abs(area) / 2f;
        }

        public static (double TotalArea, double ShadowedArea) CalculateShadowedArea(
            List<RoofPoint2D> panelPolygon,
            bool sunIsVisible,
            Vector3 shadowVector3D,
            Vector3 baseLineVector3D,
            RoofPoint2D horizontalLineOrigin,
            double horizontalLineLength,
            double cosRoofAzi,
            double sinRoofAzi,
            double cosRoofEl,
            bool computeTotalArea = false)
        {
            bool IsTriangleLeftHanded(RoofPoint2D a, RoofPoint2D b, RoofPoint2D c)
            {
                double cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                return cross < 0;
            }

            // Calculate areas
            var totalArea = computeTotalArea ? CalculatePolygonArea(panelPolygon) : 0.0;

            if (!sunIsVisible)
            {
                // Entire panel is in shadow
                totalArea = computeTotalArea ? totalArea : CalculatePolygonArea(panelPolygon);
                return (totalArea, totalArea);
            }
            // Create shadow polygon (triangle)

            var shadowPoint = ConvertToRoofPoint2D(shadowVector3D, horizontalLineOrigin, horizontalLineLength, cosRoofAzi, sinRoofAzi, cosRoofEl);
            var basePoint = ConvertToRoofPoint2D(baseLineVector3D, horizontalLineOrigin, horizontalLineLength, cosRoofAzi, sinRoofAzi, cosRoofEl);
            var isLeftHanded = IsTriangleLeftHanded(horizontalLineOrigin, basePoint, shadowPoint);
            var shadowPolygon = isLeftHanded ? new List<RoofPoint2D> 
                {
                    horizontalLineOrigin,
                    shadowPoint,
                    basePoint
                } : new List<RoofPoint2D>
                {
                    horizontalLineOrigin,
                    basePoint,
                    shadowPoint
                };

            var intersectionPolygon = PolygonIntersection(panelPolygon, shadowPolygon);

            double shadowedArea = intersectionPolygon.Count >= 3 ? CalculatePolygonArea(intersectionPolygon) : 0;

            return (totalArea, shadowedArea);
        }


        /// <summary>
        /// Calculates the shadowed area of a PV panel polygon
        /// </summary>
        /// <param name="panelPolygon">PV panel vertices in roof plane coordinates</param>
        /// <param name="shadowVector3D">Shadow vector from previous calculation</param>
        /// <param name=" baseLineVector3D">Shadow vector from previous calculation</param>
        /// <param name="shadowWidth">Width of the shadow-casting object (perpendicular to shadow direction)</param>
        /// <returns>Tuple of (total panel area, shadowed area)</returns>
        public static (double TotalArea, double ShadowedArea) CalculateShadowedArea_0(
            List<RoofPoint2D> panelPolygon,
            Vector3 shadowVector3D,
            Vector3 baseLineVector3D,
            double shadowWidth)
        {
            // Calculate total panel area
            double totalArea = CalculatePolygonArea(panelPolygon);

            // Project shadow vector onto roof plane (2D)
            // The shadow vector is already in 3D space, we need its 2D projection
            // Assuming the roof plane has X parallel to horizontal line direction
            // and Y perpendicular along the roof
            RoofPoint2D shadowDirection2D = new RoofPoint2D(shadowVector3D.X, shadowVector3D.Y);
            double shadowLength = Math.Sqrt(shadowDirection2D.X * shadowDirection2D.X +
                                            shadowDirection2D.Y * shadowDirection2D.Y);

            if (shadowLength < 1e-6f)
            {
                // No shadow or shadow perpendicular to roof
                return (totalArea, 0f);
            }

            // Normalize shadow direction
            shadowDirection2D.X /= shadowLength;
            shadowDirection2D.Y /= shadowLength;

            // Create shadow polygon (rectangle)
            // Starting from origin P (0,0), extending along shadow direction
            // Shadow is cast by a horizontal line, creating a parallelogram on the tilted roof

            // Perpendicular direction to shadow (for width)
            RoofPoint2D perpDirection = new RoofPoint2D(-shadowDirection2D.Y, shadowDirection2D.X);

            // Shadow rectangle vertices
            List<RoofPoint2D> shadowPolygon = new List<RoofPoint2D>
        {
            new RoofPoint2D(0, 0),  // Origin P
            new RoofPoint2D(
                shadowDirection2D.X * shadowLength,
                shadowDirection2D.Y * shadowLength),
            new RoofPoint2D(
                shadowDirection2D.X * shadowLength + perpDirection.X * shadowWidth,
                shadowDirection2D.Y * shadowLength + perpDirection.Y * shadowWidth),
            new RoofPoint2D(
                perpDirection.X * shadowWidth,
                perpDirection.Y * shadowWidth)
        };

            // Calculate intersection of shadow polygon with panel polygon
            List<RoofPoint2D> intersection = PolygonIntersection(panelPolygon, shadowPolygon);

            double shadowedArea = intersection.Count >= 3 ? CalculatePolygonArea(intersection) : 0f;

            return (totalArea, shadowedArea);
        }

        /// <summary>
        /// Calculates the intersection of two polygons using Sutherland-Hodgman algorithm
        /// </summary>
        private static List<RoofPoint2D> PolygonIntersection(List<RoofPoint2D> subject, List<RoofPoint2D> clip)
        {
            List<RoofPoint2D> output = new List<RoofPoint2D>(subject);

            // For each edge of the clipping polygon
            for (int i = 0; i < clip.Count; i++)
            {
                if (output.Count == 0) break;

                List<RoofPoint2D> input = new List<RoofPoint2D>(output);
                output.Clear();

                RoofPoint2D A = clip[i];
                RoofPoint2D B = clip[(i + 1) % clip.Count];

                for (int j = 0; j < input.Count; j++)
                {
                    RoofPoint2D P1 = input[j];
                    RoofPoint2D P2 = input[(j + 1) % input.Count];

                    bool p1Inside = IsPointLeftOfLine(P1, A, B);
                    bool p2Inside = IsPointLeftOfLine(P2, A, B);

                    if (p2Inside)
                    {
                        if (!p1Inside)
                        {
                            // Entering: add intersection point
                            RoofPoint2D? intersection = LineIntersection(P1, P2, A, B);
                            if (intersection.HasValue)
                                output.Add(intersection.Value);
                        }
                        output.Add(P2);
                    }
                    else if (p1Inside)
                    {
                        // Leaving: add intersection point
                        RoofPoint2D? intersection = LineIntersection(P1, P2, A, B);
                        if (intersection.HasValue)
                            output.Add(intersection.Value);
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Checks if point P is on the left side of line AB
        /// </summary>
        private static bool IsPointLeftOfLine(RoofPoint2D P, RoofPoint2D A, RoofPoint2D B)
        {
            return ((B.X - A.X) * (P.Y - A.Y) - (B.Y - A.Y) * (P.X - A.X)) >= 0;
        }

        /// <summary>
        /// Finds intersection point of two line segments
        /// </summary>
        private static RoofPoint2D? LineIntersection(RoofPoint2D P1, RoofPoint2D P2, RoofPoint2D P3, RoofPoint2D P4)
        {
            double denom = (P1.X - P2.X) * (P3.Y - P4.Y) - (P1.Y - P2.Y) * (P3.X - P4.X);

            if (Math.Abs(denom) < 1e-10f)
                return null; // Parallel lines

            double t = ((P1.X - P3.X) * (P3.Y - P4.Y) - (P1.Y - P3.Y) * (P3.X - P4.X)) / denom;

            return new RoofPoint2D(
                P1.X + t * (P2.X - P1.X),
                P1.Y + t * (P2.Y - P1.Y)
            );
        }

        /// <summary>
        /// Complete calculation combining shadow vector and area calculations
        /// </summary>
        public static (double TotalArea, double ShadowedArea)
            CalculateCompleteShadowAnalysis(
                List<RoofPoint2D> panelPolygon,
                double roofAzimuth,
                double roofElevation,
                double sunAzimuth,
                double sunElevation,
                RoofPoint2D horizontalLineOrigin,
                double horizontalLineLength,
                double lineElevation,
                bool computeTotalArea = false)
        {
            // Get shadow vector from previous calculation
            var (sunIsVisible, shadowVector, baseLineVector, cosRoofAzi, sinRoofAzi, cosRoofEl) = CalculateRoofShadow(
                roofAzimuth, roofElevation,
                sunAzimuth, sunElevation,
                lineElevation,
                Vector3.Zero);

            var (totalArea, shadowedArea) = CalculateShadowedArea(
                panelPolygon,
                sunIsVisible,
                shadowVector,
                baseLineVector,
                horizontalLineOrigin,
                horizontalLineLength,
                cosRoofAzi,
                sinRoofAzi,
                cosRoofEl,
                computeTotalArea: computeTotalArea);

            double shadowPercentage = totalArea > 0 ? (shadowedArea / totalArea) * 100f : 0f;

            return (totalArea, shadowedArea);
        }
    }
}
