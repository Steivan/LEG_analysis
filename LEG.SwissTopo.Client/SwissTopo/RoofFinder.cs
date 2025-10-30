using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json.Linq;
using System.Globalization;
using LEG.SwissTopo.Abstractions;

namespace LEG.SwissTopo.Client.SwissTopo
{
    public class RoofFinder : IRoofFinder
    {
        private static readonly GeoJsonReader geoJsonReader;
        private static readonly GeoJsonWriter geoJsonWriter;
        private static readonly GeometryFactory geometryFactory = new(new PrecisionModel(), 2056); // SRID 2056 for CH1903+ LV95

        // Initialize GeoJsonReader and Writer
        static RoofFinder()
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

        public async Task<List<RoofInfo>> GetOverlappingRoofsAsync(Geometry? buildingPolygon, BuildingInfo selectedBuilding, List<BuildingInfo> allBuildings)
        {
            var roofs = new List<RoofInfo>();
            if (buildingPolygon == null || selectedBuilding == null || allBuildings == null) return roofs;
            try
            {
                var envelope = buildingPolygon.EnvelopeInternal;
                const int loCount = 100;
                const int hiCount = 200;
                const int maxIterations = 10;
                var deltaLo = 0.0;
                var deltaHi = 300.0;
                JArray? results = null;

                for (int i = 0; i < maxIterations; i++)
                {
                    var delta = (deltaLo + deltaHi) / 2;
                    double minX = envelope.MinX - delta, minY = envelope.MinY - delta, maxX = envelope.MaxX + delta, maxY = envelope.MaxY + delta;
                    string bboxTest = $"{minX},{minY},{maxX},{maxY}";

                    results = await GeoAdminClient.IdentifyRoofsInBboxAsync(bboxTest);
                    int roofCount = results?.Count ?? 0;

                    if (roofCount >= loCount && roofCount <= hiCount) break;
                    if (roofCount < loCount) deltaLo = delta;
                    else deltaHi = delta;
                }

                if (results == null) return roofs;

                string? selectedEgrid = selectedBuilding.Egrid;
                var egridList = allBuildings
                    .Where(b => string.Equals(b.Egrid, selectedEgrid, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(b.EGID))
                    .ToList();

                foreach (var result in results)
                {
                    string? featureId = result?["id"]?.ToString();
                    if (string.IsNullOrEmpty(featureId)) continue;
                    var attrs = result?["properties"];
                    if (attrs == null) continue;

                    string? roofGwrEgid = attrs["gwr_egid"]?.ToString();
                    if (!string.IsNullOrEmpty(roofGwrEgid) && egridList.Any(b => string.Equals(b.EGID, roofGwrEgid, StringComparison.OrdinalIgnoreCase)))
                    {
                        double.TryParse(attrs["flaeche"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double area);
                        if (area > 20 * buildingPolygon.Area)
                        {
                            continue; // Skip large areas that might be regional features
                        }
                        roofs.Add(new RoofInfo
                        {
                            AreaM2 = area,
                            OrientationDeg = double.TryParse(attrs["ausrichtung"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var orientation) ? orientation : 0,
                            SlopeDeg = double.TryParse(attrs["neigung"]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var slope) ? slope : 0,
                            Suitability = attrs["klasse_text"]?.ToString() ?? attrs["eignung"]?.ToString(),
                            FeatureId = featureId
                        });
                    }
                }
            }
            catch (Exception)
            {
            }
            return roofs;
        }

        public async Task<Geometry?> GetRoofGeometryAsync(string featureId, double buildingX, double buildingY)
        {
            try
            {
                return await GeoAdminClient.GetRoofGeometryAsync(featureId, buildingX, buildingY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRoofGeometry for featureId {featureId}: {ex.Message}");
                return null;
            }
        }

        public async Task<RecordRoofProperties?> GetRoofPropertiesAsync(string featureId, double buildingX, double buildingY)
        {
            var feature = await GeoAdminClient.IdentifyRoofAsync(featureId, buildingX, buildingY);
            if (feature == null)
                return null;
            return MapperRoofProperties.MapFromGeoAdminResponse(feature);
        }

        public async Task<Dictionary<string, object?>> GetRoofAttributesAsync(
            string featureId,
            double buildingX,
            double buildingY,
            IEnumerable<string>? includeProperties = null,
            IEnumerable<string>? excludeProperties = null,
            bool includeGeometry = true)
        {
            var attributes = new Dictionary<string, object?>();
            var feature = await GeoAdminClient.IdentifyRoofAsync(featureId, buildingX, buildingY);
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

            if (includeGeometry)
            {
                var geom = feature["geometry"];
                if (geom != null)
                    attributes["geometry"] = geom.ToString();
            }

            var roofGeometry = await GetRoofGeometryAsync(featureId, buildingX, buildingY);
            if (roofGeometry != null)
            {
                if (!attributes.ContainsKey("shape_length"))
                    attributes["shape_length"] = roofGeometry.Length;
                if (!attributes.ContainsKey("shape_area"))
                    attributes["shape_area"] = roofGeometry.Area;
            }

            return attributes;
        }

        public async Task<Dictionary<string, object?>>
            GetRoofExtendedAttributesAsync(string featureId, double buildingX, double buildingY)
        {
            var attributes = await GetRoofAttributesAsync(featureId, buildingX, buildingY,
                includeProperties: null,
                excludeProperties: ["flaeche", "ausrichtung", "neigung", "eignung", "klasse_text"],
                includeGeometry: true);

            return attributes;
        }

        // Helper method to extract house number from address
        private static string ExtractHouseNumber(string address)
        {
            var parts = address.Split(' ');
            if (parts.Length < 2) return "";
            return parts[1].Split(['<'], 2)[0].Trim(); // Get house number before any HTML tags
        }
    }
}