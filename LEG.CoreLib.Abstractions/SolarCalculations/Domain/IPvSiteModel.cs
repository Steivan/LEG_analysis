using System.Collections.Generic;
using System.Threading.Tasks;

namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain
{
    public interface IPvSiteModel
    {
        PvSite PvSite { get; }
        object? MaddBuildingProperties { get; }
        object? DetailedBuildingProperties { get; }
        IReadOnlyList<object> BuildingRoofsProperties { get; }
        IReadOnlyList<object> BuildingRoofsMonthlyProperties { get; }
        IReadOnlyCollection<Inverter> Inverters { get; }
        IReadOnlyDictionary<string, PvRoof[]> RoofsPerInverter { get; }
        IReadOnlyCollection<Consumer> Consumers { get; }
        MeteoProfile MeteoProfile { get; }

        Task FetchBuildingPropertiesAsync(object buildingFinder, object coordinateTransformer);
    }
}