namespace LEG.MeteoSwiss.Abstractions.Models
{
    public readonly struct StationMetaInfo(
        string stationName, string stationCanton, string stationWigosId,
        string stationTypeDe, string stationTypeFr, string stationTypeIt, string stationTypeEn,
        string stationDataowner, string stationDataSince,
        double? stationHeightMasl, double? stationHeightBarometerMasl,
        double? stationCoordinatesLv95East, double? stationCoordinatesLv95North,
        double? stationCoordinatesWgs84Lat, double? stationCoordinatesWgs84Lon,
        string stationExpositionDe, string stationExpositionFr, string stationExpositionIt, string stationExpositionEn,
        string stationUrlDe, string stationUrlFr, string stationUrlIt, string stationUrlEn)
    {
        public string StationName { get; } = stationName ?? "";
        public string StationCanton { get; } = stationCanton ?? "";
        public string StationWigosId { get; } = stationWigosId ?? "";
        public string StationTypeDe { get; } = stationTypeDe ?? "";
        public string StationTypeFr { get; } = stationTypeFr ?? "";
        public string StationTypeIt { get; } = stationTypeIt ?? "";
        public string StationTypeEn { get; } = stationTypeEn ?? "";
        public string StationDataowner { get; } = stationDataowner ?? "";
        public string StationDataSince { get; } = stationDataSince ?? "";
        public double? StationHeightMasl { get; } = stationHeightMasl;
        public double? StationHeightBarometerMasl { get; } = stationHeightBarometerMasl;
        public double? StationCoordinatesLv95East { get; } = stationCoordinatesLv95East;
        public double? StationCoordinatesLv95North { get; } = stationCoordinatesLv95North;
        public double? StationCoordinatesWgs84Lat { get; } = stationCoordinatesWgs84Lat;
        public double? StationCoordinatesWgs84Lon { get; } = stationCoordinatesWgs84Lon;
        public string StationExpositionDe { get; } = stationExpositionDe ?? "";
        public string StationExpositionFr { get; } = stationExpositionFr ?? "";
        public string StationExpositionIt { get; } = stationExpositionIt ?? "";
        public string StationExpositionEn { get; } = stationExpositionEn ?? "";
        public string StationUrlDe { get; } = stationUrlDe ?? "";
        public string StationUrlFr { get; } = stationUrlFr ?? "";
        public string StationUrlIt { get; } = stationUrlIt ?? "";
        public string StationUrlEn { get; } = stationUrlEn ?? "";
    }
}