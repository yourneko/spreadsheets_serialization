using System;
using System.Collections.Generic;
using SpreadsheetsMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MapClass ("ExampleSheet")]
    public class ExampleData
    {
        [MapField, MapPlacement (SortOrder = 5), SerializeField] string[] ss1;
        [MapField, MapPlacement (SortOrder = 3), SerializeField] List<string> ss2;
        [MapField, MapPlacement (SortOrder = 2), SerializeField] StructData keyValueArray;
        [MapField, MapPlacement (SortOrder = 0), SerializeField] string s;
        [MapField, MapPlacement (SortOrder = 4), SerializeField] int i;
        [MapField, MapPlacement (SortOrder = 6), SerializeField] List<SubclassData> subList;

        [MapField(3, 3), MapPlacement (SortOrder = 1)] int[][] grid = {new[] {1, 2, 3}, new[] {4, 5, 6,}, new[] {7, 8, 9}};
    }
}
