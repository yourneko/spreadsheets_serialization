using System;
using System.Linq;
using System.Reflection;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    abstract class Map
    {
        private static readonly MethodInfo createRangeMethod, createValueMethod;

        /// <summary>  Starts with a field type. If type is a generic IEnumerable, next type is a generic parameter of it. </summary>
        public Type[] Types { get; protected set; }

        /// <summary> The FlexibleArray of either ContainedType (MapRange) or strings (MapValue). </summary>
        public object Values { get; protected set; }

        /// <summary> Matches a generic parameter of the array. </summary>
        public Type ContainedType => Types.Last ();

        public SpaceRequired SpaceRequirement => ClassMapping.GetTypeSpaceRequirement (ContainedType);

        public string Header { get; protected set; }

#region Create mapped container

        /// <summary> Simplify FlexibleArray of generic IEnumerable type by expanding ienumeration into another array dimension. </summary>
        protected static object AssembleArray<T>(FlexibleArray<T> _array, params DimensionInfo[] _dimensions)
        {
            object arrObj = _array;
            Type currentType = typeof (T);
            if (_dimensions.Length > 0)
            {   
                for (int i = 0; i < _dimensions.Length; i++)
                {
                    // changes a generic parameter type of FlexibleArray, sort of  FlexibleArray<List<int>>  to  FlexibleArray<int> 
                    Type expansionType = ClassMapping.GetEnumeratedType (currentType);
                    MethodInfo expandGenericMethod = typeof (Map).GetMethod ("ExpandEnumerableTypes", BindingFlags.NonPublic | BindingFlags.Static)
                                                                 .MakeGenericMethod (currentType, expansionType);
                    arrObj = expandGenericMethod.Invoke (null, new object[] { arrObj, _dimensions[i] });
                    currentType = expansionType;
                }
            }
            return arrObj;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage ("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called through Reflection")]
        private static FlexibleArray<O> ExpandEnumerableTypes<T, O>(FlexibleArray<T> _array, DimensionInfo _dimension)
        {
            return _array.ExpandArray<T, O> (_dimension);
        }

#endregion
#region Create maps

        public static Map Create(object _target, params DimensionInfo[] _dimensions)
        {
            UnityEngine.Debug.Assert (_target != null, "Can't map null");
            Type containedType = ClassMapping.GetEnumeratedTypes (_target.GetType (), _dimensions.Length).Last();
            MethodInfo method = !ClassMapping.IsMappableType (containedType) ?
                                 createValueMethod :
                                 createRangeMethod;
            return (Map)method.MakeGenericMethod (_target.GetType ())
                              .Invoke (null, new object[] { _target, _dimensions });
        }

        // static initializer
        static Map()
        {
            createValueMethod = typeof (MapValue).GetMethod ("Create", BindingFlags.Public | BindingFlags.Static)
                                                     .GetGenericMethodDefinition ();
            createRangeMethod = typeof (MapRange).GetMethod ("Create", BindingFlags.Public | BindingFlags.Static)
                                                     .GetGenericMethodDefinition ();
        }

#endregion
    }
}
