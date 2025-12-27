using LEG.PvImport.Abstractions;

namespace LEG.PvImport.Clients.Fronius.Client
{
    public class FroniusLoadPeriodRecords
    {
        public static List<IPowerRecord> LoadPowerRecords(DateTime? startTime = null, DateTime? endTime = null, bool shiftToUtc = false, int minutesShift = 0)
        {
            var hasStartTime = startTime.HasValue;
            var hasEndTime = endTime.HasValue;
            var interval = FroniusFileHelper.Interval;
            var minutesPerPeriod = (int)interval.TotalMinutes;
            var hoursPerPeriod = (double)interval.TotalHours;
            var cetZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var utcToSeriesTime = shiftToUtc ? TimeSpan.Zero : TimeZoneInfo.Local.GetUtcOffset(DateTime.Now); // Shift from UTC to local without DST

            var (firstYear, lastYear) = FroniusFileHelper.GetYears;
            DateTime startTimeValue = hasStartTime ? startTime.Value : new DateTime(firstYear, 1, 1, 0, 0, 0, shiftToUtc ? DateTimeKind.Utc : DateTimeKind.Local);
            DateTime endTimeValue = hasEndTime ? endTime.Value : new DateTime(lastYear + 1, 1, 1, 0, 0, 0, shiftToUtc ? DateTimeKind.Utc : DateTimeKind.Local) - interval;

            startTimeValue = SyncedTimestamp(startTimeValue, minutesPerPeriod);
            endTimeValue = SyncedTimestamp(endTimeValue, minutesPerPeriod);

            var firstTimeStamp = endTimeValue;
            var lastTimeStamp = startTimeValue;
            var periodDictionary = new Dictionary<DateTime, double>();
            for (var year = firstYear; year <= lastYear; year++)
            {
                var records = FromiusLoadRecords.ImportFroniusRecords(FroniusFileHelper.GetFilePath(year));
                foreach (var record in records)
                {
                    var shiftedTimestamp = record.Timestamp.AddMinutes(minutesShift);
                    var localDstTime = SyncedTimestamp(shiftedTimestamp, minutesPerPeriod);
                    var recordTime = TimeZoneInfo.ConvertTimeToUtc(localDstTime, cetZone) + utcToSeriesTime;
                    if (recordTime < startTimeValue || recordTime > endTimeValue)
                    {
                        continue;
                    }
                    firstTimeStamp = firstTimeStamp < recordTime ? firstTimeStamp : recordTime;
                    lastTimeStamp = lastTimeStamp > recordTime ? lastTimeStamp : recordTime;
                    periodDictionary[recordTime] = record.SolarProduction;
                }
            }

            startTimeValue = hasStartTime ? startTimeValue : firstTimeStamp;
            endTimeValue = hasEndTime ? endTimeValue : lastTimeStamp;

            var periodRecordsList = new List<IPowerRecord>();
            for (var time = startTimeValue; time <= endTimeValue; time += interval)
            {
                var solarProduction = periodDictionary.ContainsKey(time) ? periodDictionary[time] * hoursPerPeriod : 0.0;   // Convert from intensity [W] to power [Wh]
                periodRecordsList.Add(new IPowerRecord { Timestamp = time, SolarProduction = solarProduction }); 
            }

            return periodRecordsList;
        }

        private static DateTime SyncedTimestamp(DateTime inputTime, int minutesPerPeriod)
        {
            var syncedMinutes = (inputTime.Minute / minutesPerPeriod) * minutesPerPeriod;
            return new DateTime(inputTime.Year, inputTime.Month, inputTime.Day, inputTime.Hour, syncedMinutes, 0);
        }

    }
}
