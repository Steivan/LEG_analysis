
namespace LEG.MeteoSwiss.Abstractions.Models
{
    public record MeteoParameters(
        double? SunshineDuration,
        double? DirectRadiation,
        double? DirectNormalIrradiance,
        double? GlobalRadiation,
        double? DiffuseRadiation,
        double? Temperature,
        double? WindSpeed,
        double? SnowDepth,
        double? DirectRadiationVariance = null // Optional for history/forecast
    );

    public record StationMeteoData(string StationId, List<MeteoParameters> WeatherData);

}