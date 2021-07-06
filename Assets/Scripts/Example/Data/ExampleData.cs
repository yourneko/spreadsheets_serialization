using System;
using System.Collections.Generic;
using RecursiveMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MapClass ("ExampleSheet")]
    public class ExampleData
    {
        [MapField, MapPlacementAttribute (5), SerializeField]
        string[] ss1;

        [MapField, MapPlacementAttribute (3), SerializeField]
        List<string> ss2;

        [MapField, MapPlacementAttribute (2), SerializeField]
        StructData keyValueArray;

        [MapField, MapPlacementAttribute (0), SerializeField]
        string s;

        [MapField, MapPlacementAttribute (4), SerializeField]
        int i;

        [MapField(3, 3), MapPlacementAttribute (5)] private readonly int[][] intGrid = {new[] {1, 2, 3}, new[] {4, 5, 6,}, new[] {7, 8, 9}};

        [MapField, MapPlacementAttribute (6), SerializeField]
        List<SubclassData> subList;
    }
}