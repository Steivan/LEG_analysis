using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.PV.Core.Models
{
    public class PvDataClass
    {
        public record PvRecord
        {
            public PvRecord(
                DateTime timestamp, 
                int index,
                PvGeometryFactors geometryFactors,
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
            public DateTime Timestamp { get; init; }                                           // Timestamp [YYYY-MM-DD HH:MM:SS]
            public int Index { get; init; }                                                    // Index [unitless]
            public PvGeometryFactors GeometryFactors { get; set; }
            public MeteoParameters MeteoParameters { get; set; }
            public double Weight { get; set; }
            public double Age { get; set; }                                                    // Age [years]
            public double? MeasuredPower { get; init; }                                        // P_meas [W]
            public bool HasMeasuredPower => MeasuredPower.HasValue;
            
            public PvPowerRecord ComputedPower(                                                // P_computed [W]
                PvModelParams modelParams, 
                double installedPower,
                int periodsPerHour) 
            {
                return PvPowerJacobian.EffectiveCellPower(
                    installedPower, 
                    periodsPerHour,
                    GeometryFactors,
                    MeteoParameters, 
                    Age,
                    modelParams
                    );
            }
        }

        public static PvModelParams GetDefaultPriorModelParams()
        {
            var (etha, gamma, u0, u1, lDegr) = PvPriorConfig.GetAllPriorsMeans();
            return new PvModelParams(etha, gamma, u0, u1, lDegr);
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

    }
}
