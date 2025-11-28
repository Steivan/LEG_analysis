
namespace LEG.MeteoSwiss.Client.Forecast
{
    public class ForecastBlender
    {
        public static List<BlendedPeriod> CreateBlendedForecast(
            DateTime now, // <-- Reference time
            List<ForecastPeriod> longTermData,
            List<ForecastPeriod> midTermData,
            List<NowcastPeriod> shortTermData,
            int smoothingFilterId = 1)
        {
            // --- STEP 1: Initialize the full 15-minute time axis ---

            // Find the total duration from the longest forecast (URL 1)
            var startTime = longTermData.Min(p => p.Time).AddMinutes(-45);
            var endTime = longTermData.Max(p => p.Time);

            var blendedData = new Dictionary<DateTime, BlendedPeriod>();

            // Create the full 15-minute time index
            for (var time = startTime; time <= endTime.AddMinutes(45); time = time.AddMinutes(15))
            {
                // Initialize all records as empty or interpolated later
                blendedData[time] = new BlendedPeriod(time, null, null, null, null, null, null);
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
                        blendedData[quarterTime] = new BlendedPeriod(
                            Time: quarterTime,
                            TempC: hourData.TemperatureC,
                            WindKmh: hourData.WindSpeedKmh, // Hourly Wind$
                            DirectHRWm2: hourData.DirectRadiationWm2, // Hourly DNI
                            DNIWm2: hourData.DirectNormalIrradianceWm2, // Hourly DNI
                            DiffuseHRWm2: hourData.DiffuseRadiationWm2,
                            SnowDepthM: 0.0 // Placeholder for Snow Depth
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
                            TempC = hourData.TemperatureC ?? blendedData[quarterTime].TempC,
                            WindKmh = hourData.WindSpeedKmh ?? blendedData[quarterTime].WindKmh,
                            DirectHRWm2 = hourData.DirectRadiationWm2 ?? blendedData[quarterTime].DirectHRWm2,
                            DNIWm2 = hourData.DirectNormalIrradianceWm2 ?? blendedData[quarterTime].DNIWm2,
                            DiffuseHRWm2 = hourData.DiffuseRadiationWm2 ?? blendedData[quarterTime].DiffuseHRWm2,
                            SnowDepthM = hourData.SnowDepthM ?? blendedData[quarterTime].SnowDepthM
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
                        TempC = quarterData.TemperatureC ?? blendedData[quarterData.Time].TempC,
                        WindKmh = quarterData.WindSpeedKmh ?? blendedData[quarterData.Time].WindKmh,
                        DirectHRWm2 = quarterData.DirectRadiationWm2 ?? blendedData[quarterData.Time].DirectHRWm2,
                        DNIWm2 = quarterData.DirectNormalIrradianceWm2 ?? blendedData[quarterData.Time].DNIWm2,
                        DiffuseHRWm2 = quarterData.DiffuseRadiationWm2 ?? blendedData[quarterData.Time].DiffuseHRWm2,
                        SnowDepthM = blendedData[quarterData.Time].SnowDepthM     // No snow depth in nowcast     
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