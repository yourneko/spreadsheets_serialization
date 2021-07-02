using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    static class AttributeExtensions
    {
        static readonly MethodInfo AddMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.Instance | BindingFlags.Public);
        public static MappedAttribute MapAttribute(this FieldInfo field)
        {
            var attribute = (MappedAttribute)Attribute.GetCustomAttribute (field, typeof(MappedAttribute));
            if (attribute != null && !attribute.Initialized)
                attribute.CacheMeta (field);
            return attribute;
        }

        public static MappedClassAttribute MapAttribute(this Type type)
        {
            var attribute = (MappedClassAttribute)Attribute.GetCustomAttribute (type, typeof(MappedClassAttribute));
            if (!attribute.Initialized)
                attribute.CacheMeta (type);
            return attribute;
        }

        public static ContentType GetContentType(this Type type)     // probably move to attribute
        {
            var attribute = type.MapAttribute ();
            return attribute is null
                       ? ContentType.Value
                       : string.IsNullOrEmpty (attribute.SheetName)
                           ? ContentType.Object
                           : ContentType.Sheet;
        }

        static RecursiveMap<object> Unwrap(this object obj, string name, int depth)    // todo  - remove legacy
        {
            return depth == 0
                       ? new RecursiveMap<object> (obj, name)
                       : obj is ICollection c
                           ? new RecursiveMap<object> (c.Cast<object> ().Select ((e, i) => Unwrap (e, $"{name} {i}",depth - 1)), name)
                           : throw new Exception("can't cast to collection. provided depth is too big");
        }

        public static RecursiveMap<object> UnwrapField(this object o, string parentName, MappedAttribute a) // todo  - remove legacy
        {
            return a.Field.GetValue (o).Unwrap (parentName.JoinSheetNames(a.FrontType.SheetName), a.DimensionCount);
        }

        public static void AddContent(this object parent, Type type, IEnumerable<object> children)
        {
            var addMethod = AddMethodInfo.MakeGenericMethod (type);
            foreach (var element in children)
                addMethod.Invoke (parent, new[] {element});
        }

        public static void ForEachValue<T>(this RecursiveMap<T> map, Action<RecursiveMap<T>> action) // todo  - remove legacy
        {
            if (map is null ||  action is null) return;
            if (map.IsValue)
                action.Invoke (map);
            else
                foreach (var element in map.Collection)
                    element.ForEachValue (action);
        }
    }
}