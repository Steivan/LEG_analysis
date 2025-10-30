using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using LEG.SwissTopo.Abstractions;

namespace LEG.SwissTopo.Client.SwissTopo
{
    public static class MapperMaddBuildingProperties
    {
        public static RecordMaddBuildingProperties? Parse(string xml)
        {
            XNamespace ns = "http://www.ech.ch/xmlns/eCH-0206/2";
            //XNamespace ns58 = "http://www.ech.ch/xmlns/eCH-0058/5";

            var doc = XDocument.Parse(xml);

            var buildingItem = doc.Descendants(ns + "buildingItem").FirstOrDefault();
            if (buildingItem == null) return null;

            var building = buildingItem.Element(ns + "building");
            var coordinates = building?.Element(ns + "coordinates");
            var dateOfConstructionElem = building?.Element(ns + "dateOfConstruction");

            var entranceItem = buildingItem.Descendants(ns + "buildingEntranceItem").FirstOrDefault();
            var entrance = entranceItem?.Element(ns + "buildingEntrance");
            //var entranceCoords = entrance?.Element(ns + "coordinates");
            var street = entrance?.Element(ns + "street");
            var streetNameItem = street?.Descendants(ns + "streetNameItem").FirstOrDefault();
            var locality = entrance?.Element(ns + "locality");

            var municipality = buildingItem.Element(ns + "municipality");
            var realestate = buildingItem.Descendants(ns + "realestateIdentificationItem").FirstOrDefault();

            return new RecordMaddBuildingProperties(
                EGID: buildingItem.Element(ns + "EGID")?.Value ?? "",
                OfficialBuildingNo: building?.Element(ns + "officialBuildingNo")?.Value ?? "",
                East: double.TryParse(coordinates?.Element(ns + "east")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var east) ? east : 0,
                North: double.TryParse(coordinates?.Element(ns + "north")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var north) ? north : 0,
                BuildingStatus: int.TryParse(building?.Element(ns + "buildingStatus")?.Value, out var status) ? status : 0,
                BuildingCategory: int.TryParse(building?.Element(ns + "buildingCategory")?.Value, out var cat) ? cat : 0,
                BuildingClass: int.TryParse(building?.Element(ns + "buildingClass")?.Value, out var cls) ? cls : 0,
                DateOfConstruction: dateOfConstructionElem?.Element(ns + "dateOfConstruction")?.Value ?? "",
                PeriodOfConstruction: int.TryParse(dateOfConstructionElem?.Element(ns + "periodOfConstruction")?.Value, out var period) ? period : 0,
                SurfaceAreaOfBuilding: double.TryParse(building?.Element(ns + "surfaceAreaOfBuilding")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var area) ? area : 0,
                NumberOfFloors: int.TryParse(building?.Element(ns + "numberOfFloors")?.Value, out var floors) ? floors : 0,
                MunicipalityName: municipality?.Element(ns + "municipalityName")?.Value ?? "",
                CantonAbbreviation: municipality?.Element(ns + "cantonAbbreviation")?.Value ?? "",
                StreetName: streetNameItem?.Element(ns + "descriptionLong")?.Value ?? "",
                HouseNumber: entrance?.Element(ns + "buildingEntranceNo")?.Value ?? "",
                ZipCode: locality?.Element(ns + "swissZipCode")?.Value ?? "",
                PlaceName: locality?.Element(ns + "placeName")?.Value ?? "",
                EGRID: realestate?.Element(ns + "EGRID")?.Value ?? ""
            );
        }
    }
}