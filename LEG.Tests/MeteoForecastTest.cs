
using LEG.MeteoSwiss.Client.Forecast;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LEG.MeteoSwiss.Abstractions.Models;
using static LEG.MeteoSwiss.Client.Forecast.ForecastBlender;

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

            var longCast = await client.Get16DayMeteoParametersAsync(lat, lon);
            var midCast = await client.Get7DayMeteoParametersAsync(lat, lon);
            var nowCast = await client.GetNowcast15MinuteMeteoParametersAsync(lat, lon);

            var blendedForecast = CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast);

            printForecastSamples($"Lat: {lat:F4}, Lon: {lon:F4}", longCast, midCast, nowCast, blendedForecast);
        }

        [TestMethod]
        public async Task GetForecastForZipList()
        {
            var client = new WeatherForecastClient();

            foreach (var zip in selectedZips)
            {
                var longCast = await client.Get16DayMeteoParametersByZipCodeAsync(zip);
                var midCast = await client.Get7DayMeteoParametersByZipCodeAsync(zip);
                var nowCast = await client.GetNowcast15MinuteMeteoParametersByZipCodeAsync(zip);

                var blendedForecast = CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast);

                printForecastSamples($"ZIP: {zip}", longCast, midCast, nowCast, blendedForecast);
            }
        }

        [TestMethod]
        public async Task GetForecastForWeatherStations()
        {
            var client = new WeatherForecastClient();
            foreach (var stationId in selectedStationsIdList)
            {
                var longCast = await client.Get16DayMeteoParametersByStationIdAsync(stationId);
                var midCast = await client.Get7DayMeteoParametersByStationIdAsync(stationId);
                var nowCast = await client.GetNowcast15MinuteMeteoParametersByStationIdAsync(stationId);

                var blendedForecast = CreateBlendedForecast(DateTime.UtcNow, longCast, midCast, nowCast);

                printForecastSamples($"Station ID: {stationId}", longCast, midCast, nowCast, blendedForecast);
            }
        }

        private static void PrintMeteoParametersDataRecord(string label, MeteoParameters data)
        {
            Console.WriteLine($"{label,12} : {data.Time:dd.MM.yyyy} | {data.Interval.Minutes} m | {data.Temperature:F1}°C | WindSpeed: {data.WindSpeed:F1} km/h | " +
                $"DNI: {data.DirectNormalIrradiance:F0} W/m² | Diffuse: {data.DiffuseRadiation:F0} W/m² | Direct: {data.DirectRadiation:F0} W/m²");
        }

        public static void printForecastSamples(string location, List<MeteoParameters> longCast, List<MeteoParameters> midCast, List<MeteoParameters> nowCast, List<MeteoParameters> blendedForecast)
        {
            Console.WriteLine($"10-Day Forecast for {location}:");

            if (longCast.Count > 0)
            {
                // Convert to MeteoParameters for uniform output
                PrintMeteoParametersDataRecord("NOW", longCast[0]);
                PrintMeteoParametersDataRecord("Outlook", longCast[^1]);
            }

            Console.WriteLine($"7-Day Forecast for {location}:");
            if (midCast.Count > 0)
            {
                PrintMeteoParametersDataRecord("NOW", midCast[0]);
                PrintMeteoParametersDataRecord("Outlook", midCast[^1]);
            }

            Console.WriteLine($"90-Hour Nowcast: for {location}:");
            if (nowCast.Count > 0)
            {
                PrintMeteoParametersDataRecord("NOW", nowCast[0]);
                PrintMeteoParametersDataRecord("Outlook", nowCast[^1]);
            }

            Console.WriteLine($"Blended forecast: for {location}:");
            if (blendedForecast.Count > 0)
            {
                PrintMeteoParametersDataRecord("NOW", blendedForecast[0]);
                PrintMeteoParametersDataRecord("Outlook", blendedForecast[^1]);
            }

            Console.WriteLine();
        }

    }
}