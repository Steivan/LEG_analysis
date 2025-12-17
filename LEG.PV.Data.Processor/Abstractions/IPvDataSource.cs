using static LEG.PV.Core.Models.PvDataClass;

namespace LEG.PV.Data.Processor.Abstractions
{
    public interface IPvDataSource
    {
        /// <summary>
        /// A human-readable name for the data source (e.g., "E3DC CSV", "Simulation", "Excel Import").
        /// </summary>
        string SourceName { get; }

        /// <summary>
        /// Loads PV production records for a given site and time range.
        /// </summary>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="start">Start of the time range (inclusive).</param>
        /// <param name="end">End of the time range (exclusive).</param>
        /// <returns>List of PV records (at minimum, must include timestamp and produced power).</returns>
        Task<IList<PvRecord>> LoadPvRecordsAsync(string siteId, DateTime start, DateTime end);

        // Optionally: Add methods for extra data, metadata, or different record types as your needs evolve.
    }
}
