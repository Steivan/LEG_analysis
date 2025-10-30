using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace LEG.MeteoSwiss.Client.MeteoSwiss
{
    public static class MeteoSwissHelper
    {

        private static string[] _validGroundStations = [];

        public static string[] ValidGroundStations
        {
            get => _validGroundStations;
            set => _validGroundStations = value ?? [];
        }

        private static readonly string[] BaselineGroundStations = [ "AEG", "AIG", "ALT", "AND", "ANT", "ARH", "ARO", "ATT", "BAN", "PSI", "STC", "UEB", "KUE", "GIN", "SMA", "SCU", "ABO" ]; // Added ABO
        private static readonly string[] ValidTowerStations = [ "BAN", "PSI", "STC", "UEB" ];
        private static readonly string[] ValidGranularity = [ "t", "h", "d" ];


        public static string[] GetBaselineGroundStations() => BaselineGroundStations;

        /// <summary>
        /// Updates the list of valid ground stations at runtime.
        /// </summary>
        public static void UpdateValidGroundStations(IEnumerable<string> newStations)
        {
            ValidGroundStations = [.. newStations];
        }

        private static string GetCsvFilename(string fileBody, string stationId, string period, string granularity)
        {
            ValidateGranularity(granularity);
            period = period.ToLowerInvariant();
            string fileTail;
            if (new List<string> { "historical", "recent", "now" }.Contains(period))
            {
                fileTail = period;
            }
            else
            {
                period = NormalizeAndValidatePeriod(period);
                fileTail = $"historical_{period}";
            }
            var lowerStationId = stationId.ToLowerInvariant();
            return $"{fileBody}_{lowerStationId}_{granularity}_{fileTail}.csv";
        }

        public static (string fileName, string filePath) GetTowerCsvFilename(string stationId, string period, string granularity)
        {
            ValidateTowerStationId(stationId);
            var filename = GetCsvFilename("ogd-smn-tower", stationId, period, granularity);
            var filePath = MeteoSwissConstants.DataFolder + $"{stationId.ToUpper()}/" + filename;
            return (filename, filePath);
        }

        public static (string fileName, string filePath) GetGroundCsvFilename(string stationId, string period, string granularity)
        {
            ValidateGroundStationId(stationId);
            var filename = GetCsvFilename("ogd-smn", stationId, period, granularity);
            var filePath = MeteoSwissConstants.DataFolder + $"{stationId.ToLower()}/" + filename;
            return (filename, filePath);
        }

        public static void ValidateTowerStationId(string stationId)
        {
            var stationIdCopy = stationId.ToLowerInvariant();
            if (!ValidTowerStations.Contains(stationIdCopy, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Invalid stationId '{stationId}'. Valid options: {string.Join(", ", ValidTowerStations)}");
        }

        public static void ValidateGroundStationId(string stationId)
        {
            var stationIdCopy = stationId.ToLowerInvariant();
            if (!ValidGroundStations.Contains(stationIdCopy, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Invalid stationId '{stationId}'. Valid options: {string.Join(", ", ValidGroundStations)}");
        }

        public static void ValidateGranularity(string granularity)
        {
            if (!ValidGranularity.Contains(granularity.ToLowerInvariant()))
                throw new ArgumentException($"Invalid granularity '{granularity}'. Valid options: {string.Join(", ", ValidGranularity)}");
        }

        public static string NormalizeAndValidatePeriod(string period)
        {
            if (Regex.IsMatch(period, @"^\d{3}0-\d{3}9$"))
            {
                return period;
            }
            if (Regex.IsMatch(period, @"^\d{4}$"))
            {
                var year = int.Parse(period);
                var decadeStart = (year / 10) * 10;
                int decadeEnd = decadeStart + 9;
                return $"{decadeStart}-{decadeEnd}";
            }
            throw new ArgumentException($"Invalid period '{period}'. Format must be xyz0-xyz9 (e.g., 2010-2019) or a single year (e.g., 2012).");
        }
    }
}