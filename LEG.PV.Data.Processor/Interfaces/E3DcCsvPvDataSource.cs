using LEG.PV.Data.Processor.Abstractions;
using LEG.E3Dc.Client;
using static LEG.PV.Core.Models.PvDataClass;

namespace LEG.PV.Data.Processor.Interfaces
{
    public class E3DcCsvPvDataSource : IPvDataSource
    {
        public string SourceName => "E3DC CSV";

        /// <summary>
        /// Loads PV production records from E3DC CSV files for a given site/folder and time range.
        /// </summary>
        /// <param name="siteId">The site identifier (should be convertible to folder number).</param>
        /// <param name="start">Start of the time range (inclusive).</param>
        /// <param name="end">End of the time range (exclusive).</param>
        /// <returns>List of PV records.</returns>
        public async Task<IList<PvRecord>> LoadPvRecordsAsync(string siteId, DateTime start, DateTime end)
        {
            // E3DcLoadPeriodRecords expects a folder number, so parse siteId as int
            if (!int.TryParse(siteId, out int folderNumber))
                throw new ArgumentException("siteId must be a valid folder number for E3DC import.", nameof(siteId));

            // E3DcLoadPeriodRecords.LoadRecords returns List<E3DcRecord>
            // You may need to map E3DcRecord to PvRecord if they are not the same type
            return await Task.Run(() =>
            {
                var e3dcRecords = E3DcLoadPeriodRecords.LoadRecords(folderNumber, start, end);
                // If E3DcRecord is already a PvRecord, just cast:
                return e3dcRecords.Cast<PvRecord>().ToList();
                // If not, map each E3DcRecord to PvRecord here.
            });
        }
    }
}