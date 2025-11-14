using LEG.MeteoSwiss.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LEG.MeteoSwiss.Abstractions
{
    public interface IMeteoDataService : IDisposable
    {
        // ** REFACTORED: Removed bbox, made stationId required, and added granularity. **
        Task<List<WeatherData>> GetHistoricalWeatherAsync(string startDate, string endDate, string stationId, string granularity = "PT10M");
        Task<List<WeatherData>> GetOpenMeteoHistoricalAsync(double latitude, double longitude, string startDate, string endDate);
        Task<byte[]> GetShortTermForecastRawAsync(double[] bbox);
    }
}