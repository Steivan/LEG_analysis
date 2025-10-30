using CsvHelper.Configuration.Attributes;

namespace LEG.MeteoSwiss.Abstractions
{
    public class WeatherCsvRecord
    {
        [Name("station_abbr")]
        public string StationAbbr { get; set; } = string.Empty;

        [Name("reference_timestamp"), TypeConverter(typeof(DateTimeCustomFormatConverter))]
        public DateTime ReferenceTimestamp { get; set; }

        [Name("ta1tows0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Ta1Tows0 { get; set; }

        [Name("tdetows0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? TdeTows0 { get; set; }

        [Name("uretows0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? UreTows0 { get; set; }

        [Name("fkltowz1"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? FklTowz1 { get; set; }

        [Name("fk1towz0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Fk1Towz0 { get; set; }

        [Name("dv1towz0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Dv1Towz0 { get; set; }

        [Name("fu3towz0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Fu3Towz0 { get; set; }

        [Name("fu3towz1"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Fu3Towz1 { get; set; }

        [Name("gre000z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Gre000z0 { get; set; }

        [Name("sre000z0"), TypeConverter(typeof(NullableDoubleConverter))]
        public double? Sre000z0 { get; set; }
    }
}