using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LEG.Common.Utils;
using LEG.HorizonProfiles.Abstractions;
using System.Globalization;

namespace LEG.HorizonProfiles.Client
{
    public class HorizonProfileClient : IHorizonProfileClient
    {
        private readonly string _apiKey;
        private readonly string _cacheFile = "C:\\code\\LEG_analysis\\Data\\CacheHorizonProfiles\\horizon_profiles.json";
        private readonly HttpClient _httpClient = new();
        private Dictionary<(double Lat, double Lon, double Azimuth), double> cache = [];
        private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

        public HorizonProfileClient(string googleApiKey)
        {
            _apiKey = googleApiKey ?? throw new ArgumentNullException(nameof(googleApiKey));
            LoadCache();
        }

        public async Task<List<double>> GetHorizonAnglesAsync(
            double lat,
            double lon,
            double? siteElev,
            double roofHeight = 10.0,
            List<double>? azimuths = null,
            double minDistKm = 0.5,
            double maxDistKm = 50.0,
            int numPoints = 50,
            double diameterKm = 0.01)
        {
            // This method was previously named GetHorizonAnglesAndSaveCacheAsync
            // Round lat/lon seconds to 2 decimal places (~30cm) for caching
            lat = GeoUtils.RoundToSecDecimal(lat, secondsDecimals: 2);
            lon = GeoUtils.RoundToSecDecimal(lon, secondsDecimals: 2);

            // Default azimuths: every 5° from -150 to 150 (South=0°)
            var azimuthsLocal = azimuths ?? [..Enumerable.Range(0, 61).Select(i => -150 + i * 5.0)];

            // Validate inputs
            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                throw new ArgumentException("Invalid lat/lon");
            if (azimuthsLocal.Any(az => az < -180 || az > 180))
                throw new ArgumentException("Azimuths must be in [-180, 180]");
            if (minDistKm <= 0 || maxDistKm <= minDistKm || numPoints < 2 || diameterKm <= 0)
                throw new ArgumentException("Invalid distance or diameter");

            // Fetch site elevation if not provided (average over area)
            var effectiveElev = siteElev ?? await GetAverageElevationAsync(lat, lon, diameterKm);
            effectiveElev += roofHeight;

            var horizonAngles = new List<double>();
            foreach (var azPv in azimuthsLocal)
            {
                var key = (lat, lon, azPv);
                if (cache.TryGetValue(key, out var cachedAngle))
                {
                    horizonAngles.Add(cachedAngle);
                    continue;
                }

                var azGeo = (azPv + 180) % 360;
                var points = GenerateRayPoints(lat, lon, azGeo, minDistKm, maxDistKm, numPoints);

                var elevations = await QueryElevationApiAsync(points, diameterKm);

                var maxAngle = 0.0;
                for (var i = 0; i < elevations.Count; i++)
                {
                    var distKm = points[i].DistanceKm;
                    var elev = elevations[i];
                    var deltaElev = elev - effectiveElev;
                    var angle = GeoUtils.RadToDeg(Math.Atan2(deltaElev, distKm * 1000)); // * 180 / Math.PI; 

                    if ((distKm < 1.0 && Math.Abs(deltaElev) > 1000) || Math.Abs(angle) > 45)
                    {
                        continue;
                    }

                    maxAngle = Math.Max(maxAngle, angle);
                }

                cache[key] = maxAngle;
                horizonAngles.Add(maxAngle);
            }

            await SaveCacheAsync();
            return horizonAngles;
        }

        // ... (All other private helper methods: GetAverageElevationAsync, GenerateCirclePoints, etc.)
        // Get average elevation over a circular area
        private async Task<double> GetAverageElevationAsync(double lat, double lon, double diameterKm)
        {
            var points = GenerateCirclePoints(lat, lon, diameterKm);
            var locations = string.Join("|", points.Select(p => $"{p.Lat.ToString(CultureInfo.InvariantCulture)},{p.Lon.ToString(CultureInfo.InvariantCulture)}"));
            var url = $"https://maps.googleapis.com/maps/api/elevation/json?locations={locations}&key={_apiKey}";
            var response = await RetryApiCallAsync(url);
            var json = JsonDocument.Parse(response);
            var elevations = json.RootElement.GetProperty("results").EnumerateArray()
                                .Select(r => r.GetProperty("elevation").GetDouble()).ToList();

            // Filter outliers (e.g., >1000m difference from median)
            var median = elevations.OrderBy(e => e).Skip(elevations.Count / 2).First();
            elevations = [..elevations.Where(e => Math.Abs(e - median) < 1000)];
            return elevations.Count != 0 ? elevations.Average() : median;
        }

        // Generate points in a circle (center + 4 points at radius D/2)
        private static List<(double Lat, double Lon)> GenerateCirclePoints(double lat, double lon, double diameterKm)
        {
            var points = new List<(double Lat, double Lon)> { (lat, lon) }; // Center
            var radiusKm = diameterKm / 2;
            var latRad = GeoUtils.DegToRad(lat); // lat * Math.PI / 180; 
            var lonRad = GeoUtils.DegToRad(lon); // lon * Math.PI / 180;

            // Add 4 points at 0°, 90°, 180°, 270° at radius D/2
            var angles = new[] { 0, 90, 180, 270 };
            foreach (var angle in angles)
            {
                var azRad = GeoUtils.DegToRad(angle); // angle * Math.PI / 180;
                var (newLat, newLon, _) = CalculateDestPoint(latRad, lonRad, azRad, radiusKm);
                points.Add((newLat, newLon));
            }
            return points;
        }

        // Generate ray points logarithmically along azimuth (geographic: North=0°)
        private static List<(double Lat, double Lon, double DistanceKm)> GenerateRayPoints(
            double lat,
            double lon,
            double azGeo,
            double minDistKm,
            double maxDistKm,
            int numPoints)
        {
            var points = new List<(double, double, double)>();
            var latRad = GeoUtils.DegToRad(lat); // lat * Math.PI / 180;
            var lonRad = GeoUtils.DegToRad(lon); // lon * Math.PI / 180;
            var azRad = GeoUtils.DegToRad(azGeo); // azGeo * Math.PI / 180;

            // Logarithmic spacing: d_i = minDistKm * (maxDistKm / minDistKm)^(i/(numPoints-1))
            for (var i = 0; i < numPoints; i++)
            {
                var t = i / (double)(numPoints - 1);
                var distKm = minDistKm * Math.Pow(maxDistKm / minDistKm, t);
                points.Add(CalculateDestPoint(latRad, lonRad, azRad, distKm));
            }

            return points;
        }

        // Haversine formula for destination point
        private static (double Lat, double Lon, double DistanceKm) CalculateDestPoint(double latRad, double lonRad, double azRad, double distKm)
        {
            const double R = 6371; // Earth's radius (km)
            var d = distKm / R;
            var lat2 = Math.Asin(Math.Sin(latRad) * Math.Cos(d) + Math.Cos(latRad) * Math.Sin(d) * Math.Cos(azRad));
            var lon2 = lonRad + Math.Atan2(Math.Sin(azRad) * Math.Sin(d) * Math.Cos(latRad),
                                           Math.Cos(d) - Math.Sin(latRad) * Math.Sin(lat2));
            return (GeoUtils.RadToDeg(lat2), GeoUtils.RadToDeg(lon2), distKm);
            //return (lat2 * 180 / Math.PI, lon2 * 180 / Math.PI, distKm);
        }

        // Query Google Elevation API for path with area averaging
        private async Task<List<double>> QueryElevationApiAsync(List<(double Lat, double Lon, double DistanceKm)> points, double diameterKm)
        {
            var elevations = new List<double>();
            foreach (var (Lat, Lon, _) in points)
            {
                var elev = await GetAverageElevationAsync(Lat, Lon, diameterKm);
                elevations.Add(elev);
            }
            return elevations;
        }

        // Retry API call with exponential backoff
        private async Task<string> RetryApiCallAsync(string url, int maxRetries = 3)
        {
            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException) when (i < maxRetries - 1)
                {
                    await Task.Delay(1000 * (1 << i)); // Exponential backoff: 1s, 2s, 4s
                }
            }
            throw new HttpRequestException("API call failed after retries");
        }

        // Load cache from JSON file
        private void LoadCache()
        {
            cache = [];
            if (!File.Exists(_cacheFile)) return;

            var json = File.ReadAllText(_cacheFile);
            var data = JsonSerializer.Deserialize<List<CacheEntry>>(json);
            if (data != null)
            {
                foreach (var entry in data)
                    cache[(entry.Lat, entry.Lon, entry.Azimuth)] = entry.HorizonAngle;
            }
        }

        // Save cache to JSON file
        private async Task SaveCacheAsync()
        {
            var data = cache.Select(kvp => new CacheEntry
            {
                Lat = kvp.Key.Lat,
                Lon = kvp.Key.Lon,
                Azimuth = kvp.Key.Azimuth,
                HorizonAngle = kvp.Value
            }).ToList();
            await File.WriteAllTextAsync(_cacheFile, JsonSerializer.Serialize(data, s_jsonOptions));
        }

        // Cache entry structure
        private class CacheEntry
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
            public double Azimuth { get; set; }
            public double HorizonAngle { get; set; }
        }
    }
}