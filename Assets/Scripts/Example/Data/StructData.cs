using System;
using SpreadsheetsMapper;

namespace Example
{
    [Serializable, MapClass] public class StructData
    {
        [MapField, MapPlacement (SortOrder = 0)] public string key;
        [MapField] public string value;
    }
}
