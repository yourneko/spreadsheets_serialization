using System;

namespace Mimimi.SpreadsheetsSerialization
{
    [AttributeUsage (AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public abstract class MapSpaceAttribute : Attribute
    {
        public abstract SpaceRequired RequiredSpace { get; }
    }
}
