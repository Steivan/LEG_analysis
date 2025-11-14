using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        private static readonly string[] AG = ["BEZ", "BUS", "LEI", "MOE", "PSI"];
        private static readonly string[] AI = ["SAE"];
        private static readonly string[] AR = [];
        private static readonly string[] BE = ["ABO", "BAN", "BER", "BOL", "BRZ", "CHA", "COY", "FRU", "GRH", "INT", "KOP", "LAG", "MER", "MUB", "NAP", "THU", "WYN"];
        private static readonly string[] BL = ["BAS", "RUE"];
        private static readonly string[] BS = ["STC"];
        private static readonly string[] FL = ["VAD"];
        private static readonly string[] FR = ["GRA", "MAS", "MLS", "PLF"];
        private static readonly string[] GE = ["GVE"];
        private static readonly string[] GL = ["ELM", "GLA"];
        private static readonly string[] GR = ["AND", "ARO", "BEH", "BIV", "BUF", "CHU", "CMA", "COV", "DAV", "DIS", "GRO", "ILZ", "LAT", "NAS", "PMA", "ROB", "SAM", "SBE", "SCU", "SIA", "SMM", "SRS", "VAB", "VIO", "VLS", "WFJ"];
        private static readonly string[] JU = ["DEM", "FAH"];
        private static readonly string[] LU = ["EGO", "FLU", "LUZ", "MOA", "SPF"];
        private static readonly string[] NE = ["BRL", "CDF", "CHM", "CRM", "NEU"];
        private static readonly string[] NW = [];
        private static readonly string[] OW = ["ENG", "GIH", "PIL", "TIT"];
        private static readonly string[] SG = ["ARH", "EBK", "OBR", "QUI", "RAG", "SCM", "STG"];
        private static readonly string[] SH = ["HLL", "SHA"];
        private static readonly string[] SO = ["GOE", "GRE"];
        private static readonly string[] SZ = ["EIN", "GES", "LAC", "SAG"];
        private static readonly string[] TG = ["BIZ", "GUT", "HAI", "STK", "TAE"];
        private static readonly string[] TI = ["BIA", "CEV", "CIM", "COM", "GEN", "LUG", "MAG", "MTR", "OTL", "PIO", "ROE", "SBO"];
        private static readonly string[] UR = ["ALT", "ANT", "GOS", "GUE"];
        private static readonly string[] VD = ["AIG", "BIE", "CDM", "CGI", "CHB", "CHD", "DIA", "DOL", "FRE", "MAH", "ORO", "PAY", "PRE", "PUY", "VEV", "VIT"];
        private static readonly string[] VS = ["ATT", "BIN", "BLA", "BOU", "EGH", "EVI", "EVO", "GOR", "GRC", "GSB", "JUN", "MAR", "MOB", "MTE", "MVE", "SIM", "SIO", "ULR", "VIS", "ZER"];
        private static readonly string[] ZG = ["AEG", "CHZ"];
        private static readonly string[] ZH = ["HOE", "KLO", "LAE", "PFA", "REH", "SMA", "UEB", "WAE"];

        private static readonly string[] AllGroundStations = [
            ..AG, ..AI, ..BE, ..BL, ..BS, ..FL, ..FR, ..GE, ..GL, ..GR, ..JU, ..LU, ..NE, 
            ..OW, ..SG, ..SH, ..SO, ..SZ, ..TG, ..TI, ..UR, ..VD, ..VS, ..ZG, ..ZH
            ];

        private static readonly string[] BaselineGroundStations = [ "AEG", "AIG", "ALT", "AND", "ANT", "ARH", "ARO", "ATT", "BAN", "PSI", "STC", "UEB", "KUE", "GIN", "SMA", "SCU", "ABO" ]; // Added ABO
        private static readonly string[] ValidTowerStations = [ "BAN", "PSI", "STC", "UEB" ];
        private static readonly string[] ValidGranularity = [ "t", "h", "d" ];


        public static string[] GetAllGroundStations() => AllGroundStations;
        public static string[] GetCantoGroundStations(string canton)
        { 
            switch (canton.ToUpperInvariant())
            {
                case "CH": return AllGroundStations;
                case "AG": return AG;
                case "AI": return AI;
                case "AR": return AR;
                case "BE": return BE;
                case "BL": return BL;
                case "BS": return BS;
                case "FL": return FL;
                case "FR": return FR;
                case "GE": return GE;
                case "GL": return GL;
                case "GR": return GR;
                case "JU": return JU;
                case "LU": return LU;
                case "NE": return NE;
                case "NW": return NW;
                case "OW": return OW;
                case "SG": return SG;
                case "SH": return SH;
                case "SO": return SO;
                case "SZ": return SZ;
                case "TG": return TG;
                case "TI": return TI;
                case "UR": return UR;
                case "VD": return VD;
                case "VS": return VS;
                case "ZG": return ZG;
                case "ZH": return ZH;
                default:
                    throw new ArgumentException($"Invalid canton '{canton}'.");
            }
        }
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