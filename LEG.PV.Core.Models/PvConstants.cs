namespace LEG.PV.Core.Models
{
    public class PvConstants
    {
        internal const double solarConstant = 1361.0;                                   // [W/m²]
        internal const double directRatio = 0.73;
        internal const double diffuseRatio = 1.0 - directRatio;
        internal const double baselineIrradiance = 1000;                                // [W/m^2]
        internal const double solarConstantRatio = solarConstant / baselineIrradiance;
        internal const double mpSPerKmh = 1000 / 3600;                                  // 1 km/h = 1000 m / 3600 s  ]
        internal const double meanTempStc = 25;                                         // [°C]
        public static double ConvertKmhToMpS(double vKmh)
        {
            return vKmh * mpSPerKmh;
        }

        public static double ConvertMpSToKmh(double vmpS)
        {
            return vmpS / mpSPerKmh;
        }
    }
}
