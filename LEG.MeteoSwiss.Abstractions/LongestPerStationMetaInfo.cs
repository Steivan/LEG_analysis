namespace LEG.MeteoSwiss.Abstractions
{
    public class LongestPerStationMetaInfo
    {
        public string Name { get; set; } = string.Empty;
        public string NatAbbr { get; set; } = string.Empty;
        public string WmoInd { get; set; } = string.Empty;
        public double? Chx { get; set; }
        public double? Chy { get; set; }
        public double? Lon { get; set; }
        public double? Lat { get; set; }
        public double? Height { get; set; }
        public string ClimateRegion { get; set; } = string.Empty;
        public int? ClimateRegionNr { get; set; }
        public int? FirstYearDailyObs { get; set; }
        public int? LastYearDailyObs { get; set; }
        public int? TotNrYearsDailyObs { get; set; }
        public int? NrNaYearsDailyObs { get; set; }
        public int? NrYearsDailyObsInRefPer { get; set; }
        public int? NrNaYearsDailyObsInRefPer { get; set; }
        public int? FirstYearHourlyObs { get; set; }
        public int? LastYearHourlyObs { get; set; }
        public int? NrYearsHourlyObs { get; set; }
        public int? NrNaYearsHourlyObs { get; set; }
        public bool HasHourlyData { get; set; }
    }
}