using LEG.SwissTopo.Abstractions;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LEG.SwissTopo.Client.SwissTopo
{
    public class BuildingFinder : IBuildingFinder
    {
        private static readonly HttpClient httpClient = new();
        private static readonly GeoJsonReader geoJsonReader;
        private static readonly GeoJsonWriter geoJsonWriter;
        private static readonly GeometryFactory geometryFactory = new(new PrecisionModel(), 2056); // SRID 2056 for CH1903+ LV95

        static BuildingFinder()
        {
            try
            {
                geoJsonReader = new GeoJsonReader();
                geoJsonWriter = new GeoJsonWriter();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Required for interface or future instance use")]
        public async Task<List<BuildingInfo>> GetBuildingsByStreetZipAsync(string street, string zip, string? houseNumberPrefix = null)
        {
            var buildings = new List<BuildingInfo>();
            try
            {
                houseNumberPrefix = houseNumberPrefix?.ToLower();
                var results = await GeoAdminClient.SearchLocationsAsync(street, zip, houseNumberPrefix);
                if (results == null) return buildings;

                foreach (var result in results)
                {
                    var attrs = result["attrs"];
                    if (attrs?["origin"]?.ToString() != "address") continue;

                    var address = attrs["label"]?.ToString()?.Replace("<b>", "").Replace("</b>", "") ?? "";
                    var houseNumber = ExtractHouseNumber(address);

                    if (string.IsNullOrEmpty(houseNumberPrefix) || (houseNumber.StartsWith(houseNumberPrefix) && Regex.IsMatch(houseNumber, @"^\d+(?:[a-z]*(?:\.\d+)?)?$")))
                    {
                        var resultId = result["id"]?.ToString();
                        double.TryParse(attrs["y"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double x);
                        double.TryParse(attrs["x"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double y);
                        var (egid, garea, gwrEgid, gebnr, egrid, lparz,
                            properties,
                            buildingProperties) = await GetBuildingDetailsFromIdentifyAsync(x, y, buildingEgid: resultId ?? string.Empty);

                        buildings.Add(new BuildingInfo
                        {
                            Address = address,
                            EGID = egid ?? "",
                            X = x,
                            Y = y,
                            GArea = garea,
                            GwrEgid = gwrEgid,
                            GebNr = gebnr,
                            Egrid = egrid,
                            Lparz = lparz,
                            Properties = properties
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Log error if necessary
            }
            return buildings;
        }

        private static async Task GetBuildingDetailsMetaDataAsync()
        {
            try
            {
                var url = $"https://api3.geo.admin.ch/rest/services/api/MapServer/ch.bfs.gebaeude_wohnungs_register";
                var response = await httpClient.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBuildingDetailsMetaDataAsync: {ex.Message}");
                Console.Out.Flush();
            }
        }

        public static async Task<RecordBuildingProperties?> GetBuildingPropertiesAsync(double x, double y, string buildingEgid = "")
        {
            var feature = await GeoAdminClient.IdentifyBuildingAsync(x, y, buildingEgid);
            if (feature == null)
                return null;
            return MapperBuildingProperties.MapFromGeoAdminResponse(feature);
        }

        public static async Task<Dictionary<string, object?>> GetBuildingAttributesAsync(
            double x,
            double y,
            string buildingEgid = "",
            IEnumerable<string>? includeProperties = null,
            IEnumerable<string>? excludeProperties = null)
        {
            var attributes = new Dictionary<string, object?>();
            var feature = await GeoAdminClient.IdentifyBuildingAsync(x, y, buildingEgid);
            if (feature == null)
                return attributes;

            if (feature["properties"] is JObject props)
            {
                var selectAll = includeProperties == null || !includeProperties.Any();
                excludeProperties = selectAll ? (excludeProperties ?? []) : [];
                foreach (var prop in props)
                {
                    string key = prop.Key.ToLower();
                    if ((selectAll && !excludeProperties.Contains(prop.Key, StringComparer.OrdinalIgnoreCase)) ||
                        (!selectAll && (includeProperties?.Contains(prop.Key, StringComparer.OrdinalIgnoreCase) ?? false)))
                    {
                        if (prop.Value != null)
                        {
                            attributes[key] = prop.Value.Type == JTokenType.Array
                                ? prop.Value.ToObject<List<object>>()
                                : prop.Value.ToObject<object>();
                        }
                        else
                        {
                            attributes[key] = null;
                        }
                    }
                }
            }
            return attributes;
        }

        public static async Task<(Dictionary<string, object?> attributes, RecordBuildingProperties? properties)>
            GetBuildingExtendedAttributesAsync(double x, double y, string buildingEgid = "",
                IEnumerable<string>? includeProperties = null,
                IEnumerable<string>? excludeProperties = null)
        {
            var attributes = await GetBuildingAttributesAsync(x, y, buildingEgid, includeProperties, excludeProperties);
            var properties = await GetBuildingPropertiesAsync(x, y, buildingEgid);
            return (attributes, properties);
        }

        private static async Task<(string? egid, double garea, string? gwrEgid, string? gebnr, string? egrid, string? lparz,
            Dictionary<string, object?> properties,
            RecordBuildingProperties? buildingProperties)> GetBuildingDetailsFromIdentifyAsync(double x, double y, string buildingEgid = "")
        {
            var propertiesDict = await GetBuildingAttributesAsync(x, y, buildingEgid, includeProperties: [], excludeProperties: []);
            var buildingProperties = await GetBuildingPropertiesAsync(x, y, buildingEgid);

            var egid = propertiesDict.TryGetValue("egid", out var egidVal) ? egidVal?.ToString() : null;
            var gareaStr = propertiesDict.TryGetValue("garea", out var gareaVal) ? gareaVal?.ToString() : null;
            double.TryParse(gareaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double garea);
            string gwrEgid = "";
            string gebnr = propertiesDict.TryGetValue("gebnr", out var gebnrVal) ? gebnrVal?.ToString() ?? "" : "";
            string egrid = propertiesDict.TryGetValue("egrid", out var egridVal) ? egridVal?.ToString() ?? "" : "";
            string lparz = propertiesDict.TryGetValue("lparz", out var lparzVal) ? lparzVal?.ToString() ?? "" : "";

            return (egid, garea, gwrEgid, gebnr, egrid, lparz, propertiesDict, buildingProperties);
        }

        public static async Task<(string egid, double X, double Y, RecordMaddBuildingProperties? maddProperties)>
            GetMaddBuildingDataAsync(string egid)
        {
            var maddProperties = await MaddApiClient.FetchMaddBuildingPropertiesAsync(egid);
            if (maddProperties == null)
                return ("", double.NaN, double.NaN, null);

            return (maddProperties.EGID, maddProperties.East, maddProperties.North, maddProperties);
        }

        public async Task<(string egid, double X, double Y, RecordMaddBuildingProperties? maddProperties, RecordBuildingProperties? detailedProperties)>
            GetBuildingDataAsync(string egid)
        {
            var (maddEgid, x, y, maddProperties) = await GetMaddBuildingDataAsync(egid);
            if (string.IsNullOrEmpty(maddEgid))
                return ("", double.NaN, double.NaN, null, null);

            var detailedProperties = await GetBuildingPropertiesAsync(x, y, maddEgid);

            return (maddEgid, x, y, maddProperties, detailedProperties);
        }

        public async Task<(string egid,
                (double X, double Y) coordinates, double buildingarea,
                object? maddProperties,
                object? detailedProperties)>
            GetBuildingDataFromMaddAsync(string trialEgId,
                string? zip = null, string? streetName = null, string? houseNumber = null,
                double? x = null, double? y = null
            )
        {
            var buildingEgid = trialEgId;
            var hasAddress = (streetName != null && zip != null && houseNumber != null);
            var hasCoordinates = (x != null && y != null);
            var buildingX = (hasCoordinates && x.HasValue) ? x.Value : double.NaN;
            var buildingY = (hasCoordinates && y.HasValue) ? y.Value : double.NaN;
            var buildingArea = 200.0;

            if (string.IsNullOrEmpty(buildingEgid) && (hasAddress || hasCoordinates))
            {
                if (hasAddress)
                {
                    var buildings = await GetBuildingsByStreetZipAsync(streetName!, zip!, houseNumberPrefix: houseNumber);

                    if (buildings != null && buildings.Count == 1)
                    {
                        buildingEgid = buildings[0].EGID ?? "";
                        if (buildings[0].X > 0 && buildings[0].Y > 0)
                        {
                            buildingX = buildings[0].X;
                            buildingY = buildings[0].Y;
                            buildingArea = buildings[0].GArea > 0 ? buildings[0].GArea : buildingArea;
                        }
                    }
                }

                if (string.IsNullOrEmpty(buildingEgid) && hasCoordinates)
                {
                    var properties = await GetBuildingPropertiesAsync(buildingX, buildingY, buildingEgid: "");
                    var newEgId = properties?.egid;
                    if (!string.IsNullOrEmpty(newEgId))
                    {
                        buildingEgid = newEgId;
                        if (properties?.dkode != null && double.TryParse(properties.dkode.ToString(), out var dx))
                            buildingX = dx;
                        if (properties?.dkodn != null && double.TryParse(properties.dkodn.ToString(), out var dy))
                            buildingY = dy;
                        if (properties?.garea != null && double.TryParse(properties.garea.ToString(), out var da))
                            buildingArea = da;
                    }
                }
            }

            if (string.IsNullOrEmpty(buildingEgid))
                return ("", (buildingX, buildingY), buildingArea, null, null);

            var (maddEgid, X, Y, maddRecord, detailedProperties) = await GetBuildingDataAsync(buildingEgid);
            if (maddRecord == null)
            {
                return (maddEgid, (X, Y), buildingArea, null, detailedProperties);
            }
            (buildingX, buildingY) = (X, Y);
            (x, y) = (maddRecord.East, maddRecord.North);

            var oneBuildings = await GetBuildingsByStreetZipAsync(maddRecord.StreetName, maddRecord.ZipCode,
                houseNumberPrefix: maddRecord.HouseNumber);

            if (oneBuildings == null || oneBuildings.Count != 1)
            {
                Console.WriteLine($"Error in count of buildings fetched for {maddRecord.StreetName} {maddRecord.HouseNumber}, {maddRecord.ZipCode}");
            }
            else
            {
                detailedProperties = await GetBuildingPropertiesAsync(oneBuildings[0].X, oneBuildings[0].Y, buildingEgid: oneBuildings[0].EGID ?? string.Empty);
            }

            return (maddRecord.EGID, (buildingX, buildingY), buildingArea, maddRecord, detailedProperties);
        }

        public static Task<Geometry?> CreateFallbackPolygonAsync(double x, double y, double gArea)
        {
            try
            {
                double offset = 5; // Default 5m radius
                if (gArea > 0)
                {
                    offset = Math.Sqrt(gArea) / 2; // Approximate side length
                }
                var coordinates = new Coordinate[]
                {
                    new (x - offset, y - offset),
                    new (x + offset, y - offset),
                    new (x + offset, y + offset),
                    new (x - offset, y + offset),
                    new (x - offset, y - offset)
                };
                var polygon = geometryFactory.CreatePolygon(coordinates);
                return Task.FromResult<Geometry?>(polygon);
            }
            catch (Exception)
            {
                return Task.FromResult<Geometry?>(null);
            }
        }

        public async Task<(object? buildingPolygon, string source)> GetBuildingPolygonAsync(string egid, double x, double y, double gArea)
        {
            Geometry? buildingPolygon = null;
            var source = "WFS";
            //buildingPolygon = await FetchBuildingPolygonAsync(egid, x, y);   // TODO: Reactivate when WFS is fixed
            if (buildingPolygon == null)
            {
                buildingPolygon = await CreateFallbackPolygonAsync(x, y, gArea);
                source = "Fallback (from area)";
            }
            return (buildingPolygon, source);
        }

        private static string ExtractHouseNumber(string address)
        {
            var parts = address.Split(' ');
            if (parts.Length < 2) return "";
            return parts[1].Split(['<'], 2)[0].Trim();
        }
    }
}