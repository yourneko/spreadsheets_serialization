using System;
using Mimimi.SpreadsheetsSerialization;

namespace Example
{
    [Serializable, Mimimi.SpreadsheetsSerialization.Range]
    public class StructData
    {
        [Map (0)] public string key;
        [Map (1)] public string value;
    }
}
