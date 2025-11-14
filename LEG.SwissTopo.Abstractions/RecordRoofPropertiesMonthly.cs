
namespace LEG.SwissTopo.Abstractions
{
    /// <summary>
    /// Represents monthly roof properties as per SOLKAT_CH_DACH_MONAT (Tabelle 4.).
    /// </summary>
    public record RecordRoofPropertiesMonthly(
        int ObjectId,                  // Int32: OBJECTID - Object ID
        long DfUid,                    // Int64: DF_UID - Dachflächenidentifikator (Fremdschlüssel auf SOLKAT_CH_DACH)
        int DfNummer,                  // Int16: DF_NUMMER - Dachflächennummer (fortlaufend pro Gebäude)
        Guid SbUuid,                   // Guid: SB_UUID - UUID swissBUILDINGS (Gebäudezuordnung)
        int Monat,                     // Int16: MONAT - Monat (Kalendermonat)
        double MstrahlungMonat,        // Float: MSTRAHLUNG_MONAT - Mittlere Einstrahlung [kWh/m2/Monat]
        double AParam,                 // Float: A_PARAM - Parameter a
        double BParam,                 // Float: B_PARAM - Parameter b
        double CParam,                 // Float: C_PARAM - Parameter c
        int Heizgradtage,              // Int16: HEIZGRADTAGE - Heizgradtage (monatlich)
        double MtempMonat,             // Float: MTEMP_MONAT - Monatsmitteltemperatur [°C]
        long StromertragMonat          // Int64: STROMERTRAG_MONAT - Elektrischer Ertrag [kWh/m2/Monat]
    );
}
