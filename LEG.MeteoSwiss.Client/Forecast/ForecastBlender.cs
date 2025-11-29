
using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.MeteoSwiss.Client.Forecast
{
    public class ForecastBlender
    {
        public static List<MeteoParameters> CreateBlendedForecast(
            DateTime now, // <-- Reference time
            List<ForecastPeriod> longTermData,
            List<ForecastPeriod> midTermData,
            List<NowcastPeriod> shortTermData,
            int smoothingFilterId = 0)      // smoothing filters 0, 1, 2, ... ; -1 = no smoothing
        {
            // --- STEP 1: Initialize the full 15-minute time axis ---

            // Find the total duration from the longest forecast (URL 1)
            var startTime = longTermData.Min(p => p.Time).AddMinutes(-45);
            var endTime = longTermData.Max(p => p.Time);

            var blendedData = new Dictionary<DateTime, MeteoParameters>();

            // Create the full 15-minute time index
            for (var time = startTime; time <= endTime.AddMinutes(45); time = time.AddMinutes(15))
            {
                // Initialize all records as empty or interpolated later
                blendedData[time] = new MeteoParameters(time, null, null, null, null, null, null, null, null, null);
            }

            // --- STEP 2: Apply Long-Term Base (Hourly to 15-min Upscaling) ---

            foreach (var hourData in longTermData.Where(p => p.TemperatureC.HasValue))
            {
                // Upscale the hourly data to four 15-minute slots
                for (int i = 0; i < 4; i++)
                {
                    var quarterTime = hourData.Time.AddMinutes(15 * i - 45);
                    if (blendedData.ContainsKey(quarterTime))
                    {
                        // This is the BASE LAYER.
                        // Note: Wind speed conversion Km/h -> m/s is required here (factor 0.2778)
                        blendedData[quarterTime] = new MeteoParameters(
                            Time: quarterTime,
                            SunshineDuration: null,                                     // Not available in forecast
                            DirectRadiation: hourData.DirectRadiationWm2,
                            DirectNormalIrradiance: hourData.DirectNormalIrradianceWm2,
                            GlobalRadiation: hourData.GlobalRadiationWm2,
                            DiffuseRadiation: hourData.DiffuseRadiationWm2,
                            Temperature: hourData.TemperatureC,
                            WindSpeed: hourData.WindSpeedKmh, // Hourly Wind
                            SnowDepth: 0.0,                                             // Placeholder for Snow Depth
                            DirectRadiationVariance: null                               // Not available in forecast
                        );
                    }
                }
            }

            // --- STEP 3: Patch with Mid-Term High-Res (Hourly ICON-D2) ---
            // Overwrites the Long-Term data for the first ~3 days.

            foreach (var hourData in midTermData.Where(p => p.TemperatureC.HasValue))
            {
                // Repeat the upscaling logic: ICON-D2 is higher quality than ECMWF
                for (int i = 0; i < 4; i++)
                {
                    var quarterTime = hourData.Time.AddMinutes(15 * i - 45);
                    if (blendedData.ContainsKey(quarterTime))
                    {
                        // OVERWRITE: Higher fidelity hourly data
                        blendedData[quarterTime] = blendedData[quarterTime] with
                        {
                            DirectRadiation = hourData.DirectRadiationWm2 ?? blendedData[quarterTime].DirectRadiation,
                            DirectNormalIrradiance = hourData.DirectNormalIrradianceWm2 ?? blendedData[quarterTime].DirectNormalIrradiance,
                            GlobalRadiation = hourData.GlobalRadiationWm2 ?? blendedData[quarterTime].GlobalRadiation,
                            DiffuseRadiation = hourData.DiffuseRadiationWm2 ?? blendedData[quarterTime].DiffuseRadiation,
                            Temperature = hourData.TemperatureC ?? blendedData[quarterTime].Temperature,
                            WindSpeed = hourData.WindSpeedKmh ?? blendedData[quarterTime].WindSpeed,
                            SnowDepth = hourData.SnowDepthM ?? blendedData[quarterTime].SnowDepth
                        };
                    }
                }
            }

            // Smooth after mid-term patching
            if (smoothingFilterId >= 0) blendedData = SmoothBlender.SmoothBlendedPeriod(blendedData, filterId : smoothingFilterId);

            // --- STEP 4: Patch with Short-Term High-Res (15-min Nearcast) ---
            // Overwrites all prior data for the first ~48 hours.

            foreach (var quarterData in shortTermData.Where(p => p.TemperatureC.HasValue))
            {
                if (blendedData.ContainsKey(quarterData.Time))
                {
                    // OVERWRITE: Highest fidelity, highest resolution data
                    blendedData[quarterData.Time] = blendedData[quarterData.Time] with
                    {
                        DirectRadiation = quarterData.DirectRadiationWm2 ?? blendedData[quarterData.Time].DirectRadiation,
                        DirectNormalIrradiance = quarterData.DirectNormalIrradianceWm2 ?? blendedData[quarterData.Time].DirectNormalIrradiance,
                        GlobalRadiation = quarterData.GlobalRadiationWm2 ?? blendedData[quarterData.Time].GlobalRadiation,
                        DiffuseRadiation = quarterData.DiffuseRadiationWm2 ?? blendedData[quarterData.Time].DiffuseRadiation,
                        Temperature = quarterData.TemperatureC ?? blendedData[quarterData.Time].Temperature,
                        WindSpeed = quarterData.WindSpeedKmh ?? blendedData[quarterData.Time].WindSpeed,
                        SnowDepth = blendedData[quarterData.Time].SnowDepth     // No snow depth in nowcast     
                    };
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
    }
}