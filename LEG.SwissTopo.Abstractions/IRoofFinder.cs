using NetTopologySuite.Geometries;

namespace LEG.SwissTopo.Abstractions
{
    public interface IRoofFinder
    {
        Task<List<RoofInfo>> GetOverlappingRoofsAsync(Geometry? buildingPolygon, BuildingInfo selectedBuilding, List<BuildingInfo> allBuildings);

        Task<Geometry?> GetRoofGeometryAsync(string featureId, double buildingX, double buildingY);

        Task<RecordRoofProperties?> GetRoofPropertiesAsync(string featureId, double buildingX, double buildingY);

        Task<Dictionary<string, object?>> GetRoofAttributesAsync(
            string featureId,
            double buildingX,
            double buildingY,
            IEnumerable<string>? includeProperties = null,
            IEnumerable<string>? excludeProperties = null,
            bool includeGeometry = true);

        Task<Dictionary<string, object?>> GetRoofExtendedAttributesAsync(string featureId, double buildingX, double buildingY);
    }
}