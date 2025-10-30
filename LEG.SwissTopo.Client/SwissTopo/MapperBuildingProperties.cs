using Newtonsoft.Json.Linq;
using System.Globalization;
using LEG.SwissTopo.Abstractions;

namespace LEG.SwissTopo.Client.SwissTopo
{
    public static class MapperBuildingProperties
    {

        public static RecordBuildingProperties? MapFromGeoAdminResponse(JToken feature)
        {
            var props = feature["properties"];
            if (props == null) return null;

            return new RecordBuildingProperties(
                gastw: props["gastw"]?.ToObject<int?>(),
                gkat: props["gkat"]?.ToObject<int?>(),
                gwaerdath1: ParseDate(props["gwaerdath1"]?.ToString()),
                deinr: props["deinr"]?.ToString(),
                gwaerzw2: props["gwaerzw2"]?.ToObject<int?>(),
                esid: props["esid"]?.ToObject<int?>(),
                strname: props["strname"]?.ToObject<List<string>>(),
                edid: props["edid"]?.ToObject<int?>(),
                wstat: props["wstat"]?.ToObject<List<int>>(),
                plz_plz6: props["plz_plz6"]?.ToString(),
                gksce: props["gksce"]?.ToObject<int?>(),
                wabbj: props["wabbj"]?.ToObject<List<int?>>(),
                ewid: props["ewid"]?.ToObject<List<string>>(),
                dexpdat: ParseDate(props["dexpdat"]?.ToString()),
                gwaerzh1: props["gwaerzh1"]?.ToObject<int?>(),
                wstwk: props["wstwk"]?.ToObject<List<int>>(),
                gschutzr: props["gschutzr"]?.ToObject<int?>(),
                gabbj: props["gabbj"]?.ToObject<int?>(),
                gwaerzh2: props["gwaerzh2"]?.ToObject<int?>(),
                stroffiziel: props["stroffiziel"]?.ToString(),
                doffadr: props["doffadr"]?.ToObject<int?>(),
                egid: props["egid"]?.ToString(),
                gwaerdatw1: ParseDate(props["gwaerdatw1"]?.ToString()),
                gvolnorm: props["gvolnorm"]?.ToObject<int?>(),
                dkodn: props["dkodn"]?.ToObject<double?>(),
                egrid: props["egrid"]?.ToString(),
                wmehrg: props["wmehrg"]?.ToObject<List<int>>(),
                gbaup: props["gbaup"]?.ToObject<int?>(),
                wkche: props["wkche"]?.ToObject<List<int>>(),
                gexpdat: ParseDate(props["gexpdat"]?.ToString()),
                gazzi: props["gazzi"]?.ToObject<int?>(),
                genh1: props["genh1"]?.ToObject<int?>(),
                gebf: props["gebf"]?.ToObject<int?>(),
                genw2: props["genw2"]?.ToObject<int?>(),
                dkode: props["dkode"]?.ToObject<double?>(),
                gbez: props["gbez"]?.ToString(),
                wbez: props["wbez"]?.ToObject<List<string>>(),
                gbauj: props["gbauj"]?.ToObject<int?>(),
                gwaerdatw2: ParseDate(props["gwaerdatw2"]?.ToString()),
                gwaerscew2: props["gwaerscew2"]?.ToObject<int?>(),
                genw1: props["genw1"]?.ToObject<int?>(),
                genh2: props["genh2"]?.ToObject<int?>(),
                gklas: props["gklas"]?.ToObject<int?>(),
                lparz: props["lparz"]?.ToString(),
                gstat: props["gstat"]?.ToObject<int?>(),
                weinr: props["weinr"]?.ToString(),
                gwaerscew1: props["gwaerscew1"]?.ToObject<int?>(),
                lgbkr: props["lgbkr"]?.ToObject<int?>(),
                gwaerdath2: ParseDate(props["gwaerdath2"]?.ToString()),
                lparzsx: props["lparzsx"]?.ToObject<long?>(),
                dplz4: props["dplz4"]?.ToObject<int?>(),
                whgnr: props["whgnr"]?.ToString(),
                gbaum: props["gbaum"]?.ToObject<int?>(),
                strindx: props["strindx"]?.ToObject<List<string>>(),
                gkode: props["gkode"]?.ToObject<double?>(),
                strnamk: props["strnamk"]?.ToObject<List<string>>(),
                strname_deinr: props["strname_deinr"]?.ToString(),
                ggdename: props["ggdename"]?.ToString(),
                ggdenr: props["ggdenr"]?.ToObject<int?>(),
                wbauj: props["wbauj"]?.ToObject<List<int>>(),
                gvolsce: props["gvolsce"]?.ToObject<int?>(),
                gdekt: props["gdekt"]?.ToString(),
                garea: props["garea"]?.ToObject<int?>(),
                gebnr: props["gebnr"]?.ToString(),
                gwaersceh2: props["gwaersceh2"]?.ToObject<int?>(),
                ganzwhg: props["ganzwhg"]?.ToObject<int?>(),
                gvol: props["gvol"]?.ToObject<int?>(),
                wexpdat: props["wexpdat"]?.ToObject<List<DateTime>>(),
                ltyp: props["ltyp"]?.ToObject<int?>(),
                strsp: props["strsp"]?.ToObject<List<string>>(),
                warea: props["warea"]?.ToObject<List<int>>(),
                dplzz: props["dplzz"]?.ToObject<int?>(),
                id: props["id"]?.ToString(),
                wazim: props["wazim"]?.ToObject<List<int>>(),
                gkodn: props["gkodn"]?.ToObject<double?>(),
                gwaersceh1: props["gwaersceh1"]?.ToObject<int?>(),
                dplzname: props["dplzname"]?.ToString(),
                gwaerzw1: props["gwaerzw1"]?.ToObject<int?>(),
                egaid: props["egaid"]?.ToObject<int?>()
            );
        }

         private static DateTime? ParseDate(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr) || dateStr == "-") return null;
            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) return dt;
            return null;
        }
    }
}