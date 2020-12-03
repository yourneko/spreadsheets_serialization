using System;
using System.Linq;
using System.Collections.Generic;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    internal static class SheetGroupsExtensions
    {
        internal static string SubstituteIndices(this string _indexedString, int[] _indices)
        {
            UnityEngine.Debug.Assert (_indexedString.Count ('#'.Equals) == _indices.Length); // all indices should be assigned at once
            int index = 0;
            return _indexedString.Replace ("#", GetNextElement (ref index, (x) => _indices[x].ToString()));
        }

        internal static T GetNextElement<T>(ref int pointer, Func<int, T> _get) => _get (pointer++);

        internal static bool MatchesAny(this string[] _sheets, string _compared) => _sheets.Any (x => StringComparer.OrdinalIgnoreCase.Equals (_compared, x));

        internal static bool ContainsRequiredSheets(this string[] _sheets, SheetsGroupInfo _group) => SheetsGroupInfo.HasRequiredSheets (_group, _sheets);

        internal static List<T[]> ExpandSort<T>(this Func<T, int> _sort, T[] _values)
        {
            return _values.GroupBy (x => _sort (x)).Select (x => x.ToArray ()).ToList();
        }
    }
}