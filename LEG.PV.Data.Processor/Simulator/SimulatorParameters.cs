
namespace LEG.PV.Data.Processor.Simulator
{
    internal class SimulatorParameters
    {
        // Solar geometry constants
        internal const double earthTilt = 23.4; // [degrees]
        internal const double daysPerYears = 365.2422;
        internal const int hoursPerDay = 24;
        internal const int minutesPerHour = 60;
        internal const double minutesPerYear = minutesPerHour * hoursPerDay * daysPerYears;
        internal const double omegaYear = 2 * Math.PI / daysPerYears;
        internal const double omegaDay = 2 * Math.PI / hoursPerDay;

        // Mathematical constants
        internal const double radPerDeg = Math.PI / 180.0;
        internal const double degPerRad = 180.0 / Math.PI;

        // Physical constants
        internal const double maxIrradiance = 1361;                                              // [W/m^2] Solar constant
        internal const double KelvinZeroC = 273.15;                                              // [K]
        internal const double StefanBoltzmannConstant = 5.670E-8;                                // [Nm/sm^2K^4]
        internal const double specificHeatAir = 1005;                                            // [Nm/kgK]
        internal const double airDensity = 1.225;                                                // [kg/m^3]
        internal const double airPressure = 101325;                                              // [N/m^2]
        internal const double EarthtGravity = 9.81;                                              // [m/s^2]

        // Model parameters
        internal const double diffuseRadiationRatio = 0.3;
        internal const double averagediffuseRadiation = maxIrradiance * diffuseRadiationRatio;
        internal const double maxDirectIrratiance = maxIrradiance - averagediffuseRadiation;
        internal const double weightPreviousIrradiance = 0.7;
        internal const double directRadiationCV = 0.1;
        internal const double minAlbedo = 0.2;
        internal const double maxAlbedo = 0.8;

        internal const double averageTemp = 5;                                                   // [°C]
        internal const double annualTempAmplitude = 10;                                          // [°C]
        internal const double diurnalTempAmplitude = 5;                                          // [°C]

        internal const double heightOfSurfaceLayer = 200.0;                                      // [m]
        internal const double greenHouseShift = 10.0;                                            // [K]
        internal const double diffusionTimeConstant = 3600;                                      // [s]
        internal const double airMassPerArea = airPressure / EarthtGravity;                      // [[N/m^2 / (m/s^2)] = [kg/m^2] 
        internal const double airMassSurfaceLayerPerArea = airDensity * heightOfSurfaceLayer;    // [kg/m^3 * m] = [kg/m^2]

        internal const double maxWindSpeed = 150;                                                // [km/h]
        internal const double maxNewWindSpeedVariation = 20;                                     // [km/h]
        internal const double maxNewWindDirectionVariation = 30;                                 // [°]
        internal const double windVariationProbability = 0.1;
        internal const double weightPreviousWindSpeed = 0.95;

        internal const double snowDegradationFactorPerDay = 0.8;

        internal const double meanRH = 60.0;
        internal const double fogHighRH = 100.0;
        internal const double fogLoRH = 80.0;
        internal const double fogDeltaRH = fogHighRH - fogLoRH;
        internal const double deltaDewPoint = 0.1;

        // MeteoSeriesSimulator
        internal const int hoursPerBlock = 3;
        internal const int blocksPerDay = hoursPerDay / hoursPerBlock;

        internal const double minNewSnow = 1;
        internal const double maxNewSnow = 20;
        internal const double maxNewSnowRandom = 1 + maxNewSnow - minNewSnow;
        internal const int fogDissolveStartLo = 6;
        internal const int fogDissolveStartHi = 8;
        internal const int fogDissolveEndLo = 10;
        internal const int fogDissolveEndHi = 14;

        internal static List<int> daysPerMonth = new List<int> { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        internal static List<int> averageClearDaysPerMonth = new List<int> { 5, 5, 5, 0, 5, 5, 10, 10, 5, 5, 0, 0 };
        internal static List<int> averageCoveredDaysPerMonth = new List<int> { 5, 5, 5, 10, 5, 5, 5, 5, 5, 5, 10, 5 };
        internal static List<int> averageSnowyowDaysPerMonth = new List<int> { 10, 10, 0, 0, 0, 0, 0, 0, 0, 0, 5, 10 };
        internal static List<int> averageFoggyDaysPerMonth = new List<int> { 10, 5, 0, 0, 0, 0, 0, 0, 0, 5, 10, 10 };

        // Random noixse and outliers
        internal const double randomNoiseVariation = 0.1;

        internal const double probabilityPeriodOutlier = 0.001;
        internal const double probabilityHourOutlier = 0.001;
        internal const double probabilityBlockOutlier = 0.001;

    }
}
