using LEG.MeteoSwiss.Abstractions.Models;
using static LEG.PV.Core.Models.DataRecords;

namespace LEG.PV.Core.Models
{
    public class DataRecords
    {
        public record PvRecord
        {
            public PvRecord(
                DateTime timestamp, 
                int index,
                GeometryFactors geometryFactors,
                MeteoParameters meteoParameters,
                double weight, 
                double age, 
                double? measuredPower)
            {
                Timestamp = timestamp;
                Index = index;
                GeometryFactors = geometryFactors;
                MeteoParameters = meteoParameters;
                Weight = weight;
                Age = age;
                MeasuredPower = measuredPower;
            }
            public DateTime Timestamp { get; init; }                                            // Timestamp [YYYY-MM-DD HH:MM:SS]
            public int Index { get; init; }                                                     // Index [unitless]
            public GeometryFactors GeometryFactors { get; init; }
            public MeteoParameters MeteoParameters { get; init; }
            public double Weight { get; init; }
            public double Age { get; init; }                                                    // Age [years]
            public double? MeasuredPower { get; init; }                                         // P_meas [W]
            public bool HasMeasuredPower => MeasuredPower.HasValue;
            
            public double ComputedPower(                                                        // P_meas [W]
                PvModelParams modelParams, 
                double installedPower,
                int periodsPerHour) 
            {
                return PvRTWAJacobian.EffectiveCellPower(installedPower, periodsPerHour,
                    GeometryFactors,
                    //GeometryFactors.DirectGeometryFactor,
                    //GeometryFactors.DiffuseGeometryFactor,
                    //GeometryFactors.SinSunElevation,
                    MeteoParameters, 
                    Age,
                    modelParams
                    );
            }
        }

        public record PvRecordCalculated
        {
            public PvRecordCalculated(DateTime timestamp, int index, 
                GeometryFactors geometryFactors,
                MeteoParameters meteoParameters,
                double age, double measuredPower, double computedPower)
            {
                Timestamp = timestamp;
                Index = index;
                GeometryFactors = geometryFactors;
                MeteoParameters = meteoParameters;
                Age = age;
                MeasuredPower = measuredPower;
                ComputedPower = computedPower;
            }

            public DateTime Timestamp { get; init; }                                            // Timestamp [YYYY-MM-DD HH:MM:SS]
            public int Index { get; init; }                                                     // Index [unitless]
            public GeometryFactors GeometryFactors { get; init; }
            public MeteoParameters MeteoParameters { get; init; }
            public double Weight => 1.0;
            public double Age { get; init; }                                                    // Age [years]
            public double MeasuredPower { get; init; }                                          // P_meas [W]
            public double ComputedPower { get; init; }                                          // P_comp [W]
        }

        public record GeometryFactors
        { 
            public GeometryFactors(double directGeometryFactor, double diffuseGeometryFactor, double sinSunElevation)
            {
                DirectGeometryFactor = directGeometryFactor;
                DiffuseGeometryFactor = diffuseGeometryFactor;
                SinSunElevation = sinSunElevation;
            }
            public double DirectGeometryFactor { get; init; }                                   // G_POA / G_ref [unitless]
            public double DiffuseGeometryFactor { get; init; }                                  // G_POA / G_ref [unitless]
            public double SinSunElevation { get; init; }                                        // G_GHI / G_DNI [unitless]
        }

        public record PvRecordLists
        {
            public PvRecordLists(DateTime timestamp, int index, List<double?> power, List<double?> radiation, List<double?> temperature, List<double?> windSpeed)
            {
                Timestamp = timestamp;
                Index = index;
                Power = power;
                Radiation = radiation;
                Temperature = temperature;
                WindSpeed = windSpeed;
            }

            public DateTime Timestamp { get; init; }                // Timestamp [YYYY-MM-DD HH:MM:SS]
            public int Index { get; init; }                         // Index [unitless]
            public List<double?> Power { get; init; }               // P [W]
            public List<double?> Radiation { get; init; }           // G_POA [W/m²]
            public List<double?> Temperature { get; init; }         // T [°C]
            public List<double?> WindSpeed { get; init; }           // v_wind [m/s]
            public bool HasMeteoData()
            {
                if (Radiation.All(x => !x.HasValue)) return false;
                if (Temperature.All(x => !x.HasValue)) return false;
                if (WindSpeed.All(x => !x.HasValue)) return false;
                return true;
            }
            
        }
        public record PvRecordLabels
        {
            public PvRecordLabels(List<string> powerLabels, List<string> radiationLabels, List<string> temperatureLabels, List<string> windSpeedLabels)
            {
                PowerLabels = powerLabels;
                RadiationLabels = radiationLabels;
                TemperatureLabels = temperatureLabels;
                WindSpeedLabels = windSpeedLabels;
            }
            public List<string> PowerLabels { get; init; }    
            public List<string> RadiationLabels { get; init; }  
            public List<string> TemperatureLabels { get; init; } 
            public List<string> WindSpeedLabels { get; init; }
        }
        public record PvModelParams
        {
            public PvModelParams(double etha, double gamma, double u0, double u1, double lDegr)
            {
                this.Etha = etha;
                this.Gamma = gamma;
                this.U0 = u0;
                this.U1 = u1;
                this.LDegr = lDegr;
            }

            public double Etha { get; init; }
            public double Gamma { get; init; }
            public double U0 { get; init; }
            public double U1 { get; init; }
            public double LDegr { get; init; }
        }

        public static PvModelParams GetDefaultPriorModelParams()
        {
            var (etha, gamma, u0, u1, lDegr) = PvPriorConfig.GetAllPriorsMeans();
            return new PvModelParams(etha, gamma, u0, u1, lDegr);
        }

    }
}
