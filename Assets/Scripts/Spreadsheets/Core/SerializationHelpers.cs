using System;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public static class SerializationHelpers
    {
        public const string DEFAULT_RANGE_PIVOT = "B2";
        public const string DEFAULT_RANGE_END = "ZZ999";

        public static string GetSheetName(this ValueRange _range) => _range.Range.Split ('!')[0].Trim ('\'');
        public static bool MatchSheetName(this ValueRange _range, string _match) => StringComparer.OrdinalIgnoreCase.Equals (_range.GetSheetName (), _match);

        private static string Range(string _sheet, string _pivot) => $"'{_sheet}'!{_pivot}:{DEFAULT_RANGE_END}";
        public static string Range(string _sheet, Type _type) => Range (_sheet, ClassMapping.GetPivotPoint (_type).A1);

        public static string LogFlexStringArray(FlexibleArray<string> _array)
        {
            return _array.IsValue ?
                   $"'{_array.FirstValue}'" :
                   $"({string.Join(" ", _array.Enumerate().Select(x => LogFlexStringArray(x)))})";
        }

        public static string LogFlexStringArray<T>(FlexibleArray<T> _array, Func<T, string> _tostring)
        {
            return _array.IsValue ?
                   $"'{_tostring.Invoke(_array.FirstValue)}'" :
                   $"({string.Join (" ", _array.Enumerate ().Select (x => LogFlexStringArray (x, _tostring)))})";
        }

        public static string LogFlexFields(Type _type)
        {
            return LogFlexStringArray (ClassMapping.GetClassFields (_type), x => x.FieldType.Name);
        }
    }
}