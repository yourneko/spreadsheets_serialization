using System;
using System.Linq;
using System.Reflection;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public static class ClassNaming
    {
        public const string PARAMETER_PLACE = "{0}";
        public const string DEFAULT_MAIN_SHEET_NAME = "Main";

        public static bool IsSheet(Type _type) => ClassMapping.GetTypeSpaceRequirement (_type) == SpaceRequired.Sheet;
        public static bool IsGroup(Type _type) => ClassMapping.GetTypeSpaceRequirement (_type) == SpaceRequired.SheetsGroup;

        private static string IndexedString(int[] _indices)
        {
            return (_indices is null || _indices.Length == 0) ?
                string.Empty :
                string.Join ("", _indices.Select (x => " " + x.ToString ()));
        }

        private static string SheetName(Type _type) => IsSheet (_type) ? _type.GetCustomAttribute<SheetAttribute> ().SheetName :
                                                       IsGroup (_type) ? _type.GetCustomAttribute<SheetsGroupAttribute> ().SheetName :
                                                       null;

        private static string SheetsGroupName(Type _type) => IsGroup (_type) ?
                                                             _type.GetCustomAttribute<SheetsGroupAttribute> ().GroupName :
                                                             null;


        public static string AssembleSheetName(Type _type, string _parametrized, params int[] _indices)
        {
            var result = string.Format (_parametrized, SheetName (_type) + IndexedString (_indices));
            UnityEngine.Debug.Assert (!result.Contains (PARAMETER_PLACE));
            return result;
        }

        public static string AssembleGroupName(Type _type, string _parametrized, params int[] _indices)
        {
            var result = string.Format (_parametrized, SheetsGroupName (_type) + IndexedString (_indices));
            if (!result.Contains (PARAMETER_PLACE))
                result += " " + PARAMETER_PLACE;
            return result;
        }
    }
}
