namespace LEG.SwissTopo.Abstractions
{
    public interface IBuildingFinder
    {
        Task<(string egid, (double X, double Y) coordinates, double buildingarea, object? maddProperties, object? detailedProperties)>
            GetBuildingDataFromMaddAsync(string trialEgId, string? zip, string? streetName, string? houseNumber, double? x, double? y);

        Task<(object? buildingPolygon, string source)>
            GetBuildingPolygonAsync(string egid, double x, double y, double gArea);
    }
}