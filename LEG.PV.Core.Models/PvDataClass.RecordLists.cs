
namespace LEG.PV.Core.Models
{
    public partial class PvDataClass
    {
        public record PvRecordLists
        {
            public PvRecordLists(DateTime timestamp, int index,
                List<double?> power, List<double?> residuals,
                List<double?> radiation, List<double?> temperature, List<double?> windSpeed)
            {
                Timestamp = timestamp;
                Index = index;
                Power = power;
                Residuals = residuals;
                Radiation = radiation;
                Temperature = temperature;
                WindSpeed = windSpeed;
            }

            public DateTime Timestamp { get; init; }                // Timestamp [YYYY-MM-DD HH:MM:SS]
            public int Index { get; init; }                         // Index [unitless]
            public List<double?> Power { get; init; }               // P [W]
            public List<double?> Residuals { get; init; }
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
            public PvRecordLabels(
                List<string> powerLabels, List<string> residualsLabels,
                List<string> radiationLabels, List<string> temperatureLabels, List<string> windSpeedLabels)
            {
                PowerLabels = powerLabels;
                ResidualsLabels = residualsLabels;
                RadiationLabels = radiationLabels;
                TemperatureLabels = temperatureLabels;
                WindSpeedLabels = windSpeedLabels;
            }
            public List<string> PowerLabels { get; init; }
            public List<string> ResidualsLabels { get; init; }
            public List<string> RadiationLabels { get; init; }
            public List<string> TemperatureLabels { get; init; }
            public List<string> WindSpeedLabels { get; init; }
        }

    }
}
