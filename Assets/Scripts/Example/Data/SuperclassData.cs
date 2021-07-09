using System;
using SpreadsheetsMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MapClass ("Group {0}")]
    public class SuperclassData
    {
        [SerializeField, MapField, MapPlacement(SortOrder = 1)] ExampleData[] data;
        [SerializeField, MapField, MapPlacement(SortOrder = 0)] string title;
    }
}