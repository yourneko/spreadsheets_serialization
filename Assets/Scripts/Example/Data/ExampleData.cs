using System;
using System.Collections.Generic;
using RecursiveMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MappedClass ("ExampleSheet", false)]
    public class ExampleData
    {
        [Mapped (5, 1)]
        [SerializeField] string[] ss1;

        [Mapped (3, 1)]
        [SerializeField] List<string> ss2;

        [Mapped (2)]
        [SerializeField] StructData keyValueArray;

        [Mapped (0)]
        [SerializeField] string s;

        [Mapped (1)]
        [SerializeField] int i;

        [Mapped (4, 2)]
        private readonly int[][] intGrid = { new[] { 1, 2, 3 }, new[] { 4, 5, 6, }, new[] { 7, 8, 9 } };

        [Mapped (6, 1)]
        [SerializeField] List<SubclassData> subList;
    }
}