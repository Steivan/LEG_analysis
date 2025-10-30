using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace LEG.Common
{
    public class Int32DefaultZeroConverter : Int32Converter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
}