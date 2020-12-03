using System;
using Mimimi.SpreadsheetsSerialization;
using Mimimi.Tools.A1Notation;
using UnityEngine;

namespace Example
{
    [Serializable, SheetsGroup("Group")]
    public class SuperclassData
    {
        [Map (0, 1), Array (0, A1Direction.Column)]
        [SerializeField] ExampleData[] data;

        [Map (0)]
        [SerializeField] string title;
    }
}
