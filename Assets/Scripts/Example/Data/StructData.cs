using System;
using RecursiveMapper;

namespace Example
{
    [Serializable, MappedClass]
    public class StructData
    {
        [Mapped (0)] public string key;
        [Mapped (1)] public string value;
    }
}