using System;
using System.Collections.Generic;
using RecursiveMapper;
using UnityEngine;

namespace Example
{
    [Serializable, MappedClass("EZ-DATA")]
    public class EasyData
    {
        [SerializeField, Mapped (0, 1)] private List<string> strings;
    }
}