namespace LEG.SwissTopo.Abstractions
{
    public class BuildingInfo
    {
        public string? Address { get; set; }
        public string? EGID { get; set; }
        public double X { get; set; } // CH1903+ LV95
        public double Y { get; set; }
        public double GArea { get; set; } // Building area from identify
        public string? GwrEgid { get; set; }
        public string? GebNr { get; set; }
        public string? Egrid { get; set; }
        public string? Lparz { get; set; }
        public Dictionary<string, object?> Properties { get; set; } = [];
    }
}