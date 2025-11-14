using NetTopologySuite.Geometries;
using System;

namespace LEG.SwissTopo.Abstractions
{
    public record RecordRoofProperties(
        int ObjectId,
        long DfUid,
        int DfNummer,
        DateTime DatumErstellung,
        DateTime DatumAenderung,
        Guid SbUuid,
        int SbObjektart,
        DateTime SbDatumErstellung,
        DateTime SbDatumAenderung,
        int Klasse,
        double Flaeche,
        int Ausrichtung,
        int Neigung,
        int Mstrahlung,
        long Gstrahlung,
        long Stromertrag,
        long StromertragSommerhalbjahr,
        long StromertragWinterhalbjahr,
        long Waermeertrag,
        int Duschgaenge,
        int DgHeizung,
        int DgWaermebedarf,
        long BedarfWarmwasser,
        long BedarfHeizung,
        double FlaecheKollektoren,
        long VolumenSpeicher,
        long? GwrEgid,

        // Monthly arrays
        int[]? Monate,
        double[]? MstrahlungMonat,
        double[]? AParam,
        double[]? BParam,
        double[]? CParam,
        int[]? Heizgradtage,
        double[]? MtempMonat,
        long[]? StromertragMonat,

        // Shape
        Geometry? Shape,
        double ShapeLength,
        double ShapeArea
    );
}