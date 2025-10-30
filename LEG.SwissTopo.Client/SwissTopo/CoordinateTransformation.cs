using System.Net.Http;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json;
using LEG.SwissTopo.Abstractions;

namespace LEG.SwissTopo.Client.SwissTopo
{
    public class CoordinateTransformation : ICoordinateTransformation
    {
        // Correct URL: The official swisstopo REFRAME-service
        private const string ApiBaseUrl = "http://geodesy.geo.admin.ch/reframe/wgs84tolv95";

        // The API uses Longitude/Latitude, but labels them as 'Easting' and 'Northing'
        private const string LonParamName = "Easting";
        private const string LatParamName = "Northing";

        /// <summary>
        /// Converts WGS84 (Longitude/Latitude) into "Schweizer Landeskoordinaten" LV95 (Easting/Northing)
        /// via the official REFRAME GET-Service.
        /// </summary>
        /// <param name="wgs84Lon"></param>
        /// <param name="wgs84Lat"></param>
        /// <returns></returns>
        public async Task<(double eastingLv95, double northingLv95)?> FromWgs84ToLv95(double wgs84Lon, double wgs84Lat)
        {
            // 1. Coordinates as strings (with decimal point)
            var lonString = wgs84Lon.ToString(CultureInfo.InvariantCulture);
            var latString = wgs84Lat.ToString(CultureInfo.InvariantCulture);

            // 2. Final URL (with optional 'format=json' for a clean response)
            var url = $"{ApiBaseUrl}?{LonParamName}={lonString}&{LatParamName}={latString}&format=json";

            using var client = new HttpClient();

            try
            {
                var response = await client.GetAsync(url);

                var responseString = await response.Content.ReadAsStringAsync();

                response.EnsureSuccessStatusCode();

                // 3. Deserializing the JSON-response
                // Response-structure: {"Easting": "...", "Northing": "...", ...}
                var result = JsonConvert.DeserializeObject<ReframingResponse>(responseString);

                if (result != null &&
                    double.TryParse(result.Easting, NumberStyles.Float, CultureInfo.InvariantCulture, out double eastingLv95) &&
                    double.TryParse(result.Northing, NumberStyles.Float, CultureInfo.InvariantCulture, out double northingLv95))
                {
                    return (eastingLv95, northingLv95);
                }
            }
            catch (HttpRequestException e)
            {
                System.Console.WriteLine($"API-Fehler: {e.Message}");
            }
            catch (JsonException e)
            {
                System.Console.WriteLine($"JSON-Fehler: {e.Message}");
            }

            return null;
        }
    }

    // Helper class for deserializing the REFRAME-response
    public class ReframingResponse
    {
        public string Easting { get; set; } = string.Empty;
        public string Northing { get; set; } = string.Empty;

        // public string altitude { get; set; } // Optional
    }
}