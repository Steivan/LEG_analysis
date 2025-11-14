namespace LEG.MeteoSwiss.Abstractions
{
    public static class MeteoSwissFieldNames
    {
        private static readonly Dictionary<string, string> _fieldMap = new()
        {
            ["Ta1Tows0"] = "Air Temperature (°C)",
            ["TdeTows0"] = "Dew Point (°C)",
            ["UreTows0"] = "Relative Humidity (%)",
            ["FklTowz1"] = "Wind Speed 10m (km/h)",
            ["Fk1Towz0"] = "Wind Speed 2m (km/h)",
            ["Dv1Towz0"] = "Wind Direction (°)",
            ["Fu3Towz0"] = "Gust Speed 2m (km/h)",
            ["Fu3Towz1"] = "Gust Speed 10m (km/h)",
            ["Gre000z0"] = "Global Radiation (W/m²)",
            ["Sre000z0"] = "Sunshine Duration (h)"
        };

        public static string GetFieldName(string code)
            => _fieldMap.TryGetValue(code, out var name) ? name : code;
    }
}