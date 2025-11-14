using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Globalization;

namespace LEG.MeteoSwiss.Abstractions
{
    public class NullableDoubleConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Equals("NA", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
}