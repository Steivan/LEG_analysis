namespace LEG.E3Dc.Client
{
    /// <summary>
    /// Configuration for an E3DC system
    /// </summary>
    public class E3dcSystemConfig
    {
        public string SystemName { get; set; } = string.Empty;
        public string InstallationNumber { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public E3dcSystemConfig() { }

        public E3dcSystemConfig(string systemName, string installationNumber, string serialNumber, string apiKey)
        {
            SystemName = systemName;
            InstallationNumber = installationNumber;
            SerialNumber = serialNumber;
            ApiKey = apiKey;
        }
    }
}
