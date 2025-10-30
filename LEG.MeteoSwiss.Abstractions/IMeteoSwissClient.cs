using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LEG.MeteoSwiss.Abstractions
{
    public interface IMeteoSwissClient : IDisposable
    {
        // ** REFACTORED: Signature updated to match the working implementation. **
        // Removed bbox, made stationId required, and granularity is a simple string.
        Task<string[]> GetHistoricalDataAsync(string startDate, string endDate, string stationId, string granularity);

        Task<byte[]> GetShortTermForecastAsync(double[] bbox);
        Task ListItemsForCollectionAsync(string collectionId, int limit = 5);
        Task<List<string>> ListAvailableCollectionsAsync();
    }
}