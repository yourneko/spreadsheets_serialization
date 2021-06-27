using System;
using RecursiveMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MappedClass("Group {0}", false)]
    public class SuperclassData
    {
        [SerializeField, Mapped(1, 1)] ExampleData[] data;

        [SerializeField, Mapped(0)] string title;
    }
}