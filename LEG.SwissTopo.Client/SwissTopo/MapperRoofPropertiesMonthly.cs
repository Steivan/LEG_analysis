using Newtonsoft.Json.Linq;
using LEG.SwissTopo.Abstractions;

namespace LEG.SwissTopo.Client.SwissTopo
{
    public static class MapperRoofPropertiesMonthly
    {
        public static RecordRoofPropertiesMonthly? MapFromGeoAdminResponse(JToken feature)
        {
            var props = feature["properties"];
            if (props == null) return null;

            return new RecordRoofPropertiesMonthly(
                ObjectId: props["OBJECTID"]?.ToObject<int>() ?? 0,
                DfUid: props["DF_UID"]?.ToObject<long>() ?? 0,
                DfNummer: props["DF_NUMMER"]?.ToObject<short>() ?? 0,
                //SbUuid: props["SB_UUID"]?.ToObject<Guid>() ?? Guid.Empty,
                SbUuid: Guid.TryParse(props["SB_UUID"]?.ToString(), out var guid) ? guid : Guid.Empty,
                Monat: props["MONAT"]?.ToObject<short>() ?? 0,
                MstrahlungMonat: props["MSTRAHLUNG_MONAT"]?.ToObject<double>() ?? 0,
                AParam: props["A_PARAM"]?.ToObject<double>() ?? 0,
                BParam: props["B_PARAM"]?.ToObject<double>() ?? 0,
                CParam: props["C_PARAM"]?.ToObject<double>() ?? 0,
                Heizgradtage: props["HEIZGRADTAGE"]?.ToObject<short>() ?? 0,
                MtempMonat: props["MTEMP_MONAT"]?.ToObject<double>() ?? 0,
                StromertragMonat: props["STROMERTRAG_MONAT"]?.ToObject<long>() ?? 0
            );
        }
    }
}