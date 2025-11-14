using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Globalization;

namespace LEG.MeteoSwiss.Abstractions
{
    public class DateTimeCustomFormatConverter : DateTimeConverter
    {
        private readonly string _format;

        public DateTimeCustomFormatConverter()
        {
            _format = "dd.MM.yyyy HH:mm"; // matches '13.01.2020 00:00'
        }

        public DateTimeCustomFormatConverter(string format)
        {
            _format = format;
        }

        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (DateTime.TryParseExact(text, _format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
}