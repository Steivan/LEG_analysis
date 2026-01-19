
namespace LEG.PV.Core.Models
{
    public partial class PvDataClass
    {
        public record PvRecordLists
        {
            public PvRecordLists(DateTime timestamp, int index,
                Dictionary<string, double?> consumption,
                Dictionary<string, double?> production,
                Dictionary<string, double?> residuals,
                Dictionary<string, double?> radiation,
                Dictionary<string, double?> temperature,
                Dictionary<string, double?> windSpeed,
                Dictionary<string, double?> snowDepth,
                Dictionary<string, double?> relativeHumidity)
            {
                Timestamp = timestamp;
                Index = index;
                Consumption = consumption;
                Production = production;
                Residuals = residuals;
                Radiation = radiation;
                Temperature = temperature;
                WindSpeed = windSpeed;
                SnowDepth = snowDepth;
                RelativeHumidity = relativeHumidity;
            }

            public DateTime Timestamp { get; init; }                        // Timestamp [YYYY-MM-DD HH:MM:SS]
            public int Index { get; init; }                                 // Index [unitless]
            public Dictionary<string, double?> Consumption { get; }         // P [W]
            public Dictionary<string, double?> Production { get; }          // P [W]
            public Dictionary<string, double?> Residuals { get; }
            public Dictionary<string, double?> Radiation { get;}            // G_POA [W/m²]
            public Dictionary<string, double?> Temperature { get;}          // T [°C]
            public Dictionary<string, double?> WindSpeed { get; }           // v_wind [m/s]
            public Dictionary<string, double?> SnowDepth { get; }           // T [°C]
            public Dictionary<string, double?> RelativeHumidity { get; }    // v_wind [m/s]
            public bool HasMeteoData()
            {
                if (Radiation == null || !Radiation.Values.Any(v => v.HasValue)) return false;
                if (Temperature == null || !Temperature.Values.Any(v => v.HasValue)) return false;
                if (WindSpeed == null || !WindSpeed.Values.Any(v => v.HasValue)) return false;
                if (SnowDepth == null || !SnowDepth.Values.Any(v => v.HasValue)) return false;
                if (RelativeHumidity == null || !RelativeHumidity.Values.Any(v => v.HasValue)) return false;
                return true;
            }

        }
        public record PvRecordLabels
        {
            public PvRecordLabels(
                List<string> consumptionLabels,
                List<string> productionLabels, 
                List<string> residualsLabels,
                List<string> radiationLabels, 
                List<string> temperatureLabels, 
                List<string> windSpeedLabels,
                List<string> snowDepthLabels,
                List<string> relativeHumidityLabels)
            {
                ConsumptionLabels = consumptionLabels;
                ProductionLabels = productionLabels;
                ResidualsLabels = residualsLabels;
                RadiationLabels = radiationLabels;
                TemperatureLabels = temperatureLabels;
                WindSpeedLabels = windSpeedLabels;
                SnowDepthLabels = snowDepthLabels;
                RelativeHumidityLabels = relativeHumidityLabels;
            }
            public List<string> ConsumptionLabels { get; init; }
            public List<string> ProductionLabels { get; init; }
            public List<string> ResidualsLabels { get; init; }
            public List<string> RadiationLabels { get; init; }
            public List<string> TemperatureLabels { get; init; }
            public List<string> WindSpeedLabels { get; init; }
            public List<string> SnowDepthLabels { get; init; }
            public List<string> RelativeHumidityLabels { get; init; }
        }
    }
}
