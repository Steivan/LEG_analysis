namespace LEG.MeteoSwiss.Abstractions.ReferenceData
{
    /// <summary>
    /// Defines the official abbreviations for MeteoSwiss stations.
    /// This data is now centralized in the abstractions layer.
    /// </summary>
    public static class MeteoStations
    {
        public const string AND = "AND"; // Andermatt
        public const string ARO = "ARO"; // Arosa
        public const string BEH = "BEH"; // Bernhardin
        public const string BIV = "BIV"; // Bivio
        public const string BUF = "BUF"; // Buffalora
        public const string CHU = "CHU"; // Chur
        public const string CMA = "CMA"; // Cimetta
        public const string COV = "COV"; // Covara
        public const string DAV = "DAV"; // Davos
        public const string DIS = "DIS"; // Disentis
        public const string GRO = "GRO"; // Grono
        public const string GUE = "GUE"; // Gütsch
        public const string HOE = "HOE"; // Hörnli
        public const string ILZ = "ILZ"; // Ilanz
        public const string KLO = "KLO"; // Kloten
        public const string LAE = "LAE"; // Lägern
        public const string LAT = "LAT"; // La Dôle
        public const string NAS = "NAS"; // Nàss
        public const string PFA = "PFA"; // Pfäffikon
        public const string PMA = "PMA"; // Plaffeien
        public const string REH = "REH"; // Rehen
        public const string ROB = "ROB"; // Robbia
        public const string SAM = "SAM"; // Samedan
        public const string SBE = "SBE"; // Säntis
        public const string SCU = "SCU"; // Scuol
        public const string SIA = "SIA"; // Sial
        public const string SMM = "SMM"; // St. Maria
        public const string SRS = "SRS"; // Sils/Segl Maria
        public const string SMA = "SMA"; // Zürich-Fluntern
        public const string UEB = "UEB"; // Uetliberg
        public const string VAB = "VAB"; // Valbella
        public const string VIO = "VIO"; // Vicosoprano
        public const string VLS = "VLS"; // Vals
        public const string WAE = "WAE"; // Wädenswil
        public const string WFJ = "WFJ"; // Weissfluhjoch
    }
}

// Old implementation: Compare this snippet from LEG.CoreLib/ReferenceData/MeteoStations.cs:
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace LEG.CoreLib.ReferenceData
//{
//    public class MeteoStations
//    {
//        // Ground Stations in ZH
//        public const string HOE = nameof(HOE);
//        public const string KLO = nameof(KLO);
//        public const string LAE = nameof(LAE);
//        public const string PFA = nameof(PFA);
//        public const string REH = nameof(REH);
//        public const string SMA = nameof(SMA);
//        public const string UEB = nameof(UEB);
//        public const string WAE = nameof(WAE);

//        // Ground Stations in GR
//        public const string AND = nameof(AND);
//        public const string ARO = nameof(ARO);
//        public const string BEH = nameof(BEH);
//        public const string BIV = nameof(BIV);
//        public const string BUF = nameof(BUF);
//        public const string CHU = nameof(CHU);
//        public const string CMA = nameof(CMA);
//        public const string COV = nameof(COV);
//        public const string DAV = nameof(DAV);
//        public const string DIS = nameof(DIS);
//        public const string GRO = nameof(GRO);
//        public const string ILZ = nameof(ILZ);
//        public const string LAT = nameof(LAT);
//        public const string NAS = nameof(NAS);
//        public const string PMA = nameof(PMA);
//        public const string ROB = nameof(ROB);
//        public const string SAM = nameof(SAM);
//        public const string SBE = nameof(SBE);
//        public const string SCU = nameof(SCU);
//        public const string SIA = nameof(SIA);
//        public const string SMM = nameof(SMM);
//        public const string SRS = nameof(SRS);
//        public const string VAB = nameof(VAB);
//        public const string VIO = nameof(VIO);
//        public const string VLS = nameof(VLS);
//        public const string WFJ = nameof(WFJ);

//    }
//}