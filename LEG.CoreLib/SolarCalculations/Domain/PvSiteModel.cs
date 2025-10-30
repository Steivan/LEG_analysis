using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.SwissTopo.Abstractions;
using LEG.SwissTopo.Client.SwissTopo;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LEG.CoreLib.SolarCalculations.Domain
{
    public class PvSiteModel(
        PvSite pvSite,
        IReadOnlyCollection<Inverter> inverters,
        IReadOnlyDictionary<string, PvRoof[]> roofsPerInverter,
        IReadOnlyCollection<Consumer> consumers,
        MeteoProfile meteoProfile) : IPvSiteModel
    {
        private static readonly List<RecordRoofProperties> recordRoofProperties = [];

        public PvSite PvSite { get; set; } = pvSite;
        public RecordMaddBuildingProperties? MaddBuildingProperties { get; private set; }
        public RecordBuildingProperties? DetailedBuildingProperties { get; private set; }
        public List<RecordRoofProperties> BuildingRoofsProperties { get; } = recordRoofProperties;
        public List<RecordRoofPropertiesMonthly> BuildingRoofsMonthlyProperties { get; } = [];
        public IReadOnlyCollection<Inverter> Inverters { get; set; } = inverters;
        public IReadOnlyDictionary<string, PvRoof[]> RoofsPerInverter { get; set; } = roofsPerInverter;
        public IReadOnlyCollection<Consumer> Consumers { get; set; } = consumers;
        public MeteoProfile MeteoProfile { get; set; } = meteoProfile;

        // Explicit implementation for the interface properties
        object? IPvSiteModel.MaddBuildingProperties => this.MaddBuildingProperties;
        object? IPvSiteModel.DetailedBuildingProperties => this.DetailedBuildingProperties;
        IReadOnlyList<object> IPvSiteModel.BuildingRoofsProperties => [.. BuildingRoofsProperties.Cast<object>()];
        IReadOnlyList<object> IPvSiteModel.BuildingRoofsMonthlyProperties => [.. BuildingRoofsMonthlyProperties.Cast<object>()];

        // The implementation still uses the concrete types
        public async Task FetchBuildingPropertiesAsync(IBuildingFinder buildingFinder,
            ICoordinateTransformation coordinateTransformer)
        {
            // Convert WGS84 (lon / lat) to LV95 (Easting / Northing)
            var lv95Coords = await coordinateTransformer.FromWgs84ToLv95(PvSite.Lon, PvSite.Lat);

            double? xCoord = null;
            double? yCoord = null;
            if (lv95Coords.HasValue)
            {
                xCoord = lv95Coords.Value.eastingLv95;
                yCoord = lv95Coords.Value.northingLv95;
            }

            var (buildingEgId, (X, Y), buildingArea, maddProps, detailedProps) =
                await buildingFinder.GetBuildingDataFromMaddAsync(
                    PvSite.EgId,
                    zip: PvSite.ZipNumber,
                    streetName: PvSite.StreetName,
                    houseNumber: PvSite.HouseNumber,
                    x: xCoord,
                    y: yCoord);

            MaddBuildingProperties = maddProps as RecordMaddBuildingProperties;
            DetailedBuildingProperties = detailedProps as RecordBuildingProperties;

            var (buildingPolygon, source) =
                await buildingFinder.GetBuildingPolygonAsync(buildingEgId, X, Y, buildingArea);
        }

        // Explicit implementation for the interface method
        async Task IPvSiteModel.FetchBuildingPropertiesAsync(object buildingFinder, object coordinateTransformer)
        {
            if (buildingFinder is IBuildingFinder bf && coordinateTransformer is ICoordinateTransformation ct)
            {
                await this.FetchBuildingPropertiesAsync(bf, ct);
            }
            else
            {
                throw new System.ArgumentException("Invalid argument types passed for buildingFinder or coordinateTransformer.");
            }
        }
    }
}