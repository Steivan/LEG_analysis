namespace LEG.E3Dc.Client
{
    /// <summary>
    /// Interface for downloading time series data from E3DC systems
    /// </summary>
    public interface IE3dcDownloader : IDisposable
    {
        /// <summary>
        /// Login to the E3DC portal
        /// </summary>
        /// <param name="username">Portal username</param>
        /// <param name="password">Portal password</param>
        /// <returns>True if login successful</returns>
        Task<bool> LoginAsync(string username, string password);

        /// <summary>
        /// Download time series data for a specific system
        /// </summary>
        /// <param name="systemConfig">System configuration</param>
        /// <param name="folderName">Output folder for CSV files</param>
        /// <param name="startDate">Start date for data download</param>
        /// <param name="endDate">End date for data download (optional, defaults to today)</param>
        /// <param name="timeResolution">Time resolution in minutes (default: 15)</param>
        /// <returns>Path to the downloaded CSV file</returns>
        Task<string> DownloadTimeSeriesAsync(
            E3dcSystemConfig systemConfig,
            string folderName,
            DateTime startDate,
            DateTime? endDate = null,
            int timeResolution = 15);

        /// <summary>
        /// Logout from the E3DC portal
        /// </summary>
        Task LogoutAsync();
    }
}
