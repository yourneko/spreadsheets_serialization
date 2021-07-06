using System;
using RecursiveMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MapClass ("Group {0}")]
    public class SuperclassData
    {
        [SerializeField, MapField, MapPlacement(1)] ExampleData[] data;
        [SerializeField, MapField, MapPlacement(0)] string title;
    }
}