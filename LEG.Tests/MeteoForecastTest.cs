
using LEG.MeteoSwiss.Client.Forecast;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LEG.Tests
{
    [TestClass]
    public class MeteoForecastTest
    {
        // Selected stations, available parameters and blending weights
        List<string> selectedStationsIdList = ["SMA", "KLO", "HOE", "UEB"];
        List<string> selectedZips = ["8124", "7550"];

        [TestMethod]
        public async Task GetForecastForLatLon()
        {
            var (lat, lon) = (47.377925, 8.565742);     // SMA

            var client = new WeatherForecastClient();

            var longCast = await client.Get10DayPeriodsAsync(lat, lon);
            var midCast = await client.Get7DayPeriodsAsync(lat, lon);
            var nowCast = await client.GetNowcast15MinuteAsync(lat, lon);

            printForecastSamples($"Lat: {lat:F4}, Lon: {lon:F4}", longCast, midCast, nowCast);
        }

        [TestMethod]
        public async Task GetForecastForZipList()
        {
            var client = new WeatherForecastClient();

            foreach (var zip in selectedZips)
            {
                var longCast = await client.Get10DayPeriodsByZipCodeAsync(zip);
                var midCast = await client.Get7DayPeriodsByZipCodeAsync(zip);
                var nowCast = await client.GetNowcast15MinuteByZipCodeAsync(zip);

                printForecastSamples($"ZIP: {zip}", longCast, midCast, nowCast);
            }
        }

        [TestMethod]
        public async Task GetForecastForWeatherStations()
        {
            var client = new WeatherForecastClient();
            foreach (var stationId in selectedStationsIdList)
            {
                var longCast = await client.Get10DayPeriodsByStationIdAsync(stationId);
                var midCast = await client.Get7DayPeriodsByStationIdAsync(stationId);
                var nowCast = await client.GetNowcast15MinuteByStationIdAsync(stationId);

                printForecastSamples($"Station: {stationId}", longCast, midCast, nowCast);
            }
        }

        public void printForecastSamples(string location, List<ForecastPeriod> longCast, List<ForecastPeriod> midCast, List<NowcastPeriod> nowCast)
        {
            Console.WriteLine($"10-Day Forecast for {location}:");
            if (longCast.Count > 0)
            {
                var current = longCast[0];
                var last = longCast[^1];
                Console.WriteLine($"NOW     → {current.LocalTime:HH:mm} | {current.TemperatureC:F1}°C | WindSpeed: {current.WindSpeedKmh:F1} km/h | " +
                                              $"Direct: {current.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {current.DiffuseRadiationWm2:F0} W/m² | Solar: {current.DirectRadiationWm2:F0} W/m²");
                Console.WriteLine($"Outlook → {last.LocalTime:HH:mm} | {last.TemperatureC:F1}°C | WindSpeed: {last.WindSpeedKmh:F1} km/h | " +
                                              $"Direct: {last.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {last.DiffuseRadiationWm2:F0} W/m² | Solar: {last.DirectRadiationWm2:F0} W/m²");
            }

            Console.WriteLine($"7-Day Forecast for {location}:");
            if (midCast.Count > 0)
            {
                var current = midCast[0];
                var last = midCast[^1];
                Console.WriteLine($"NOW     → {current.LocalTime:HH:mm} | {current.TemperatureC:F1}°C | WindSpeed: {current.WindSpeedKmh:F1} km/h | " +
                                              $"Direct: {current.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {current.DiffuseRadiationWm2:F0} W/m² | Solar: {current.DirectRadiationWm2:F0} W/m²");
                Console.WriteLine($"Outlook → {last.LocalTime:HH:mm} | {last.TemperatureC:F1}°C | WindSpeed: {last.WindSpeedKmh:F1} km/h | " +
                                              $"Direct: {last.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {last.DiffuseRadiationWm2:F0} W/m² | Solar: {last.DirectRadiationWm2:F0} W/m²");
            }

            Console.WriteLine($"90-Hour Nowcast: for {location}:");
            if (nowCast.Count > 0)
            {
                var current = nowCast[0];
                var last = nowCast[^1];
                Console.WriteLine($"NOW     → {current.LocalTime:HH:mm} | {current.TemperatureC:F1}°C | WindSpeed: {current.WindSpeedKmh:F1} km/h | " +
                                              $"Direct: {current.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {current.DiffuseRadiationWm2:F0} W/m² | Solar: {current.SolarRadiationWm2:F0} W/m²");
                Console.WriteLine($"Outlook → {last.LocalTime:HH:mm} | {last.TemperatureC:F1}°C | WindSpeed: {last.WindSpeedKmh:F1} km/h | " +
                                              $"Direct: {last.DirectNormalIrradianceWm2:F0} W/m² | Diffuse: {last.DiffuseRadiationWm2:F0} W/m² | Solar: {last.SolarRadiationWm2:F0} W/m²");
            }
            Console.WriteLine();
        }
    }
}