using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace LEG.SwissTopo.Client.SwissTopo
{
    public static class GeoAdminClient
    {
        private static readonly HttpClient httpClient = new();
        private static readonly GeoJsonReader geoJsonReader = new();

        public static async Task<JArray?> SearchLocationsAsync(string street, string zip, string? houseNumberPrefix = null)
        {
            var query = $"{street} {zip}".Trim();
            if (!string.IsNullOrEmpty(houseNumberPrefix))
            {
                query = $"{street} {houseNumberPrefix} {zip}".Trim();
            }

            var url = $"https://api3.geo.admin.ch/rest/services/api/SearchServer?" +
                      $"type=locations&searchText={Uri.EscapeDataString(query)}&origins=address&limit=50&sr=2056";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);
            return json["results"] as JArray;
        }

        public static async Task<JObject?> IdentifyBuildingAsync(double x, double y, string? buildingEgid = null)
        {
            HttpResponseMessage? response = null;
            string? responseString = null;

            // Try with EGID first if provided
            if (!string.IsNullOrEmpty(buildingEgid))
            {
                var url = $"https://api3.geo.admin.ch/rest/services/api/MapServer/ch.bfs.gebaeude_wohnungs_register/{buildingEgid}";
                response = await httpClient.GetAsync(url);
                responseString = await response.Content.ReadAsStringAsync();
            }

            // Fallback to coordinates if EGID not provided or failed
            if (response == null || !response.IsSuccessStatusCode)
            {
                var url = $"https://api3.geo.admin.ch/rest/services/api/MapServer/identify?" +
                          $"geometryType=esriGeometryPoint&geometry={x},{y}&" +
                          $"layers=all:ch.bfs.gebaeude_wohnungs_register&tolerance=5&" +
                          $"imageDisplay=1,1,96&mapExtent=0,0,1,1&returnGeometry=true&geometryFormat=geojson&sr=2056";
                response = await httpClient.GetAsync(url);
                responseString = await response.Content.ReadAsStringAsync();
            }

            if (response == null || !response.IsSuccessStatusCode || string.IsNullOrEmpty(responseString))
                return null;

            var json = JObject.Parse(responseString);
            if (json["results"] is not JArray results || results.Count == 0)
                return null;

            return results[0] as JObject;
        }

        public static async Task<JObject?> IdentifyRoofAsync(string featureId, double buildingX, double buildingY)
        {
            // Fetch roof geometry to get a more precise centroid for the identify call
            var roofGeometry = await GetRoofGeometryAsync(featureId, buildingX, buildingY);
            double x = roofGeometry?.Centroid.X ?? buildingX;
            double y = roofGeometry?.Centroid.Y ?? buildingY;

            // Use Identify endpoint with the featureId and precise point
            string url = $"https://api3.geo.admin.ch/rest/services/api/MapServer/identify?" +
                         $"layers=all:ch.bfe.solarenergie-eignung-daecher&featureIds={featureId}&" +
                         $"tolerance=0&imageDisplay=1,1,96&returnGeometry=true&geometryFormat=geojson&sr=2056&" +
                         $"geometryType=esriGeometryPoint&geometry={x},{y}";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);

            if (json["results"] is JArray results && results.Count > 0)
            {
                if (results[0] is JObject feature && feature["id"]?.ToString() != "-99")
                    return feature;
            }
            return null;
        }

        public static async Task<JArray?> IdentifyRoofsInBboxAsync(string bbox)
        {
            string url = $"https://api3.geo.admin.ch/rest/services/api/MapServer/identify?" +
                         $"geometryType=esriGeometryEnvelope&geometry={bbox}&" +
                         $"layers=all:ch.bfe.solarenergie-eignung-daecher&tolerance=0&" +
                         $"imageDisplay=1,1,96&mapExtent=0,0,1,1&returnGeometry=true&geometryFormat=geojson&sr=2056";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);
            return json["results"] as JArray;
        }

        public static async Task<Geometry?> GetRoofGeometryAsync(string featureId, double buildingX, double buildingY)
        {
            // Use a bbox around the building to find the specific roof feature
            double delta = 50.0; // 50m radius
            string bbox = $"{buildingX - delta},{buildingY - delta},{buildingX + delta},{buildingY + delta}";

            var results = await IdentifyRoofsInBboxAsync(bbox);
            if (results == null) return null;

            // Find the feature matching the requested featureId
            foreach (var feature in results)
            {
                if (feature["id"]?.ToString() == featureId || feature["properties"]?["label"]?.ToString() == featureId)
                {
                    var geomJson = feature["geometry"]?.ToString();
                    return geomJson != null ? geoJsonReader.Read<Geometry>(geomJson) : null;
                }
            }
            return null; // No matching feature found
        }
    }
}