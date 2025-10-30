using System.Diagnostics.CodeAnalysis;

namespace LEG.SwissTopo.Abstractions
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Names match external data source columns")]
    public record RecordBuildingProperties(
        int? gastw,                // SMALLINT: Anzahl Geschosse
        int? gkat,                 // SMALLINT: Gebäudekategorie
        DateTime? gwaerdath1,      // DATETIME: Aktualisierungsdatum Heizung 1
        string? deinr,             // VARCHAR: Eingangsnummer Gebäude
        int? gwaerzw2,             // SMALLINT: Wärmeerzeuger Warmwasser 2
        int? esid,                 // INTEGER: Eidg. Strassenidentifikator (ESID)
        List<string>? strname,     // VARCHAR: Strassenbezeichnung
        int? edid,                 // SMALLINT: Eidg. Eingangsidentifikator (EDID)
        List<int>? wstat,          // SMALLINT: Wohnungsstatus
        string? plz_plz6,          // VARCHAR: PLZ/PLZ6
        int? gksce,                // SMALLINT: Koordinatenherkunft
        List<int?>? wabbj,         // SMALLINT: Abbruchjahr Wohnung
        List<string>? ewid,        // SMALLINT: Eidg. Wohnungsidentifikator (EWID)
        DateTime? dexpdat,         // DATETIME: Datenstand
        int? gwaerzh1,             // SMALLINT: Wärmeerzeuger Heizung 1
        List<int>? wstwk,          // SMALLINT: Stockwerk
        int? gschutzr,             // SMALLINT: Zivilschutzraum
        int? gabbj,                // SMALLINT: Abbruchjahr des Gebäudes
        int? gwaerzh2,             // SMALLINT: Wärmeerzeuger Heizung 2
        string? stroffiziel,       // VARCHAR: Strassenbezeichnung offiziell
        int? doffadr,              // SMALLINT: Offizielle Adresse
        string? egid,              // VARCHAR: Eidg. Gebäudeidentifikator (EGID)
        DateTime? gwaerdatw1,      // DATETIME: Aktualisierungsdatum Warmwasser 1
        int? gvolnorm,             // SMALLINT: Gebäudevolumen: Norm
        double? dkodn,             // FLOAT: N-Gebäudeeingangskoordinate
        string? egrid,             // VARCHAR: Eidg. Grundstücksidentifikator (EGRID)
        List<int>? wmehrg,         // SMALLINT: Mehrgeschossige Wohnung
        int? gbaup,                // SMALLINT: Bauperiode
        List<int>? wkche,          // SMALLINT: Kocheinrichtung
        DateTime? gexpdat,         // DATETIME: Publikationsstand
        int? gazzi,                // SMALLINT: Anzahl separate Wohnräume
        int? genh1,                // SMALLINT: Energie-/Wärmequelle Heizung 1
        int? gebf,                 // INTEGER: Energiebezugsfläche
        int? genw2,                // SMALLINT: Energie-/Wärmequelle Warmwasser 2
        double? dkode,             // FLOAT: E-Gebäudeeingangskoordinate
        string? gbez,              // VARCHAR: Name des Gebäudes
        List<string>? wbez,        // VARCHAR: Lage auf dem Stockwerk
        int? gbauj,                // SMALLINT: Baujahr des Gebäudes
        DateTime? gwaerdatw2,      // DATETIME: Aktualisierungsdatum Warmwasser 2
        int? gwaerscew2,           // SMALLINT: Informationsquelle Warmwasser 2
        int? genw1,                // SMALLINT: Energie-/Wärmequelle Warmwasser 1
        int? genh2,                // SMALLINT: Energie-/Wärmequelle Heizung 2
        int? gklas,                // SMALLINT: Gebäudeklasse
        string? lparz,             // VARCHAR: Grundstücksnummer
        int? gstat,                // SMALLINT: Gebäudestatus
        string? weinr,             // VARCHAR: Physische Wohnungsnummer
        int? gwaerscew1,           // SMALLINT: Informationsquelle Warmwasser 1
        int? lgbkr,                // SMALLINT: Grundbuchkreisnummer
        DateTime? gwaerdath2,      // DATETIME: Aktualisierungsdatum Heizung 2
        long? lparzsx,             // BIGINT: Suffix der Grundstücksnummer
        int? dplz4,                // SMALLINT: Postleitzahl
        string? whgnr,             // VARCHAR: Administrative WohnungsNr
        int? gbaum,                // SMALLINT: Baumonat des Gebäudes
        List<string>? strindx,     // VARCHAR: Indexzeichen Strasse
        double? gkode,             // FLOAT: E-Gebäudekoordinate
        List<string>? strnamk,     // VARCHAR: Kurztext Strasse
        string? strname_deinr,     // VARCHAR: Strasse Nr
        string? ggdename,          // VARCHAR: Gemeindename
        int? ggdenr,               // SMALLINT: BFS-Gemeindenummer
        List<int>? wbauj,          // SMALLINT: Baujahr Wohnung
        int? gvolsce,              // SMALLINT: Informationsquelle zum Gebäudevolumen
        string? gdekt,             // VARCHAR: Kantonskürzel
        int? garea,                // INTEGER: Gebäudefläche [m2]
        string? gebnr,             // VARCHAR: Amtliche Gebäudenummer
        int? gwaersceh2,           // SMALLINT: Informationsquelle Heizung 2
        int? ganzwhg,              // SMALLINT: Anzahl Wohnungen
        int? gvol,                 // INTEGER: Gebäudevolumen [m3]
        List<DateTime>? wexpdat,   // DATETIME: Datenstand
        int? ltyp,                 // SMALLINT: Typ des Grundstücks
        List<string>? strsp,       // VARCHAR: Sprache des Strassennamen
        List<int>? warea,          // SMALLINT: Wohnungsfläche [m2]
        int? dplzz,                // SMALLINT: Postleitzahl-Zusatzziffer
        string? id,                // VARCHAR: ch.bfs.gebaeude_wohnungs_register.id
        List<int>? wazim,          // SMALLINT: Anzahl Zimmer
        double? gkodn,             // FLOAT: N-Gebäudekoordinate
        int? gwaersceh1,           // SMALLINT: Informationsquelle Heizung 1
        string? dplzname,          // VARCHAR: Ortschaft
        int? gwaerzw1,             // SMALLINT: Wärmeerzeuger Warmwasser 1
        int? egaid                 // INTEGER: Eidg. Gebäudeadressidentifikator (EGAID)
    );
}