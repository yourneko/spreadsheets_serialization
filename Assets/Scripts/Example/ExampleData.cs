using System;
using System.Collections.Generic;
using Mimimi.SpreadsheetsSerialization;
using Mimimi.Tools.A1Notation;
using UnityEngine;

namespace Example
{
    [Serializable, Sheet ("ExampleSheet")]
    public class ExampleData
    {
        [Map (2, 1), Array (0, A1Direction.Row)]
        [SerializeField] string[] ss1;

        [Map (0, 2), Array (0, A1Direction.Row)]
        [SerializeField] List<string> ss2;

        [Map (3)]
        [SerializeField] StructData keyValueArray;

        [Map (0)]
        [SerializeField] string s;

        [Map (1)]
        [SerializeField] int i;

        [Map (0, 1),
         Array (0, A1Direction.Row),
         Array (1, A1Direction.Column)]
        private readonly int[][] intGrid = new int[][] { new[] { 1, 2, 3 }, new[] { 4, 5, 6, }, new[] { 7, 8, 9 } };

        [Map (0, 3), Array (0, A1Direction.Column)]
        [SerializeField] List<SubclassData> subList;
    }
}
