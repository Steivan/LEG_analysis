using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using LEG.Common;
using LEG.E3Dc.Abstractions;

namespace LEG.E3Dc.Client
{
    public class E3DcLoadArrayRecords
    {
        public static List<E3DcPeriodArrayRecord> LoadE3DcArrayRecords(string folder, int startYear, int endYear)
        {
            var listOfArrayRecords = new List<E3DcPeriodArrayRecord>();

            var shortStartYear = startYear % 100;
            var shortEndYear = endYear % 100;
            for (var shortYear = shortStartYear; shortYear <= shortEndYear; shortYear++)
            {
                var arrayRecords = new E3DcPeriodArrayRecord();
                arrayRecords.InitArrayRecord(shortYear);

                for (var month = 1; month <= 12; month++)
                {
                    var filePath = folder + E3DcFileHelper.FileName(shortYear, month);
                    if (System.IO.File.Exists(filePath))
                    {
                        var records = ImportCsv.ImportFromFile<E3DcRecord>(filePath, ";");
                        foreach (var record in records) arrayRecords.LoadE3DcRecord(record);
                    }
                }
                listOfArrayRecords.Add(arrayRecords);
            }

            return listOfArrayRecords;
        }
    }
}