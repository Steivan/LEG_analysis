using System.Numerics;

namespace LEG.Common.Utils
{
    public static class RoofGeometry
    {
        internal const double GradToRat = Math.PI / 180.0;

        /// <summary>
        /// Computes the direction vector of the intersection line between two planes
        /// </summary>
        /// <param name="n1">Normal vector of the first plane</param>
        /// <param name="n2">Normal vector of the second plane</param>
        /// <param name="p">A common point on the intersection line</param>
        /// <returns>The direction vector of the intersection line</returns>
        public static Vector3 GetIntersectionLineDirection(Vector3 n1, Vector3 n2, Vector3 p)
        {
            // The direction vector is the cross product of the two normal vectors
            Vector3 direction = Vector3.Cross(n1, n2);

            // Check if planes are parallel (cross product is zero or near-zero)
            if (direction.LengthSquared() < 1e-10f)
            {
                throw new ArgumentException("Planes are parallel or coincident - no unique intersection line exists");
            }

            // Optionally normalize the direction vector
            return Vector3.Normalize(direction);
        }

        /// <summary>
        /// Computes both the point and direction of the intersection line
        /// </summary>
        /// <returns>Tuple of (point on line, direction vector)</returns>
        public static (Vector3 Point, Vector3 Direction) GetIntersectionLine(Vector3 n1, Vector3 n2, Vector3 p)
        {
            Vector3 direction = GetIntersectionLineDirection(n1, n2, p);
            return (p, direction);
        }

        /// <summary>
        /// Calculates the shadow of a horizontal line on a tilted roof
        /// </summary>
        /// <param name="roofAzimuth">Roof azimuth in degrees (deviation from S, westward positive)</param>
        /// <param name="roofElevation">Roof elevation in degrees (0=flat, 90=vertical)</param>
        /// <param name="sunAzimuth">Sun azimuth in degrees (deviation from S)</param>
        /// <param name="sunElevation">Sun elevation in degrees (0=horizon, 90=zenith)</param>
        /// <param name="P">Origin point on the roof</param>
        /// <returns>Shadow vector on the roof surface</returns>
        public static (bool SunIsVisible, Vector3 ShadowVector, Vector3 BaseLineVector, double CosRoofAzi, double SinRoofAzi, double CosRoofEl) CalculateRoofShadow(
            double roofAzimuth,
            double roofElevation,
            double sunAzimuth,
            double sunElevation,
            double lineElevation,
            Vector3 P)
        {
            if (lineElevation >= roofElevation)
            {
                throw new InvalidOperationException("Elevation of obstacle line must be less than roof elevation.");
            }

            // Convert degrees to radians and proceed with float calculations
            var roofAzRad = (float)(roofAzimuth * GradToRat);
            var roofElRad = (float)(roofElevation * GradToRat);
            var sunAzRad = (float)(sunAzimuth * GradToRat);
            var sunElRad = (float)(sunElevation * GradToRat);
            var lineElRad = (float)(lineElevation * GradToRat);

            var cosRoofAz = MathF.Cos(roofAzRad);
            var sinRoofAz = MathF.Sin(roofAzRad);
            var cosRoofEl = MathF.Cos(roofElRad);
            var sinRoofEl = MathF.Sin(roofElRad);
            var tanRoofEl = MathF.Tan(roofElRad);

            var cosSunAz = MathF.Cos(sunAzRad);
            var sinSunAz = MathF.Sin(sunAzRad);
            var cosSunEl = MathF.Cos(sunElRad);
            var sinSunEl = MathF.Sin(sunElRad);

            var tanLineEl = MathF.Tan(lineElRad);

            // 1. Horizontal unit vector (pointing in roof azimuth direction), lowered towards roof
            // Azimuth from South: 0° = South, positive = West, negative = East
            // In standard coords: South = -Y, West = -X, East = +X, North = +Y
            Vector3 horizontalLine = new Vector3(
                -sinRoofAz,                 // West component
                -cosRoofAz,                 // South component
                -tanLineEl                  // Horizontal
            );
            // vertical projection of line to roof
            Vector3 baseLineVector = new Vector3(
                horizontalLine.X,           // West component
                horizontalLine.Y,           // South component
                -tanRoofEl                  // Horizontal
            );

            // 2. Sun direction vector (pointing FROM sun TO ground)
            // This is the direction light travels
            Vector3 sunDirection = new Vector3(
                -sinSunAz * cosSunEl,       // West component
                -cosSunAz * cosSunEl,       // South component
                sinSunEl                    // Downward (negative Z)
            );

            // 3. Roof normal vector (pointing upward from roof surface)
            Vector3 roofNormal = new Vector3(
                -sinRoofAz * sinRoofEl,     // West component
                -cosRoofAz * sinRoofEl,     // South component
                cosRoofEl                   // Upward component
            );

            // 4. Project horizontal line onto roof along sun direction
            // Shadow vector = horizontalLine - projection of horizontalLine onto sunDirection
            //                 then project result onto roof plane

            // Check if sun is shining on the roof (not from below)
            var sunDotNormal = Vector3.Dot(sunDirection, roofNormal);
            var sunIsVisible = sunDotNormal > 0;

            // Find where the horizontal line intersects the roof when projected along sun rays
            // We need to project the horizontal line onto the roof plane along the sun direction

            // Shadow direction = horizontalLine - (horizontalLine · roofNormal / sunDirection · roofNormal) * sunDirection
            var horizontalDotNormal = Vector3.Dot(horizontalLine, roofNormal);

            // t = horizontalDotNormal / sunDotNormal;
            var shadowVector = sunIsVisible ? horizontalLine - horizontalDotNormal / sunDotNormal * sunDirection : baseLineVector;

            var checkShadowVector = Vector3.Cross(roofNormal, Vector3.Cross(horizontalLine, sunDirection));
            var isParallel = Vector3.Cross(shadowVector, checkShadowVector).LengthSquared() < 1e-6;
            if (sunIsVisible && !isParallel)
            { 
                throw new InvalidOperationException("Calculated shadow vector is not parallel to expected direction.");
            }

            return (sunIsVisible, shadowVector, baseLineVector, cosRoofAz, sinRoofAz, cosRoofEl);
        }

        ///// <summary>
        ///// Alternative method that returns the shadow as point + direction
        ///// </summary>
        //public static (Vector3 Origin, Vector3 Direction) GetShadowLine(
        //    double roofAzimuth,
        //    double roofElevation,
        //    double sunAzimuth,
        //    double sunElevation,
        //    Vector3 P)
        //{
        //    var (sunIsVisible, shadowVector, baseLineVector, _,_,_) = CalculateRoofShadow(
        //        roofAzimuth, roofElevation,
        //        sunAzimuth, sunElevation, P);

        //    return (P, Vector3.Normalize(shadowVector));
        //}
    }
}
