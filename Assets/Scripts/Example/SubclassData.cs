using System;
using RecursiveMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MappedClass]
    public class SubclassData
    {
        [Mapped (0)]
        [SerializeField] string name;

        [Mapped (1)]
        [SerializeField] int subvalue;

        [Mapped (2, 1)]
        [SerializeField] StructData[] keyValueArray;
    }
}