using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEG.PV.Core.Models
{
    public class PvParameters
    {

        internal const double mpSPerKmh = 1000 / 3600;       // 1 km/h = 1000 m / 3600 s  ]
        internal const double baselineIrradiance = 1000;    // [W/m^2]
        internal const double meanTempStc = 25;              // [°C]
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
