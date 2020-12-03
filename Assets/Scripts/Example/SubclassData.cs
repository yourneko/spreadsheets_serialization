using System;
using Mimimi.SpreadsheetsSerialization;
using Mimimi.Tools.A1Notation;
using UnityEngine;

namespace Example
{
    [Serializable, Mimimi.SpreadsheetsSerialization.Range]
    public class SubclassData
    {
        [Map (0)]
        [SerializeField] string name;

        [Map (1)]
        [SerializeField] int subvalue;

        [Map (2), Array (0, A1Direction.Column)]
        [SerializeField] StructData[] keyValueArray;
    }
}
