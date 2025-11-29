using System;
using System.Collections.Generic;

namespace LEG.MeteoSwiss.Abstractions.Models
{
    public static class UnifiedWeatherDataExtensions
    {
        public static IEnumerable<UnifiedWeatherData> ToUnifiedWeatherData(this WeatherCsvRecord record)
        {
            var interval = TimeSpan.FromMinutes(10); // Raw historic data interval
            var anchor = IntervalAnchor.End;

            if (record.SunshineDuration.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.SunshineDuration,
                    record.SunshineDuration,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.DirectRadiation.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.DirectRadiation,
                    record.DirectRadiation,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.DirectNormalIrradiance.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.DirectNormalIrradiance,
                    record.DirectNormalIrradiance,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.ShortWaveRadiation.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.GlobalRadiation,
                    record.ShortWaveRadiation,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.DiffuseRadiation.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.DiffuseRadiation,
                    record.DiffuseRadiation,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.Temperature2m.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.Temperature,
                    record.Temperature2m,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.WindSpeed10min_kmh.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.WindSpeed,
                    record.WindSpeed10min_kmh,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.WindDirection.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.WindDirection,
                    record.WindDirection,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.SnowDepth.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.SnowDepth,
                    record.SnowDepth,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.RelativeHumidity2m.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.RelativeHumidity,
                    record.RelativeHumidity2m,
                    WeatherDataSource.Historic,
                    anchor);

            if (record.DewPoint2m.HasValue)
                yield return new UnifiedWeatherData(
                    record.ReferenceTimestamp,
                    interval,
                    record.StationAbbr,
                    MeteoParameterType.DewPoint,
                    record.DewPoint2m,
                    WeatherDataSource.Historic,
                    anchor);
        }

        public static IEnumerable<UnifiedWeatherData> ToUnifiedWeatherData(this ForecastPeriod period, string stationId, WeatherDataSource source)
        {
            var interval = TimeSpan.FromHours(1); // Forecast interval (adjust to 15' if harmonized)
            var anchor = IntervalAnchor.End;

            if (period.DirectRadiationWm2.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.DirectRadiation, period.DirectRadiationWm2, source, anchor);

            if (period.DirectNormalIrradianceWm2.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.DirectNormalIrradiance, period.DirectNormalIrradianceWm2, source, anchor);

            if (period.GlobalRadiationWm2.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.GlobalRadiation, period.GlobalRadiationWm2, source, anchor);

            if (period.DiffuseRadiationWm2.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.DiffuseRadiation, period.DiffuseRadiationWm2, source, anchor);

            if (period.TemperatureC.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.Temperature, period.TemperatureC, source, anchor);

            if (period.WindSpeedKmh.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.WindSpeed, period.WindSpeedKmh, source, anchor);

            if (period.WindDirectionDeg.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.WindDirection, period.WindDirectionDeg, source, anchor);

            if (period.SnowDepthM.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.SnowDepth, period.SnowDepthM, source, anchor);

            if (period.RelativeHumidity.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.RelativeHumidity, period.RelativeHumidity, source, anchor);

            if (period.DewPointC.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.DewPoint, period.DewPointC, source, anchor);
        }

        public static IEnumerable<UnifiedWeatherData> ToUnifiedWeatherData(this NowcastPeriod period, string stationId, WeatherDataSource source)
        {
            var interval = TimeSpan.FromMinutes(15); // Nearcast interval
            var anchor = IntervalAnchor.End;

            if (period.DirectRadiationWm2.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.DirectRadiation, period.DirectRadiationWm2, source, anchor);

            if (period.DirectNormalIrradianceWm2.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.DirectNormalIrradiance, period.DirectNormalIrradianceWm2, source, anchor);

            if (period.GlobalRadiationWm2.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.GlobalRadiation, period.GlobalRadiationWm2, source, anchor);

            if (period.DiffuseRadiationWm2.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.DiffuseRadiation, period.DiffuseRadiationWm2, source, anchor);

            if (period.TemperatureC.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.Temperature, period.TemperatureC, source, anchor);

            if (period.WindSpeedKmh.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.WindSpeed, period.WindSpeedKmh, source, anchor);

            if (period.WindDirectionDeg.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.WindDirection, period.WindDirectionDeg, source, anchor);

            if (period.RelativeHumidity.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.RelativeHumidity, period.RelativeHumidity, source, anchor);

            if (period.DewPointC.HasValue)
                yield return new UnifiedWeatherData(period.Time, interval, stationId, MeteoParameterType.DewPoint, period.DewPointC, source, anchor);
        }
    }
}