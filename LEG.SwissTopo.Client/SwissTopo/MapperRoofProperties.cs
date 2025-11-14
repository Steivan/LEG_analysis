using Newtonsoft.Json.Linq;
using NetTopologySuite.Geometries;
using LEG.SwissTopo.Abstractions;

namespace LEG.SwissTopo.Client.SwissTopo
{
    public static class MapperRoofProperties
    {
        private static MultiPolygon? ParseGeometry(JToken? geometryToken)
        {
            if (geometryToken == null || geometryToken.Type == JTokenType.Null)
                return null;

            var type = geometryToken["type"]?.ToString();
            if (type == "MultiPolygon")
            {
                var coordinates = geometryToken["coordinates"];
                if (coordinates == null || coordinates.Type != JTokenType.Array)
                    return null;

                var polygons = new List<Polygon>();
                var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory();

                foreach (var polygonArray in coordinates)
                {
                    if (polygonArray is not JArray rings || rings.Count == 0)
                        continue;

                    // The first ring is the shell, the rest are holes
                    var shellCoords = rings[0]
                        .Select(coordinate => new Coordinate(
                            coordinate[0]?.ToObject<double>() ?? 0,
                            coordinate[1]?.ToObject<double>() ?? 0))
                        .ToArray();

                    var shell = geometryFactory.CreateLinearRing(shellCoords);

                    var holes = new List<LinearRing>();
                    for (int i = 1; i < rings.Count; i++)
                    {
                        var holeCoords = rings[i]
                            .Select(coordinate => new Coordinate(
                                coordinate[0]?.ToObject<double>() ?? 0,
                                coordinate[1]?.ToObject<double>() ?? 0))
                            .ToArray();
                        holes.Add(geometryFactory.CreateLinearRing(holeCoords));
                    }

                    polygons.Add(geometryFactory.CreatePolygon(shell, [.. holes]));
                }

                return geometryFactory.CreateMultiPolygon([.. polygons]);
            }

            // Add support for other geometry types as needed
            return null;
        }
        private static void SortMonthlyFieldsByMonate(
            ref int[]? monate, ref double[]? mstrahlungMonat, ref double[]? aParam, ref double[]? bParam, ref double[]? cParam,
            ref int[]? heizgradtage, ref double[]? mtempMonat, ref long[]? stromertragMonat)
        {
            if (monate == null || monate.Length == 0)
                return;

            // Copy monate to a local variable for use in lambdas
            var monateLocal = monate;

            var monthOrder = Enumerable.Range(1, 12).ToArray();
            var indexMap = monthOrder
                .Select(month => Array.IndexOf(monateLocal, month))
                .ToArray();

            T[]? SortArray<T>(T[]? arr)
            {
                if (arr == null || arr.Length != monateLocal.Length)
                    return arr;
                var sorted = new T[monthOrder.Length];
                for (int i = 0; i < monthOrder.Length; i++)
                {
                    int idx = indexMap[i];
                    sorted[i] = idx >= 0 ? arr[idx] : default!;
                }
                return sorted;
            }

            // Only assign if monate was not null
            monate = monate != null ? monthOrder : null;
            mstrahlungMonat = SortArray(mstrahlungMonat);
            aParam = SortArray(aParam);
            bParam = SortArray(bParam);
            cParam = SortArray(cParam);
            heizgradtage = SortArray(heizgradtage);
            mtempMonat = SortArray(mtempMonat);
            stromertragMonat = SortArray(stromertragMonat);
        }

        public static RecordRoofProperties? MapFromGeoAdminResponse(JToken feature)
        {
            var props = feature["properties"];
            if (props == null) return null;

            var monate = props["monate"]?.ToObject<int[]>();
            var mstrahlungMonat = props["mstrahlung_monat"]?.ToObject<double[]>();
            var aParam = props["a_param"]?.ToObject<double[]>();
            var bParam = props["b_param"]?.ToObject<double[]>();
            var cParam = props["c_param"]?.ToObject<double[]>();
            var heizgradtage = props["heizgradtage"]?.ToObject<int[]>();
            var mtempMonat = props["mtemp_monat"]?.ToObject<double[]>();
            var stromertragMonat = props["stromertrag_monat"]?.ToObject<long[]>();

            SortMonthlyFieldsByMonate(ref monate, ref mstrahlungMonat, ref aParam, ref bParam, ref cParam, ref heizgradtage, ref mtempMonat, ref stromertragMonat);

            return new RecordRoofProperties(
                props["objectid"]?.ToObject<int>() ?? 0,
                props["df_uid"]?.ToObject<long>() ?? 0,
                props["df_nummer"]?.ToObject<int>() ?? 0,
                ParseDate(props["datum_erstellung"]?.ToString()),
                ParseDate(props["datum_aenderung"]?.ToString()),
                Guid.TryParse(props["sb_uuid"]?.ToString(), out var guid) ? guid : Guid.Empty,
                props["sb_objektart"]?.ToObject<int>() ?? 0,
                ParseDate(props["sb_datum_erstellung"]?.ToString()),
                ParseDate(props["sb_datum_aenderung"]?.ToString()),
                props["klasse"]?.ToObject<int>() ?? 0,
                props["flaeche"]?.ToObject<double>() ?? 0,
                props["ausrichtung"]?.ToObject<int>() ?? 0,
                props["neigung"]?.ToObject<int>() ?? 0,
                props["mstrahlung"]?.ToObject<int>() ?? 0,
                props["gstrahlung"]?.ToObject<long>() ?? 0,
                props["stromertrag"]?.ToObject<long>() ?? 0,
                props["stromertrag_sommerhalbjahr"]?.ToObject<long>() ?? 0,
                props["stromertrag_winterhalbjahr"]?.ToObject<long>() ?? 0,
                props["waermeertrag"]?.ToObject<long>() ?? 0,
                props["duschgaenge"]?.ToObject<int>() ?? 0,
                props["dg_heizung"]?.ToObject<int>() ?? 0,
                props["dg_waermebedarf"]?.ToObject<int>() ?? 0,
                props["bedarf_warmwasser"]?.ToObject<long>() ?? 0,
                props["bedarf_heizung"]?.ToObject<long>() ?? 0,
                props["flaeche_kollektoren"]?.ToObject<double>() ?? 0,
                props["volumen_speicher"]?.ToObject<long>() ?? 0,
                props["gwr_egid"]?.Type == JTokenType.Null ? null : props["gwr_egid"]?.ToObject<long?>(),
                // Monthly arrays
                monate, mstrahlungMonat, aParam, bParam, cParam, heizgradtage, mtempMonat, stromertragMonat,
                // Geometry
                ParseGeometry(feature["geometry"]),
                props["shape_length"]?.ToObject<double>() ?? 0,
                props["shape_area"]?.ToObject<double>() ?? 0
            );
        }

        private static DateTime ParseDate(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr) || dateStr == "-") return DateTime.MinValue;
            if (DateTime.TryParse(dateStr, out var dt)) return dt;
            return DateTime.MinValue;
        }
    }
}