using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEG.MeteoSwiss.Client.Forecast
{
    internal class ForecastBlender
    {
        public record BlendedPeriod
            (
            DateTime Time,
            double? TempC,
            double? WindMps, // Convert from km/h to m/s for model inputs
            double? DniWm2,
            double? DiffuseWm2
            // Add other fields as needed
            );

        public List<BlendedPeriod> CreateBlendedForecast(
            List<ForecastPeriod> longTermData,
            List<ForecastPeriod> midTermData,
            List<NowcastPeriod> shortTermData)
        {
            // --- STEP 1: Initialize the full 15-minute time axis ---

            // Find the total duration from the longest forecast (URL 1)
            var startTime = longTermData.Min(p => p.Time);
            var endTime = longTermData.Max(p => p.Time);

            var blendedData = new Dictionary<DateTime, BlendedPeriod>();

            // Create the full 15-minute time index
            for (var time = startTime; time <= endTime.AddMinutes(45); time = time.AddMinutes(15))
            {
                // Initialize all records as empty or interpolated later
                blendedData[time] = new BlendedPeriod(time, null, null, null, null);
            }

            // --- STEP 2: Apply Long-Term Base (Hourly to 15-min Upscaling) ---

            foreach (var hourData in longTermData.Where(p => p.TemperatureC.HasValue))
            {
                // Upscale the hourly data to four 15-minute slots
                for (int i = 0; i < 4; i++)
                {
                    var quarterTime = hourData.Time.AddMinutes(15 * i);
                    if (blendedData.ContainsKey(quarterTime))
                    {
                        // This is the BASE LAYER.
                        // Note: Wind speed conversion Km/h -> m/s is required here (factor 0.2778)
                        blendedData[quarterTime] = new BlendedPeriod(
                            Time: quarterTime,
                            TempC: hourData.TemperatureC,
                            WindMps: hourData.WindSpeedKmh * 0.2778, // Hourly Wind
                            DniWm2: hourData.DirectNormalIrradianceWm2, // Hourly DNI
                            DiffuseWm2: hourData.DiffuseRadiationWm2
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
                    var quarterTime = hourData.Time.AddMinutes(15 * i);
                    if (blendedData.ContainsKey(quarterTime))
                    {
                        // OVERWRITE: Higher fidelity hourly data (CORRECTED SYNTAX)
                        blendedData[quarterTime] = blendedData[quarterTime] with
                        {
                            TempC = hourData.TemperatureC, // Colon replaced with EQUALS SIGN
                            WindMps = hourData.WindSpeedKmh * 0.2778,
                            DniWm2 = hourData.DirectNormalIrradianceWm2,
                            DiffuseWm2 = hourData.DiffuseRadiationWm2
                        };
                    }
                }
            }

            // --- STEP 4: Patch with Short-Term High-Res (15-min Nearcast) ---
            // Overwrites all prior data for the first ~48 hours.

            foreach (var quarterData in shortTermData.Where(p => p.TemperatureC.HasValue))
            {
                if (blendedData.ContainsKey(quarterData.Time))
                {
                    // OVERWRITE: Highest fidelity, highest resolution data (CORRECTED SYNTAX)
                    blendedData[quarterData.Time] = blendedData[quarterData.Time] with
                    {
                        TempC = quarterData.TemperatureC, // Colon replaced with EQUALS SIGN
                        WindMps = quarterData.WindSpeedKmh * 0.2778,
                        DniWm2 = quarterData.DirectNormalIrradianceWm2,
                        DiffuseWm2 = quarterData.DiffuseRadiationWm2
                    };
                }
            }

            return blendedData.Values.OrderBy(p => p.Time).ToList();
        }

    }
}
