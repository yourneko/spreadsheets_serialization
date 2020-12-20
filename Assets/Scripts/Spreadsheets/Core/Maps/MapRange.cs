using System;
using System.Reflection;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class MapRange : Map
    {
        // Called via Reflection
        public static MapRange Create<T>(T _value, DimensionInfo[] _genericParameterDimensions)
        {
            return new MapRange (_type: ClassMapping.GetEnumeratedTypes (_fieldType: typeof (T),
                                                            _dimensions: _genericParameterDimensions.Length),
                                 _value: AssembleArray (_array: new FlexibleArray<T> (_value),
                                                        _dimensions: _genericParameterDimensions));
        }

        /// <summary> Turning a FlexibleArray of objects of ContainedType to a FlexibleArray with maps of those objects. </summary>
        public FlexibleArray<Map> ExpandRange()
        {
            var expandGenericMethod = typeof (MapRange).GetMethod ("ExpandToMaps", BindingFlags.NonPublic | BindingFlags.Static)
                                                       .MakeGenericMethod (ContainedType);
            return (FlexibleArray<Map>) expandGenericMethod.Invoke (null, new object[] { Values });
        }
        
        private MapRange(Type[] _type, object _value)
        {
            Types = _type;
            Values = _value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage ("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called through Reflection")]
        private static FlexibleArray<Map> ExpandToMaps<T>(FlexibleArray<T> _data)
        {
            var expanded = _data.Bind (x => ClassMapping.ObjectToMapArray (x));
            return FlexibleArray<Map>.Simplify (expanded);
        }
    }
}