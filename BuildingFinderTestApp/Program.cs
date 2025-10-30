using System;
using System.Linq;
using System.Threading.Tasks;
using LEG.SwissTopo.Abstractions;
using LEG.SwissTopo.Client.SwissTopo;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Globalization;
using NetTopologySuite.Geometries;

namespace BuildingFinderTestApp
{
    class Program
    {
        static async Task Main()
        {
            // Create instances of the finder classes
            var buildingFinder = new BuildingFinder();
            var roofFinder = new RoofFinder();

            Console.WriteLine("SwissTopo Building and PvRoof Finder (Interactive)");
            Console.WriteLine("------------------------------------------------");
            Console.Write("Enter street name (e.g., Guldenenstrasse): ");
            string street = Console.ReadLine()?.Trim() ?? "";
            street = string.IsNullOrEmpty(street) ? "Guldenenstrasse" : street;
            Console.Write("Enter ZipNumber code (e.g., 8127): ");
            string zip = Console.ReadLine()?.Trim() ?? "";
            zip = string.IsNullOrEmpty(zip) ? "8127" : zip;

            if (string.IsNullOrEmpty(street) || string.IsNullOrEmpty(zip))
            {
                Console.WriteLine("Error: Street and ZipNumber code are required.");
                return;
            }

            // Prompt for house number prefix
            Console.Write("Enter house number prefix (e.g., 1, optional - press Enter for all): ");
            string houseNumberPrefix = Console.ReadLine()?.Trim() ?? "";

            // Step 1: Search for all buildings with given street/zip and optional house number prefix
            Console.WriteLine("\nFetching buildings...");
            var buildings = await buildingFinder.GetBuildingsByStreetZipAsync(street, zip, houseNumberPrefix);

            Console.WriteLine($"\nFound {buildings.Count} building(s) for {street}, {zip}:");
            if (buildings.Count == 0)
            {
                Console.WriteLine("No buildings found.");
                return;
            }

            // List all (filtered) matching buildings
            for (int i = 0; i < buildings.Count; i++)
            {
                var building = buildings[i];
                Console.WriteLine($"  [{i + 1}] {building.Address} (egid: {building.EGID}, egrid: {building.Egrid}, gebnr: {building.GebNr}, lparz: {building.Lparz}, garea: {building.GArea:F2} m²)");
            }

            // Step 2: Select one building from this list
            Console.Write($"\nSelect a building number (1-{buildings.Count}): ");
            if (!int.TryParse(Console.ReadLine()?.Trim(), out int selectedIndex) || selectedIndex < 1 || selectedIndex > buildings.Count)
            {
                Console.WriteLine("Error: Invalid selection.");
                return;
            }

            var selectedBuilding = buildings[selectedIndex - 1];
            Console.WriteLine($"\nSelected: {selectedBuilding.Address} (egid: {selectedBuilding.EGID})");
            Console.WriteLine($"  Centroid: ({selectedBuilding.X}, {selectedBuilding.Y})");
            Console.WriteLine($"  Building Area: {selectedBuilding.GArea:F2} m²");

            // Print selected building attributes
            Console.WriteLine("\n  Building Attributes:");
            Console.WriteLine("    Property".PadRight(40) + "Value".PadLeft(30));
            Console.WriteLine("    " + new string('-', 70));

            // Display key building properties: See https://www.housing-stat.ch/de/help/42.html
            var buildingLabels = new[]
            {
                "deinr",
                "dkode",
                "dkodn",
                "doffadr",
                "dplz4",
                "dplzname",
                "egaid",
                "egid",
                "egrid",
                "esid",
                "ewid",
                "ganzwhg",
                "garea",
                "gastw",
                "gbauj",
                "gbaum",
                "gbaup",
                "gdekt",
                "gebnr",
                "genh1",
                "gexpdat",
                "ggdename",
                "ggdenr",
                "gkat",
                "gklas",
                "gkode",
                "gkodn",
                "gksce",
                "gstat",
                "label",
                "lparz",
                "strname_deinr",
                "warea",
                "wazim",
                "wbauj",
                "wkche",
                "wmehrg"
            };
            foreach (var label in buildingLabels)
            {
                var formattedKey = label.PadRight(40);
                var value = selectedBuilding.Properties.TryGetValue(label, out var propValue) ? FormatPropertyValue(propValue) : "N/A";
                Console.WriteLine($"    {formattedKey}{value,30}");
            }

            // Preparation (MADD and detailed building properties)
            var buildingData = await buildingFinder.GetBuildingDataFromMaddAsync(selectedBuilding.EGID ?? "");
            var buildingEgId = buildingData.egid;
            var (X, Y) = buildingData.coordinates;
            var buildingArea = buildingData.buildingarea;
            var maddProperties = buildingData.maddProperties as RecordMaddBuildingProperties;
            var detailedProperties = buildingData.detailedProperties as RecordBuildingProperties;

            // Step 1 (building polygon)
            Console.WriteLine("\n  Fetching building perimeter...");
            // CS8604: Ensure EGID is not null
            var polygonData = await buildingFinder.GetBuildingPolygonAsync(selectedBuilding.EGID ?? string.Empty, selectedBuilding.X, selectedBuilding.Y, selectedBuilding.GArea);
            var buildingPolygon = polygonData.buildingPolygon as Geometry;
            var source = polygonData.source;
            Console.WriteLine($"  Building polygon type: {source} (Area: {buildingPolygon?.Area:F1} m²)");

            // Step 3 & 4: Use a sufficiently wide bbox for searching all potentially matching roofs and filter by EGID of Egrid_list
            Console.WriteLine("  Step 3: Fetching all potential roofs with a wide bbox...");
            var roofs = await roofFinder.GetOverlappingRoofsAsync(buildingPolygon, selectedBuilding, buildings);
            Console.WriteLine($"  Found {roofs.Count} matching roof(s) for the selected building:");

            if (roofs.Count == 0)
            {
                Console.WriteLine("No roofs found.");
                return;
            }

            // Pre-fetch attributes for all roofs to populate the table
            var allAttrs = new Dictionary<int, Dictionary<string, object?>>();
            for (int i = 0; i < roofs.Count; i++)
            {
                var roof = roofs[i];
                allAttrs[i] = await roofFinder.GetRoofExtendedAttributesAsync(roof.FeatureId ?? string.Empty, selectedBuilding.X, selectedBuilding.Y);
            }

            // Display table with key properties for all roofs
            Console.WriteLine("\nRoof Properties Table:");
            Console.WriteLine("Label".PadRight(43) + string.Join("", roofs.Select((r, i) => $"PvRoof {i + 1}".PadRight(20))));
            Console.WriteLine(new string('-', 43 + roofs.Count * 20));
            var labels = new[]
            {
                "egid",
                "egrid",
                "building_id",
                "ID",
                "label",
                "Area [m²]",
                "Orientation [°]",
                "Slope [°]",
                "Suitability",
                "bedarf_heizung [kWh/year]",
                "bedarf_warmwasser [kWh/year]",
                "datum_aenderung",
                "datum_erstellung",
                "dg_heizung [%]",
                "dg_waermebedarf [%]",
                "duschgaenge",
                "flaeche_kollektoren [m²]",
                "gstrahlung [kWh/year]",
                "mstrahlung [kW/m²/year]",
                "sb_datum_aenderung",
                "sb_datum_erstellung",
                "sb_objektart",
                "volumen_speicher [kWh]",
                "waermeertrag [kWh/year]",
                "klasse",
                "finanzertrag [CHF/year]",
                "stromertrag [kWh/year]",
                "stromertrag_winterhalbjahr [kWh/halfyear]",
                "stromertrag_sommerhalbjahr [kWh/halfyear]",
                "gs_serie_start",
                "df_nummer",
                "shape_length [m]",
                "shape_area [m²]"
            };
foreach (var label in labels)
{
    var formattedLabel = label.PadRight(43);
    var values = new string[roofs.Count];
    for (var i = 0; i < roofs.Count; i++)
    {
        var attrs = allAttrs[i];
                    // CS8600: Defensive null handling for all value conversions
                    string? value = label switch
                    {
                        "egid" => selectedBuilding.EGID ?? "N/A",
                        "egrid" => selectedBuilding.Egrid ?? "N/A",
                        "building_id" => attrs.TryGetValue("building_id", out var bid) ? bid?.ToString() ?? "N/A" : "N/A",
                        "ID" => roofs[i].FeatureId,
                        "label" => attrs.TryGetValue("label", out var lbl) ? lbl?.ToString() ?? "N/A" : "N/A",
                        "Area [m²]" => roofs[i].AreaM2.ToString("N2"),
                        "Orientation [°]" => roofs[i].OrientationDeg.ToString("F2"),
                        "Slope [°]" => roofs[i].SlopeDeg.ToString("F2") + "°",
                        "Suitability" => attrs.TryGetValue("klasse_text", out var ktxt) ? ktxt?.ToString() ?? "N/A"
                            : attrs.TryGetValue("eignung", out var eignung) ? eignung?.ToString() ?? "N/A" : "N/A",
                        "bedarf_heizung [kWh/year]" => attrs.TryGetValue("bedarf_heizung", out var bh) && int.TryParse(bh?.ToString(), out var bhVal) ? bhVal.ToString("N0") : "N/A",
                        "bedarf_warmwasser [kWh/year]" => attrs.TryGetValue("bedarf_warmwasser", out var bw) && int.TryParse(bw?.ToString(), out var bwVal) ? bwVal.ToString("N0") : "N/A",
                        "datum_aenderung" => attrs.TryGetValue("datum_aenderung", out var da) && DateTime.TryParse(da?.ToString(), out var daVal) ? daVal.ToString("dd/MM/yyyy") : "N/A",
                        "datum_erstellung" => attrs.TryGetValue("datum_erstellung", out var de) && DateTime.TryParse(de?.ToString(), out var deVal) ? deVal.ToString("dd/MM/yyyy") : "N/A",
                        "dg_heizung [%]" => attrs.TryGetValue("dg_heizung", out var dgh) ? dgh?.ToString() ?? "N/A" : "N/A",
                        "dg_waermebedarf [%]" => attrs.TryGetValue("dg_waermebedarf", out var dgw) ? dgw?.ToString() ?? "N/A" : "N/A",
                        "duschgaenge" => attrs.TryGetValue("duschgaenge", out var dg) ? dg?.ToString() ?? "N/A" : "N/A",
                        "flaeche_kollektoren [m²]" => attrs.TryGetValue("flaeche_kollektoren", out var fk) && double.TryParse(fk?.ToString(), out var fkVal) ? fkVal.ToString("N2") : "N/A",
                        "gstrahlung [kWh/year]" => attrs.TryGetValue("gstrahlung", out var gs) && int.TryParse(gs?.ToString(), out var gsVal) ? gsVal.ToString("N0") : "N/A",
                        "mstrahlung [kW/m²/year]" => attrs.TryGetValue("mstrahlung", out var ms) && int.TryParse(ms?.ToString(), out var msVal) ? msVal.ToString("N0") : "N/A",
                        "sb_datum_aenderung" => attrs.TryGetValue("sb_datum_aenderung", out var sba) && DateTime.TryParse(sba?.ToString(), out var sbaVal) ? sbaVal.ToString("dd/MM/yyyy") : "N/A",
                        "sb_datum_erstellung" => attrs.TryGetValue("sb_datum_erstellung", out var sbe) && DateTime.TryParse(sbe?.ToString(), out var sbeVal) ? sbeVal.ToString("dd/MM/yyyy") : "N/A",
                        "sb_objektart" => attrs.TryGetValue("sb_objektart", out var sbo) ? sbo?.ToString() ?? "N/A" : "N/A",
                        "volumen_speicher [kWh]" => attrs.TryGetValue("volumen_speicher", out var vs) && int.TryParse(vs?.ToString(), out var vsVal) ? vsVal.ToString("N0") : "N/A",
                        "waermeertrag [kWh/year]" => attrs.TryGetValue("waermeertrag", out var wm) && int.TryParse(wm?.ToString(), out var wmVal) ? wmVal.ToString("N0") : "N/A",
                        "klasse" => attrs.TryGetValue("klasse", out var kl) ? kl?.ToString() ?? "N/A" : "N/A",
                        "finanzertrag [CHF/year]" => attrs.TryGetValue("finanzertrag", out var fe) && double.TryParse(fe?.ToString(), out var feVal) ? feVal.ToString("N1") : "N/A",
                        "stromertrag [kWh/year]" => attrs.TryGetValue("stromertrag", out var se) && int.TryParse(se?.ToString(), out var seVal) ? seVal.ToString("N0") : "N/A",
                        "stromertrag_winterhalbjahr [kWh/halfyear]" => attrs.TryGetValue("stromertrag_winterhalbjahr", out var sew) && int.TryParse(sew?.ToString(), out var sewVal) ? sewVal.ToString("N0") : "N/A",
                        "stromertrag_sommerhalbjahr [kWh/halfyear]" => attrs.TryGetValue("stromertrag_sommerhalbjahr", out var ses) && int.TryParse(ses?.ToString(), out var sesVal) ? sesVal.ToString("N0") : "N/A",
                        "gs_serie_start" => attrs.TryGetValue("gs_serie_start", out var gss) && DateTime.TryParse(gss?.ToString(), out var gssVal) ? gssVal.ToString("dd/MM/yyyy") : "N/A",
                        "df_nummer" => attrs.TryGetValue("df_nummer", out var dfn) ? dfn?.ToString() ?? "N/A" : "N/A",
                        "shape_length [m]" => attrs.TryGetValue("shape_length", out var sl) && double.TryParse(sl?.ToString(), out var slVal) ? slVal.ToString("N2") : "N/A",
                        "shape_area [m²]" => attrs.TryGetValue("shape_area", out var sa) && double.TryParse(sa?.ToString(), out var saVal) ? saVal.ToString("N2") : "N/A",
                        _ => "N/A"
                    };
                    values[i] = (value ?? "N/A").PadRight(20);
    }
    Console.WriteLine(formattedLabel + string.Join("", values));
            }

            // Step 6 & 7: Loop to select and fetch extended attributes for multiple roofs
            while (true)
            {
                Console.Write($"\nSelect a roof number (1-{roofs.Count}, or press Enter to exit): ");
                // CS8600: Make input nullable and handle accordingly
                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    break;
                }
                if (!int.TryParse(input, out int selectedRoofIndex) || selectedRoofIndex < 1 || selectedRoofIndex > roofs.Count)
                {
                    Console.WriteLine("Error: Invalid selection.");
                    continue;
                }

                var selectedRoof = roofs[selectedRoofIndex - 1];
                var attrs = allAttrs[selectedRoofIndex - 1];
                Console.WriteLine($"\nSelected PvRoof: {selectedRoof.FeatureId} (Area: {selectedRoof.AreaM2:N2} m²)");
                Console.WriteLine("\n  Detailed PvRoof Information:");

                // Display scalar fields in a two-column table
                Console.WriteLine($"    Property".PadRight(43) + "Value".PadLeft(27));
                Console.WriteLine("    " + new string('-', 70));
                foreach (var label in labels)
                {
                    string formattedLabel = label.PadRight(43);
                    // CS8600: Defensive null handling for all value conversions
                    string? value = label switch
                    {
                        "egid" => selectedBuilding.EGID ?? "N/A",
                        "egrid" => selectedBuilding.Egrid ?? "N/A",
                        "building_id" => attrs.TryGetValue("building_id", out var bid) ? bid?.ToString() ?? "N/A" : "N/A",
                        "ID" => selectedRoof.FeatureId,
                        "label" => attrs.TryGetValue("label", out var lbl) ? lbl?.ToString() ?? "N/A" : "N/A",
                        "Area [m²]" => selectedRoof.AreaM2.ToString("N2"),
                        "Orientation [°]" => selectedRoof.OrientationDeg.ToString("F2"),
                        "Slope [°]" => selectedRoof.SlopeDeg.ToString("F2") + "°",
                        "Suitability" => attrs.TryGetValue("klasse_text", out var ktxt) ? ktxt?.ToString() ?? "N/A"
                            : attrs.TryGetValue("eignung", out var eignung) ? eignung?.ToString() ?? "N/A" : "N/A",
                        "bedarf_heizung [kWh/year]" => attrs.TryGetValue("bedarf_heizung", out var bh) && int.TryParse(bh?.ToString(), out var bhVal) ? bhVal.ToString("N0") : "N/A",
                        "bedarf_warmwasser [kWh/year]" => attrs.TryGetValue("bedarf_warmwasser", out var bw) && int.TryParse(bw?.ToString(), out var bwVal) ? bwVal.ToString("N0") : "N/A",
                        "datum_aenderung" => attrs.TryGetValue("datum_aenderung", out var da) && DateTime.TryParse(da?.ToString(), out var daVal) ? daVal.ToString("dd/MM/yyyy") : "N/A",
                        "datum_erstellung" => attrs.TryGetValue("datum_erstellung", out var de) && DateTime.TryParse(de?.ToString(), out var deVal) ? deVal.ToString("dd/MM/yyyy") : "N/A",
                        "dg_heizung [%]" => attrs.TryGetValue("dg_heizung", out var dgh) ? dgh?.ToString() ?? "N/A" : "N/A",
                        "dg_waermebedarf [%]" => attrs.TryGetValue("dg_waermebedarf", out var dgw) ? dgw?.ToString() ?? "N/A" : "N/A",
                        "duschgaenge" => attrs.TryGetValue("duschgaenge", out var dg) ? dg?.ToString() ?? "N/A" : "N/A",
                        "flaeche_kollektoren [m²]" => attrs.TryGetValue("flaeche_kollektoren", out var fk) && double.TryParse(fk?.ToString(), out var fkVal) ? fkVal.ToString("N2") : "N/A",
                        "gstrahlung [kWh/year]" => attrs.TryGetValue("gstrahlung", out var gs) && int.TryParse(gs?.ToString(), out var gsVal) ? gsVal.ToString("N0") : "N/A",
                        "mstrahlung [kW/m²/year]" => attrs.TryGetValue("mstrahlung", out var ms) && int.TryParse(ms?.ToString(), out var msVal) ? msVal.ToString("N0") : "N/A",
                        "sb_datum_aenderung" => attrs.TryGetValue("sb_datum_aenderung", out var sba) && DateTime.TryParse(sba?.ToString(), out var sbaVal) ? sbaVal.ToString("dd/MM/yyyy") : "N/A",
                        "sb_datum_erstellung" => attrs.TryGetValue("sb_datum_erstellung", out var sbe) && DateTime.TryParse(sbe?.ToString(), out var sbeVal) ? sbeVal.ToString("dd/MM/yyyy") : "N/A",
                        "sb_objektart" => attrs.TryGetValue("sb_objektart", out var sbo) ? sbo?.ToString() ?? "N/A" : "N/A",
                        "volumen_speicher [kWh]" => attrs.TryGetValue("volumen_speicher", out var vs) && int.TryParse(vs?.ToString(), out var vsVal) ? vsVal.ToString("N0") : "N/A",
                        "waermeertrag [kWh/year]" => attrs.TryGetValue("waermeertrag", out var wm) && int.TryParse(wm?.ToString(), out var wmVal) ? wmVal.ToString("N0") : "N/A",
                        "klasse" => attrs.TryGetValue("klasse", out var kl) ? kl?.ToString() ?? "N/A" : "N/A",
                        "finanzertrag [CHF/year]" => attrs.TryGetValue("finanzertrag", out var fe) && double.TryParse(fe?.ToString(), out var feVal) ? feVal.ToString("N1") : "N/A",
                        "stromertrag [kWh/year]" => attrs.TryGetValue("stromertrag", out var se) && int.TryParse(se?.ToString(), out var seVal) ? seVal.ToString("N0") : "N/A",
                        "stromertrag_winterhalbjahr [kWh/halfyear]" => attrs.TryGetValue("stromertrag_winterhalbjahr", out var sew) && int.TryParse(sew?.ToString(), out var sewVal) ? sewVal.ToString("N0") : "N/A",
                        "stromertrag_sommerhalbjahr [kWh/halfyear]" => attrs.TryGetValue("stromertrag_sommerhalbjahr", out var ses) && int.TryParse(ses?.ToString(), out var sesVal) ? sesVal.ToString("N0") : "N/A",
                        "gs_serie_start" => attrs.TryGetValue("gs_serie_start", out var gss) && DateTime.TryParse(gss?.ToString(), out var gssVal) ? gssVal.ToString("dd/MM/yyyy") : "N/A",
                        "df_nummer" => attrs.TryGetValue("df_nummer", out var dfn) ? dfn?.ToString() ?? "N/A" : "N/A",
                        "shape_length [m]" => attrs.TryGetValue("shape_length", out var sl) && double.TryParse(sl?.ToString(), out var slVal) ? slVal.ToString("N2") : "N/A",
                        "shape_area [m²]" => attrs.TryGetValue("shape_area", out var sa) && double.TryParse(sa?.ToString(), out var saVal) ? saVal.ToString("N2") : "N/A",
                        _ => "N/A"
                    };
                    Console.WriteLine($"    {formattedLabel}{value?.PadLeft(27) ?? "N/A".PadLeft(27)}");
                }

                // Add monthly data table
                Console.WriteLine("\n  Monthly Data Table:");
                PrintMonthlyDataTable(attrs);
                // Add polygon coordinates table
                Console.WriteLine("\n  Polygon Coordinates Table:");
                PrintPolygonCoordinatesTable(attrs, selectedBuilding);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadLine();
        }

        // Helper method to format property values for display
        private static string FormatPropertyValue(object? value)
        {
            if (value == null) return "N/A";
            if (value is List<object> list)
            {
                if (list.Count == 0) return "N/A";
                int maxItems = 5; // Limit to 5 items
                var displayItems = list.Take(maxItems).Select(item => item switch
                {
                    double d => d.ToString("F4", CultureInfo.InvariantCulture),
                    _ => item?.ToString() ?? "N/A"
                });
                string result = $"[{string.Join(", ", displayItems)}";
                if (list.Count > maxItems) result += ", ...]";
                else result += "]";
                return result;
            }
            if (value is double d) return d.ToString("F2", CultureInfo.InvariantCulture);
            if (value is int i) return i.ToString("N0", CultureInfo.InvariantCulture);
            if (DateTime.TryParse(value.ToString(), out DateTime date)) return date.ToString("dd/MM/yyyy");
            return value.ToString() ?? "N/A";
        }

        // Helper method to format lists for display (kept for potential future use)
        private static string FormatList(List<object>? list)
        {
            if (list == null || list.Count == 0) return "N/A";
            int maxItems = 5; // Limit to 5 items to avoid clutter
            var displayItems = list.Take(maxItems).Select(item => item switch
            {
                double d => d.ToString("F4", CultureInfo.InvariantCulture),
                _ => item.ToString()
            });
            string result = $"[{string.Join("", displayItems)}";
            if (list.Count > maxItems) result += ", ...]";
            else result += "]";
            return result;
        }

        // Helper method to format geometry for display (kept for potential future use)
        private static string FormatGeometry(string? geometry)
        {
            if (string.IsNullOrEmpty(geometry)) return "N/A";
            int maxLength = 100;
            if (geometry.Length > maxLength)
            {
                return string.Concat(geometry.AsSpan(0, maxLength), "...");
            }
            return geometry;
        }

        // Helper method to print monthly data table
        private static void PrintMonthlyDataTable(Dictionary<string, object?> attrs)
        {
            if (!(attrs.TryGetValue("monate", out var monateObj) && monateObj is List<object> monateList))
            {
                Console.WriteLine("    No monthly data available.");
                return;
            }

            var fields = new[]
            {
                ("a_param", "a_param"),
                ("b_param", "b_param"),
                ("c_param", "c_param"),
                ("heizgradtage", "Heizgradtage [°Kdays]"),
                ("monats_ertrag", "Monats Ertrag [kW/m²/month]")
            };

            // Initialize data for all 12 months (1-12)
            var monthlyData = new Dictionary<string, double?[]>();
            foreach (var (key, _) in fields)
            {
                monthlyData[key] = new double?[12]; // 12 months, null by default
            }

            // Map values to calendar months
            for (int i = 0; i < monateList.Count && i < 12; i++)
            {
                if (int.TryParse(monateList[i]?.ToString(), out int month) && month >= 1 && month <= 12)
                {
                    foreach (var (key, _) in fields)
                    {
                        if (attrs.TryGetValue(key, out var listObj) &&
                            listObj is List<object> values &&
                            i < values.Count &&
                            double.TryParse(values[i]?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                        {
                            monthlyData[key][month - 1] = value;
                        }
                    }
                }
            }

            // Print table with month names and right-aligned values
            string[] monthNames = [ "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" ];
            Console.WriteLine("    " + "Parameter".PadRight(28) + string.Join("", monthNames.Select(m => m.PadLeft(10))));
            Console.WriteLine("    " + new string('-', 28 + 12 * 10));
            foreach (var (key, displayName) in fields)
            {
                var row = monthlyData[key].Select(v => v.HasValue ? v.Value.ToString("F2", CultureInfo.InvariantCulture).PadLeft(10) : "N/A".PadLeft(10));
                Console.WriteLine($"    {displayName,-28}{string.Join("", row)}");
            }
        }

        // Helper method to print polygon coordinates table with relative coordinates
        private static void PrintPolygonCoordinatesTable(Dictionary<string, object?> attrs, BuildingInfo selectedBuilding)
        {
            if (!attrs.TryGetValue("geometry", out var geometryObj) || string.IsNullOrEmpty(geometryObj?.ToString()))
            {
                Console.WriteLine("    No geometry data available.");
                return;
            }

            try
            {
                // Get origin coordinates (gkode, gkodn)
 
                var originX = selectedBuilding.Properties.TryGetValue("gkode", out var gkodeObj) &&
                                 double.TryParse(gkodeObj?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var gx)
                    ? gx
                    : selectedBuilding.X;

                var originY = selectedBuilding.Properties.TryGetValue("gkodn", out var gkodnObj) &&
                                 double.TryParse(gkodnObj?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var gy)
                    ? gy
                    : selectedBuilding.Y;                // CS8604: Defensive null check before parsing
                var geometryStr = attrs["geometry"]?.ToString();
                if (string.IsNullOrEmpty(geometryStr))
                {
                    Console.WriteLine("    No geometry data available.");
                    return;
                }
                var geometry = JObject.Parse(geometryStr);
                var coordinates = geometry["coordinates"]?.ToObject<List<List<List<List<double>>>>>();
                if (coordinates == null || coordinates.Count == 0)
                {
                    Console.WriteLine("    No polygon coordinates available.");
                    return;
                }

                // Assume first polygon (MultiPolygon -> first Polygon -> first ring)
                var points = coordinates[0][0];
                int pointCount = points.Count;
                int maxPoints = Math.Min(pointCount, 20);

                Console.WriteLine($"    Origin (gkode, gkodn): ({originX:F1}, {originY:F1})");
                Console.WriteLine($"    Polygon Points ({pointCount} points):");
                Console.WriteLine("    " + "Coordinate".PadRight(20) + string.Join("", Enumerable.Range(1, maxPoints).Select(i => $"Point {i}".PadLeft(12))));
                Console.WriteLine("    " + new string('-', 20 + maxPoints * 12));

                // X coordinates (relative to gkode)
                var xRow = points.Take(maxPoints).Select(p => (p[0] - originX).ToString("F1", CultureInfo.InvariantCulture).PadLeft(12));
                Console.WriteLine($"    {"X [m]",-20}{string.Join("", xRow)}");

                // Y coordinates (relative to gkodn)
                var yRow = points.Take(maxPoints).Select(p => (p[1] - originY).ToString("F1", CultureInfo.InvariantCulture).PadLeft(12));
                Console.WriteLine($"    {"Y [m]",-20}{string.Join("", yRow)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Error parsing geometry: {ex.Message}");
            }
        }
    }
}