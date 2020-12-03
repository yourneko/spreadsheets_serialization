using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mimimi.Tools.A1Notation;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public static class MappingExtensions
    {
#region Conversions over FlexArrays

        public static FlexibleArray<T> ToFlexArray<T>(this IEnumerable<T> _values, DimensionInfo _dimensionInfo)
        {
            return new FlexibleArray<T> (_values.Select (x => new FlexibleArray<T> (x)), _dimensionInfo);
        }

        public static FlexibleArray<T> ToFlexArray<T>(this IEnumerable<T> _values, A1Direction _direction)
        {
            return _values.ToFlexArray (new DimensionInfo (_direction));
        }

        /// <summary> To avoid exceptions, test types with IsExpandable(Type, Type) </summary>
        public static FlexibleArray<O> ExpandArray<T, O>(this FlexibleArray<T> _target, DimensionInfo _dimension)
        {
            UnityEngine.Debug.Assert (ClassMapping.IsExpandable (typeof (T), typeof (O)));
            return _target.Expand (ExpandFunction<T, O>, _dimension);
        }

        public static FlexibleArray<O> CollapseArray<T, O>(this FlexibleArray<T> _target)
        {
            UnityEngine.Debug.Assert (ClassMapping.IsExpandable (typeof (O), typeof (T))); // inverted order compared to Expand
            return _target.Collapse (CollapseFunction<T, O>);
        }

#endregion
#region Field to Mapped

        private static ICollection<O> ExpandFunction<T, O>(T _element)
        {
            return _element is ICollection<O> ie ? ie : null;
        }

        /// <param name="_parentObject"> Object of type T. </param>
        /// <param name="_fieldInfo"> FieldInfo with attached MapAttribute in type T. </param>
        /// <returns> MapRange created from value of given field in parentObject. </returns>
        public static Map ObjectToMap(this object _parentObject, FieldInfo _fieldInfo)
        {
            UnityEngine.Debug.Assert (_parentObject != null, "value is null");
            UnityEngine.Debug.Assert (_fieldInfo != null, $"fieldInfo is null");
            return Map.Create (_fieldInfo.GetValue (_parentObject), GetDimensions(_fieldInfo));
        }

        public static DimensionInfo[] GetDimensions(this FieldInfo _fieldInfo)
        {
            return _fieldInfo.GetCustomAttributes<ArrayAttribute> ()
                             .OrderBy (x => x.Index)
                             .Select (x => new DimensionInfo (x.Direction))
                             .ToArray ();
        }

        public static Type[] GetFieldDimensionsTypes(this FieldInfo _fieldInfo)
        {
            return ClassMapping.GetEnumeratedTypes (_fieldInfo.FieldType, GetDimensions (_fieldInfo).Length);
        }

        public static bool IsBaseTypeMappable(this FieldInfo _fieldInfo)
        {
            return ClassMapping.IsMappableType (GetFieldDimensionsTypes (_fieldInfo).Last ());
        }

        #endregion
        #region Mapped -> Object

        private static O CollapseFunction<T, O>(IEnumerable<T> _dimension) // trying to get rid of new() constraint
        {
            O result = (O)Activator.CreateInstance (typeof (O));
            if (result is IEnumerable<T> ie)
            {
                ie.Concat (_dimension);
                return result;
            }
            else
                throw new InvalidCastException ();
        }

#endregion

        // it seems there is the same method in Linq. too tired to check
        public static IEnumerable<(A first, B second)> Parallel<A, B>(this IEnumerable<A> _target, IEnumerable<B> _other)
        {
            using (var et = _target.GetEnumerator())
            using (var eo = _other.GetEnumerator())
            {
                while (et.MoveNext () && eo.MoveNext())
                    yield return (et.Current, eo.Current);
            }
        }

        public static bool ExtendCondition<T>(this Predicate<T> _condition, FlexibleArray<T> _array)
        {
            return _array.IsValue ?
                   _condition.Invoke (_array.FirstValue) :
                   _array.Enumerate().Any (x => _condition.ExtendCondition (x));
        }

        public static string GetAxisString(this A1Direction _direction) 
        {
            switch (_direction)
            {
                case A1Direction.Column:   return "#Y";
                case A1Direction.Row:      return "#X";
                default:                    throw new Exception ();
            }
        }
    }
}
