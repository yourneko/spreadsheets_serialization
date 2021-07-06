using System;
using RecursiveMapper;

namespace Example
{
    [Serializable, MapClass] public class StructData
    {
        [MapField, MapPlacementAttribute (0)] public string key;
        [MapField] public string value;
    }
}