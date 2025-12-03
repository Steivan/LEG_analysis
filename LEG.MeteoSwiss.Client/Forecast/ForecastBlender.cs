
using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.MeteoSwiss.Client.Forecast
{
    public class ForecastBlender
    {
        public static List<MeteoParameters> CreateBlendedForecast(
            DateTime now, // <-- Reference time
            List<MeteoParameters> longTermData,
            List<MeteoParameters> midTermData,
            List<MeteoParameters> shortTermData,
            int smoothingFilterId = 0)      // smoothing filters 0, 1, 2, ... ; -1 = no smoothing
        {
            // --- STEP 1: Initialize the full 15-minute time axis ---

            // Find the total duration from the longest forecast (URL 1)
            var startTime = longTermData.Min(p => p.Time).AddMinutes(-45);
            var endTime = longTermData.Max(p => p.Time);

            var blendedData = new Dictionary<DateTime, MeteoParameters>();
            var quarterInterval = TimeSpan.FromMinutes(15);

            // Create the full 15-minute time index
            for (var time = startTime; time <= endTime.AddMinutes(45); time = time.AddMinutes(15))
            {
                // Initialize all records as empty or interpolated later
                blendedData[time] = new MeteoParameters(time, quarterInterval, null, null, null, null, null, null, null, null, null, null, null);
            }

            // --- STEP 2: Apply Long-Term Base (Hourly to 15-min Upscaling) ---

            foreach (var hourData in longTermData.Where(p => p.Temperature.HasValue))
            {
                // Upscale the hourly data to four 15-minute slots
                for (int i = 0; i < 4; i++)
                {
                    var quarterTime = hourData.Time.AddMinutes(15 * i - 45);
                    if (blendedData.ContainsKey(quarterTime))
                    {
                        // This is the BASE LAYER.
                        blendedData[quarterTime] = hourData with
                        {
                            Time = quarterTime,
                            Interval = quarterInterval
                        };
                    }
                }
            }

            // --- STEP 3: Patch with Mid-Term High-Res (Hourly ICON-D2) ---
            // Overwrites the Long-Term data for the first ~3 days.

            foreach (var hourData in midTermData.Where(p => p.Temperature.HasValue))
            {
                // Repeat the upscaling logic: ICON-D2 is higher quality than ECMWF
                for (int i = 0; i < 4; i++)
                {
                    var quarterTime = hourData.Time.AddMinutes(15 * i - 45);
                    if (blendedData.ContainsKey(quarterTime))
                    {
                        // OVERWRITE: Higher fidelity hourly data
                        blendedData[quarterTime] = UpdatMeteoParametersRecord(blendedData[quarterTime], hourData);
                    }
                }
            }

            // Smooth after mid-term patching
            if (smoothingFilterId >= 0) blendedData = SmoothBlender.SmoothBlendedPeriod(blendedData, filterId: smoothingFilterId);

            // --- STEP 4: Patch with Short-Term High-Res (15-min Nearcast) ---
            // Overwrites all prior data for the first ~48 hours.

            foreach (var quarterData in shortTermData.Where(p => p.Temperature.HasValue))
            {
                var quarterTime = quarterData.Time;
                if (blendedData.ContainsKey(quarterData.Time))
                {
                    // OVERWRITE: Highest fidelity, highest resolution data
                    blendedData[quarterTime] = UpdatMeteoParametersRecord(blendedData[quarterTime], quarterData);
                }
            }

            // --- STEP 5: Apply Synchronization Filter ---
            // 1. Find the current hour rounded down (e.g., 10:23 AM becomes 10:00 AM)
            var endOfCurrentHour = now.Date.AddHours(now.Hour);

            // 2. The first 15-minute timestamp we want is the one ending 45 minutes earlier.
            //    (e.g., 10:00 AM - 45 min = 9:15 AM). 
            //    This represents the 15-min slot starting at 9:00 AM.
            var filterCutoffTime = endOfCurrentHour.AddMinutes(-45);

            // 3. Filter the final list to include only records at or after the cutoff time.
            return blendedData.Values
                .Where(p => p.Time >= filterCutoffTime)
                .OrderBy(p => p.Time)
                .ToList();
        }

        private static MeteoParameters UpdatMeteoParametersRecord(MeteoParameters baseRecord, MeteoParameters newRecord)
        {
            return baseRecord with
            {
                SunshineDuration = newRecord.SunshineDuration ?? baseRecord.SunshineDuration,
                DirectRadiation = newRecord.DirectRadiation ?? baseRecord.DirectRadiation,
                DirectNormalIrradiance = newRecord.DirectNormalIrradiance ?? baseRecord.DirectNormalIrradiance,
                GlobalRadiation = newRecord.GlobalRadiation ?? baseRecord.GlobalRadiation,
                DiffuseRadiation = newRecord.DiffuseRadiation ?? baseRecord.DiffuseRadiation,
                Temperature = newRecord.Temperature ?? baseRecord.Temperature,
                WindSpeed = newRecord.WindSpeed ?? baseRecord.WindSpeed,
                WindDirection = newRecord.WindDirection ?? baseRecord.WindDirection,
                SnowDepth = baseRecord.SnowDepth,     // No snow depth in nowcast
                RelativeHumidity = newRecord.RelativeHumidity ?? baseRecord.RelativeHumidity,
                DewPoint = newRecord.DewPoint ?? baseRecord.DewPoint,
                DirectRadiationVariance = newRecord.DirectRadiationVariance ?? baseRecord.DirectRadiationVariance
            };
        }
    }
}