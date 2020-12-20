using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mimimi.Tools.A1Notation;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public static class ClassMapping
    {
        public static readonly A1Point DefaultRangePivotPoint = new A1Point (SpreadsheetsHelpers.DEFAULT_RANGE_PIVOT);

        private static readonly Dictionary<Type, FlexibleArray<FieldInfo>> fieldsDictionary = new Dictionary<Type, FlexibleArray<FieldInfo>>();

#region Mapping

        /// <summary> Uses the scheme of type to create a FlexibleArray of Map containers. </summary>
        public static FlexibleArray<Map> ObjectToMapArray<T>(T obj)
        {
            var fields = fieldsDictionary.ContainsKey (typeof (T)) ? 
                         fieldsDictionary[typeof (T)] : 
                         CreateFieldsArray (typeof(T));
            return fields.Bind (obj.ObjectToMap);
        }

        public static FlexibleArray<FieldInfo> GetClassFields(Type _type)
        {
            if (IsMappableType (_type))
                return fieldsDictionary.ContainsKey (_type) ? 
                    fieldsDictionary[_type] : 
                    CreateFieldsArray (_type);
            else 
                throw new InvalidOperationException ();
        }

        private static FlexibleArray<FieldInfo> CreateFieldsArray(Type _type)
        {
            UnityEngine.Debug.Assert (ClassMapping.IsMappableType (_type), $"To be mapped, the type {_type.Name} has to have MapAttribute.");

            // Mapping T as groups of arrays of fields. Using user-entered parameters of attributes to sort it.
            // This kind of structure works for mapping into grids.
            var fields =   _type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Where(x => x.GetCustomAttribute<MapAttribute>() != null)
                                .GroupBy (x => x.GetCustomAttribute<MapAttribute> ().GroupIndex)
                                .OrderBy (x => x.Key)
                                .Select (x => x.OrderBy (a => a.GetCustomAttribute<MapAttribute> ().ElementIndex)
                                                .ToFlexArray (A1Direction.Row))
                                .ToArray (); 
            var result = new FlexibleArray<FieldInfo> (fields, new DimensionInfo (A1Direction.Column));
            fieldsDictionary.Add(_type, result);
            return result;
        }

#endregion
#region Unmappage

        // Used by GetRequest
        // Assembles an object of requested type from _values, and then passed it to user through callback 
        public static void InvokeGetCallback(Type _type, object _callback, FlexibleArray<string> _values)
        {
            var genericCallbackParameters = new[] { _callback, GetObjectValue (_type, _values) };
            typeof (ClassMapping).GetMethod ("GenericCallbackInvoke", BindingFlags.Static | BindingFlags.NonPublic)
                                 .MakeGenericMethod (_type)
                                 .Invoke (null, genericCallbackParameters);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage ("CodeQuality", "IDE0051:Remove unused private members", Justification = "Method is called via Reflection")]
        private static void GenericCallbackInvoke<T>(object _callback, object _value)
        {
            ((Action<T>)_callback).Invoke ((T)_value);
        }

        // NOTE:
        // Collection classes are not mapped, and all these conditions won't work on them.
        // Objects of collection types are made separately, in  AssignCollectionValues  method.
        public static object GetObjectValue(Type _type, FlexibleArray<string> _values)
        {
            if (IsMappableType (_type))
            {
                object result = Activator.CreateInstance (_type);
                AssembleMappableObject (result, _values);
                return result;
            }
            // SingleValue types - and that means 'every class without a mapped attribute'. 
            // Only few of them got custom deserialization. Just add it if you miss it.
            else
                return ValueSerializer.FromString (_values.FirstValue, _type);
        }

        /// <summary> Fills fields of existing object with values, field by field deserializing them from the array. </summary>
        private static void AssembleMappableObject(object _target, FlexibleArray<string> _values)
        {
            Type objectType = _target.GetType ();
            UnityEngine.Debug.Assert (IsMappableType (objectType));

            var fieldValues = GetClassFields (objectType).Associate (_values).GetValues ();
            foreach (var (fieldInfo, values) in fieldValues)
            {
                var arrayAttributeCount = fieldInfo.GetCustomAttributes<ArrayAttribute> ().Count();
                var fieldValue = (arrayAttributeCount > 0) ? 
                    AssignCollectionValues (GetEnumeratedTypes (fieldInfo.FieldType, arrayAttributeCount), values) : 
                    GetObjectValue(fieldInfo.FieldType, values);
                fieldInfo.SetValue (_target, fieldValue);
            }
        }

        // First type in the array is the type of collection.
        // Second type is the generic type parameter of the collection.
        // If there are more of types passed in, this method will be called recursively, 
        //          removing the first element from the type array on each step.
        /// <summary> Creates a Collection object. </summary>
        private static object AssignCollectionValues(Type[] _types, FlexibleArray<string> _values)
        {
            UnityEngine.Debug.Assert (_types.Length > 1);
            UnityEngine.Debug.Assert (_types[0].IsExpandable (_types[1]));

            Type[] nextStepTypes = _types.Skip (1).ToArray ();

            // _values might be null when sheets contain 0 of those objects
            if (_types[0].IsArray)
                return _values.IsValue && _values.FirstValue is null ? // null check for arrays
                        Array.CreateInstance (_types[0], 0) :
                        CreateArray (nextStepTypes, _values);
            else
            {
                object collectionObject = Activator.CreateInstance (_types[0]);
                if (_values.IsValue && _values.FirstValue is null) // null check for non-array collections
                    return collectionObject;
                MethodInfo genericAddMethod = _types[0].GetGenericTypeDefinition()
                                                       .MakeGenericType(_types[1])
                                                       .GetMethod ("Add", BindingFlags.Public | BindingFlags.Instance);

                foreach (var e in _values.Enumerate ().Select (nextStepTypes.GetElement))
                    genericAddMethod.Invoke (collectionObject, new[] { e });
                return collectionObject;
            }
        }

        private static object CreateArray(Type[] _types, FlexibleArray<string> _values)
        {
            var items = _values.Enumerate ().Select (_types.GetElement).ToArray ();
            var array = Array.CreateInstance (_types[0], items.Length);
            for (int i = 0; i < items.Length; i++)
                array.SetValue (items[i], i);
            return array;
        }

        private static object GetElement(this Type[] _types, FlexibleArray<string> _value)
        {
            return _types.Length > 1 ?
                   AssignCollectionValues (_types, _value) :
                   GetObjectValue (_types[0], _value);
        }

#endregion
#region Type mapping properties

        public static Type[] GetEnumeratedTypes(Type _fieldType, int _dimensions)
        {
            List<Type> tt = new List<Type> () { _fieldType };
            for (int i = 0; i < _dimensions; i++)
                tt.Add (GetEnumeratedType (tt.Last ()));
            return tt.ToArray ();
        }

        public static Type GetEnumeratedType(Type _type)
        {
            UnityEngine.Debug.Assert (_type.GetInterfaces ().Any (x => x.IsAssignableFrom (typeof (IEnumerable<>))), $"type of {_type.Name} has no enumerated type");

            return _type.GetTypeInfo ()
                        .GetInterfaces ()
                        .FirstOrDefault (x => x.IsGenericType)
                        .GetGenericArguments ()[0];
        }

        public static bool IsExpandable(this Type _collectionType, Type _elementType)
        {
            var interfaceType = typeof (ICollection<>).MakeGenericType (_elementType);
            return _collectionType.GetInterfaces ()
                                  .Any (x => x.IsAssignableFrom (interfaceType));
        }

        public static bool IsMappableType(Type _type) => _type.GetCustomAttributes<MapSpaceAttribute> ().Any ();

        public static SpaceRequired GetTypeSpaceRequirement(Type _type) => _type?.GetCustomAttribute<MapSpaceAttribute> ()?.RequiredSpace ?? SpaceRequired.SingleValue;

        public static A1Point GetPivotPoint(Type _type)
        {
            SheetAttribute attribute = _type.GetCustomAttribute<SheetAttribute> ();
            return attribute?.CustomRangeAnchor ?? DefaultRangePivotPoint;
        }

        public static Type BaseFieldType(this FieldInfo _fieldInfo)
        {
            return _fieldInfo.GetFieldDimensionsTypes ().Last ();
        }

#endregion

        public static FlexibleArray<FieldInfo> SmallElementsArray(Type _type)
        {
            UnityEngine.Debug.Assert (GetTypeSpaceRequirement (_type) == SpaceRequired.SheetsGroup);
            return GetClassFields (_type).Filter (IsSmallElementField);
        }

        private static bool IsSmallElementField(FieldInfo _info) => GetTypeSpaceRequirement (_info.BaseFieldType ()) < SpaceRequired.Sheet;
    }
}
