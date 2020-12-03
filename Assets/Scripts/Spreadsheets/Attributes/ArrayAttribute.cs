using System;
using Mimimi.Tools.A1Notation;

namespace Mimimi.SpreadsheetsSerialization
{
    [AttributeUsage (AttributeTargets.Field, AllowMultiple = true)]
    public class ArrayAttribute : Attribute
    {
        public int limitElements = 0;

        public int Index { get; private set; }

        public A1Direction Direction { get; private set; }

        public ArrayAttribute(int _index, A1Direction _direction)
        {
            Index = _index;
            Direction = _direction;
        }
    }
}
