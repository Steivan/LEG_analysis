using static LEG.MeteoSwiss.Abstractions.Models.MeteoParameterTypes;

namespace LEG.PV.Data.Processor.Helpers
{
    public class MeteoForecastSeriesBlender
    {
        public async Task<Dictionary<DateTime, MeteoParameters>> CreateBlendedForecastDictFromDicts(
            DateTime now,                                           // <-- Reference time
            Dictionary<DateTime, MeteoParameters> farCastSeriesDict,
            Dictionary<DateTime, MeteoParameters> midCastSeriesDict,
            Dictionary<DateTime, MeteoParameters> nearCastSeriesDict,
            int smoothingFilterId = 0)                              // smoothing filters 0, 1, 2, ... ; -1 = no smoothing
        {
            var supportInterval = nearCastSeriesDict.First().Value.Interval;

            // If necessary, synchronize timeStaps
            if (!MeteoIntervalConverter.FirstTimeStampIsSynced(nearCastSeriesDict))
            {
                nearCastSeriesDict = MeteoIntervalConverter.SyncTimeStamps(nearCastSeriesDict);
            }
            // Map midCast to nearCast support
            var mappedMidCastDict = MeteoIntervalConverter.MeteoFromToConvertor(midCastSeriesDict, supportInterval);
            if (!MeteoIntervalConverter.FirstTimeStampIsSynced(mappedMidCastDict))
            {
                mappedMidCastDict = MeteoIntervalConverter.SyncTimeStamps(mappedMidCastDict);
            }
            // Map farCast to nearCast support
            var mappedFarCastDict = MeteoIntervalConverter.MeteoFromToConvertor(farCastSeriesDict, supportInterval);
            if (!MeteoIntervalConverter.FirstTimeStampIsSynced(mappedFarCastDict))
            {
                mappedFarCastDict = MeteoIntervalConverter.SyncTimeStamps(mappedFarCastDict);
            }

            // Get support
            var nearCastStart = nearCastSeriesDict.Min(kv => kv.Key);
            var nearCastEnd = nearCastSeriesDict.Max(kv => kv.Key);
            var mappedMidCastStart = mappedMidCastDict.Min(kv => kv.Key);
            var mappedMidCastEnd = mappedMidCastDict.Max(kv => kv.Key);
            var mappedFarCastStart = mappedFarCastDict.Min(kv => kv.Key);
            var mappedFarCastEnd = mappedFarCastDict.Max(kv => kv.Key);
            var startTime = new DateTime[] { now, nearCastStart, mappedMidCastStart, mappedFarCastStart }.Min();
            var endTime = new DateTime[] { nearCastEnd, mappedMidCastEnd, mappedFarCastEnd }.Max();

            // Create empty series
            var blendedSeriesDict = new Dictionary<DateTime, MeteoParameters>();
            for (var time = startTime; time <= endTime; time += supportInterval)
            {
                // Initialize all records as empty or interpolated later
                blendedSeriesDict[time] = new MeteoParameters(time, supportInterval, null, null, null, null, null, null, null, null, null, null, null);
            }
            // Populate with mappedFarCast
            foreach (var record in mappedFarCastDict.Where(kvp => kvp.Value.Temperature.HasValue))
            {
                blendedSeriesDict[record.Key] = record.Value;
            }
            // Patch with mappedMidCast
            foreach (var record in mappedMidCastDict.Where(kvp => kvp.Value.Temperature.HasValue))
            {
                blendedSeriesDict[record.Key] = record.Value;
            }
            // Smooth after midCast patching
            if (smoothingFilterId >= 0) blendedSeriesDict = MeteoSeriesSmoothing.SmoothBlendedPeriod(blendedSeriesDict, filterId: smoothingFilterId);
            // Patch with nearCast
            foreach (var record in nearCastSeriesDict.Where(kvp => kvp.Value.Temperature.HasValue))
            {
                blendedSeriesDict[record.Key] = record.Value;
            }

            return blendedSeriesDict;
        }

        public async Task<List<MeteoParameters>> CreateBlendedForecastListFromLists(
            DateTime now,                                           // <-- Reference time
            List<MeteoParameters> farCastSeriesList,
            List<MeteoParameters> midCastSeriesList,
            List<MeteoParameters> nearCastSeriesList,
            int smoothingFilterId = 0)                              // smoothing filters 0, 1, 2, ... ; -1 = no smoothing
        {
            // --- STEP 1: Initialize the full 15-minute time axis ---

            // Find the total duration from the longest forecast (URL 1)
            var startTime = farCastSeriesList.Min(p => p.Time).AddMinutes(-45);
            var endTime = farCastSeriesList.Max(p => p.Time);

            var blendedSeriesDict = new Dictionary<DateTime, MeteoParameters>();
            var quarterInterval = TimeSpan.FromMinutes(15);

            // Create the full 15-minute time index
            for (var time = startTime; time <= endTime; time = time.AddMinutes(15))
            {
                // Initialize all records as empty or interpolated later
                blendedSeriesDict[time] = new MeteoParameters(time, quarterInterval, null, null, null, null, null, null, null, null, null, null, null);
            }

            // --- STEP 2: Apply Long-Term Base (Hourly to 15-min Upscaling) ---
            foreach (var hourData in farCastSeriesList.Where(p => p.Temperature.HasValue))
            {
                // Upscale the hourly data to four 15-minute slots
                for (int i = 0; i < 4; i++)
                {
                    var quarterTime = hourData.Time.AddMinutes(15 * i - 45);
                    if (blendedSeriesDict.ContainsKey(quarterTime))
                    {
                        // This is the BASE LAYER.
                        blendedSeriesDict[quarterTime] = hourData with
                        {
                            Time = quarterTime,
                            Interval = quarterInterval
                        };
                    }
                }
            }

            // --- STEP 3: Patch with Mid-Term High-Res (Hourly ICON-D2) ---
            // Overwrites the Long-Term data for the first ~3 days.
            foreach (var hourData in midCastSeriesList.Where(p => p.Temperature.HasValue))
            {
                // Repeat the upscaling logic: ICON-D2 is higher quality than ECMWF
                for (int i = 0; i < 4; i++)
                {
                    var quarterTime = hourData.Time.AddMinutes(15 * i - 45);
                    if (blendedSeriesDict.ContainsKey(quarterTime))
                    {
                        // OVERWRITE: Higher fidelity hourly data
                        blendedSeriesDict[quarterTime] = UpdatMeteoParametersRecord(blendedSeriesDict[quarterTime], hourData);
                    }
                }
            }

            // Smooth after mid-term patching
            if (smoothingFilterId >= 0) blendedSeriesDict = MeteoSeriesSmoothing.SmoothBlendedPeriod(blendedSeriesDict, filterId: smoothingFilterId);

            // --- STEP 4: Patch with Short-Term High-Res (15-min Nearcast) ---
            // Overwrites all prior data for the first ~48 hours.

            foreach (var quarterData in nearCastSeriesList.Where(p => p.Temperature.HasValue))
            {
                var quarterTime = quarterData.Time;
                if (blendedSeriesDict.ContainsKey(quarterData.Time))
                {
                    // OVERWRITE: Highest fidelity, highest resolution data
                    blendedSeriesDict[quarterTime] = UpdatMeteoParametersRecord(blendedSeriesDict[quarterTime], quarterData);
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
            return blendedSeriesDict.Values
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
                RadiationVariance = newRecord.RadiationVariance ?? baseRecord.RadiationVariance
            };
        }
    }
}
