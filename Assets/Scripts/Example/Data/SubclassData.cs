using System;
using SpreadsheetsMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MapClass] public class SubclassData
    {
        [MapField, SerializeField] string name;
        [MapField, SerializeField] int subvalue;
        [MapField(3), SerializeField] StructData[] keyValueArray;
    }
}
