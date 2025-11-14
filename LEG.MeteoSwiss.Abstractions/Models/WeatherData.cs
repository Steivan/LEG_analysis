using System.Diagnostics.CodeAnalysis;

namespace LEG.MeteoSwiss.Abstractions.Models
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Names match external data source columns")]
    public class WeatherData
    {
        public DateTime Timestamp { get; set; }

        // STAC temperature (°C)
        public double? tre200s0 { get; set; }
        // STAC GHI (W/m²)
        public double? gre000s0 { get; set; }
        // Open-Meteo temperature (°C)
        public double? temperature_2m { get; set; }
        // Open-Meteo global radiation (W/m²)
        public double? global_radiation { get; set; }
    }
}