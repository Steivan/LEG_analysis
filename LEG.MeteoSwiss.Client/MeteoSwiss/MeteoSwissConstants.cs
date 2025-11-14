namespace LEG.MeteoSwiss.Client.MeteoSwiss
{
    public static class MeteoSwissConstants
    {
        private const string MeteoDataFolder = @"C:\code\LEG_analysis\Data\MeteoData\";
        // Folders
        public const string MeteoStationsDataFolder = MeteoDataFolder + @"Stations\";
        public static readonly string DataFolder = MeteoDataFolder + @"StationsData\";
        // Files
        public const string CsvExtension = ".csv";
        public static readonly string GroundStationsMetaFile = MeteoStationsDataFolder + @"ogd-smn_meta_stations.csv";
        public static readonly string TowerStationsMetaFile = MeteoStationsDataFolder + @"ogd-smn-tower_meta_stations.csv";
        public static readonly string LongestPerStationInfoFile = MeteoStationsDataFolder + @"longest-per_stationinfo_D.csv";
        public static readonly string StandardPerStationInfoFile = MeteoStationsDataFolder + @"standard-per_stationinfo_D.csv";
        public static readonly string OgdSmnTowerSamplePath = DataFolder + @"BAN\";
        // Add more constants as needed, for example:
        public const string OgdSmnTowerSampleFile = "ogd-smn-tower_ban_t_historical_2020-2029.csv";
        public const string TowerTemperatureFile = "ogd-smn-tower_ban_t_historical_2020-2029.csv";
        public const string TowerHumidityFile = "ogd-smn-tower_ban_h_historical_2020-2029.csv";
    }
}
