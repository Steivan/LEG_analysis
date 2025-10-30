using LEG.CoreLib.Abstractions.ReferenceData;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;

namespace LEG.CoreLib.SampleData.ReferenceData
{
    public class DictionaryMeteoProfiles : IDictionaryMeteoProfiles
    {
        public IReadOnlyDictionary<string, MeteoProfile> MeteoDict { get; }

        public DictionaryMeteoProfiles()
        {
            MeteoDict = new Dictionary<string, MeteoProfile>(StringComparer.OrdinalIgnoreCase)
            {
                // Selected weather stations => TODO: Extract from actual weather data
                ["COV"] = new MeteoProfile("COV", 4, "System",
                    [0.56, 0.56, 0.53, 0.48, 0.44, 0.45, 0.53, 0.53, 0.53, 0.56, 0.5, 0.5]),
                ["SAM"] = new MeteoProfile("SAM", 4, "System",
                    [0.56, 0.56, 0.53, 0.48, 0.44, 0.45, 0.53, 0.53, 0.53, 0.56, 0.5, 0.5]),
                ["SCU"] = new MeteoProfile("SCU", 4, "System",
                    [0.56, 0.56, 0.53, 0.48, 0.44, 0.45, 0.53, 0.53, 0.53, 0.56, 0.5, 0.5]),
                ["SMA"] = new MeteoProfile("SMA", 4, "System",
                    [0.266, 0.366, 0.46, 0.517, 0.594, 0.617, 0.655, 0.609, 0.529, 0.377, 0.271, 0.23]),

                // Additional entries
                ["Bos_cha_meteo_all"] = new MeteoProfile("Bos_cha_meteo_all", 2, "meteo10",
                    [0.1695, 0.319375, 0.548625, 0.671775, 0.646, 0.646, 0.642, 0.622, 0.603525, 0.494175, 0.294375, 0.1585]),
                ["Bos_cha_meteo_N"] = new MeteoProfile("Bos_cha_meteo_N", 2, "meteo9",
                    [0.1695, 0.3577, 0.5985, 0.689, 0.646, 0.646, 0.642, 0.622, 0.619, 0.5391, 0.3297, 0.1585]),
                ["Bos_cha_meteo_S"] = new MeteoProfile("Bos_cha_meteo_S", 2, "meteo8",
                    [0.1695, 0.28105, 0.49875, 0.65455, 0.646, 0.646, 0.642, 0.622, 0.58805, 0.44925, 0.25905, 0.1585]),
                ["Clozza_meteo"] = new MeteoProfile("Clozza_meteo", 2, "meteo5",
                    [0.339, 0.511, 0.665, 0.689, 0.646, 0.646, 0.642, 0.622, 0.619, 0.599, 0.471, 0.317]),
                ["default_meteo"] = new MeteoProfile("default_meteo", 0, "meteo1",
                    [0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5]),
                ["Ligist_meteo"] = new MeteoProfile("Ligist_meteo", 2, "meteo2",
                    [0.329, 0.503, 0.556, 0.533, 0.45, 0.541, 0.54, 0.515, 0.541, 0.452, 0.27, 0.251]),
                ["Maur_meteo"] = new MeteoProfile("Maur_meteo", 2, "meteo3",
                    [0.266, 0.366, 0.46, 0.517, 0.594, 0.617, 0.655, 0.609, 0.529, 0.377, 0.271, 0.23]),
                ["Scuol_meteo"] = new MeteoProfile("Scuol_meteo", 2, "meteo6",
                    [0.56, 0.56, 0.53, 0.48, 0.44, 0.45, 0.53, 0.53, 0.53, 0.56, 0.5, 0.5]),
                ["Senn_meteo"] = new MeteoProfile("Senn_meteo", 2, "meteo4",
                    [0.288, 0.505, 0.672, 0.717, 0.633, 0.616, 0.667, 0.611, 0.622, 0.477, 0.309, 0.2]),
                ["Solar_meteo"] = new MeteoProfile("Solar_meteo", 2, "meteo7",
                    [0.65, 0.65, 0.7, 0.75, 0.75, 0.75, 0.75, 0.75, 0.75, 0.7, 0.6, 0.6]),

                // Test entries
                ["TestProfile"] = new MeteoProfile("TestMeteoId", 4, "TestOwnerMeteo",
                    [0.3, 0.35, 0.4, 0.45, 0.5, 0.55, 0.55, 0.5, 0.45, 0.4, 0.35, 0.3]),
            };
        }
    }
}