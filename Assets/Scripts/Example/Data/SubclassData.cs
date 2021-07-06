using System;
using RecursiveMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MapClass] public class SubclassData
    {
        [MapField, SerializeField] string name;
        [MapField, SerializeField] int subvalue;
        [MapField, SerializeField] StructData[] keyValueArray;
    }
}