// ** REFACTORED: Using the new, centralized MeteoStations class. **
using System.Collections.Generic;
using static LEG.MeteoSwiss.Abstractions.ReferenceData.MeteoStations;

// ** REFACTORED: Namespace updated to reflect the new project location. **
namespace LEG.MeteoSwiss.Abstractions.ReferenceData
{
    public static class DictionaryMeteoStations
    {
        public static readonly Dictionary<string, string> MeteoStationsDictionary = new()
        {
            { AND, "Andermatt" },
            { ARO, "Arosa" },
            { BEH, "Bernhardin" },
            { BIV, "Bivio" },
            { BUF, "Buffalora" },
            { CHU, "Chur" },
            { CMA, "Cimetta" },
            { COV, "Covara" },
            { DAV, "Davos" },
            { DIS, "Disentis" },
            { GRO, "Grono" },
            { GUE, "Gütsch" },
            { HOE, "Hörnli" },
            { ILZ, "Ilanz" },
            { KLO, "Kloten" },
            { LAE, "Lägern" },
            { LAT, "La Dôle" },
            { NAS, "Nàss" },
            { PFA, "Pfäffikon" },
            { PMA, "Plaffeien" },
            { REH, "Rehen" },
            { ROB, "Robbia" },
            { SAM, "Samedan" },
            { SBE, "Säntis" },
            { SCU, "Scuol" },
            { SIA, "Sial" },
            { SMM, "St. Maria" },
            { SRS, "Sils/Segl Maria" },
            { SMA, "Zürich-Fluntern" },
            { UEB, "Uetliberg" },
            { VAB, "Valbella" },
            { VIO, "Vicosoprano" },
            { VLS, "Vals" },
            { WAE, "Wädenswil" },
            { WFJ, "Weissfluhjoch" }
        };
    }
}