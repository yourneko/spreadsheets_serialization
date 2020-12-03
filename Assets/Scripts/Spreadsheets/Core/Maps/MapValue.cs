using System;
using System.Linq;
using System.Reflection;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    // wrapping common structs
    public class MapValue : Map
    {
        /// <param name="_genericParameterDimensions"> Replaces <typeparamref name="ContainedType"/> with a generic parameter of <typeparamref name="IEnumerable"/>,
        ///                                            instead of type <typeparamref name="T"/>. </param>
        /// <param name="_value"> An instance of type <typeparamref name="T"/>. If <typeparamref name="T"/> implements generic <typeparamref name="IEnumerable"/> interface, 
        ///                       add one or more of <typeparamref name="MapDimensionAttribute"/> to the origin field. </param>
        /// <summary> Represents an instance of type <typeparamref name="T"/> as <typeparamref name="FlexibleArray"/> of <typeparamref name="string"/> </summary>
        /// <remarks> If something go wrong, make sure <typeparamref name="ValueSerializer"/> implements a conversion 
        ///           from ContainedType type to <typeparamref name="string"/> </remarks>
        // Called via Reflection
        public static MapValue Create<T>(T _value, DimensionInfo[] _genericParameterDimensions)
        {
            var savedArray = AssembleArray (_array: new FlexibleArray<T> (_value),
                                            _dimensions: _genericParameterDimensions);
            Type[] types = ClassMapping.GetEnumeratedTypes (typeof (T), _genericParameterDimensions.Length);

            var stringArray = typeof (MapValue).GetMethod ("SerializeValues", BindingFlags.NonPublic | BindingFlags.Static)
                                                .MakeGenericMethod (types.Last ())
                                                .Invoke (null, new object[] { savedArray });
            return new MapValue (types, stringArray);
        }

        private MapValue(Type[] _types, object _values)
        {
            Types = _types;
            Values = _values;
        }

        /// <summary> Value of MapValue should always be a FlexibleArray of strings stored as object. </summary>
        public FlexibleArray<string> GetStringValues() => (FlexibleArray<string>)Values;

        [System.Diagnostics.CodeAnalysis.SuppressMessage ("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called through Reflection")]
        private static FlexibleArray<string> SerializeValues<T>(FlexibleArray<T> _array)
        {
            return _array is FlexibleArray<string> stringArray ? 
                stringArray : 
                _array.Bind (ValueSerializer.AsString);
        }
    }
}
