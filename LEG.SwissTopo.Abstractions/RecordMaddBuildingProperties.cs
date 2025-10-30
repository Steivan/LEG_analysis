
namespace LEG.SwissTopo.Abstractions
{
    public record RecordMaddBuildingProperties(
        string EGID,
        string OfficialBuildingNo,
        double East,
        double North,
        int BuildingStatus,
        int BuildingCategory,
        int BuildingClass,
        string DateOfConstruction,
        int PeriodOfConstruction,
        double SurfaceAreaOfBuilding,
        int NumberOfFloors,
        string MunicipalityName,
        string CantonAbbreviation,
        string StreetName,
        string HouseNumber,
        string ZipCode,
        string PlaceName,
        string EGRID
    );
}