using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    static class ExtensionMethods
    {
        static readonly MethodInfo addMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.Instance | BindingFlags.Public);

        public static MapFieldAttribute MapAttribute(this FieldInfo field)
        {
            var attribute = (MapFieldAttribute)Attribute.GetCustomAttribute (field, typeof(MapFieldAttribute));
            if (attribute != null && !attribute.Initialized)
                attribute.CacheMeta (field);
            return attribute;
        }

        public static MapClassAttribute MapAttribute(this Type type)
        {
            var attribute = (MapClassAttribute)Attribute.GetCustomAttribute (type, typeof(MapClassAttribute));
            if (!attribute.Initialized)
                attribute.CacheMeta (type);
            return attribute;
        }
        
        public static int GetFieldSortOrder(this MapFieldAttribute f) => f.Field.GetCustomAttribute<MapPlacementAttribute>()?.SortOrder
                                                                      ?? (f.Rank == 0 || (f.CollectionSize?.Count ?? 0) == f.Rank
                                                                              ? 1000
                                                                              : int.MaxValue + f.Rank - 2);

        public static Type GetEnumeratedType(this Type t) => t.GetTypeInfo ().GetInterfaces ()
                                                              .FirstOrDefault (x => x.IsGenericType && x.GetGenericTypeDefinition () == typeof(IEnumerable<>))
                                                             ?.GetGenericArguments ()[0];

        public static object AddChild(this MapFieldAttribute field, object parent, int rank, object child = null)
        {
            var childToAdd = child ?? Activator.CreateInstance (field.ArrayTypes[rank]);
            if (rank == 0)
                field.Field.SetValue (parent, childToAdd);
            else
                addMethodInfo.MakeGenericMethod (field.ArrayTypes[rank]).Invoke (parent, new[] {childToAdd});
            return childToAdd;
        }

        public static IEnumerable<(object obj, T data)> UnwrapArray<T>(this (object obj, T data) array, MapFieldAttribute f, int rank, Func<T, int, int, T> newT)
        {
            return rank > f.CollectionSize.Count
                       ? new[]{array}
                       : array.obj is ICollection c
                           ? c.Cast<object> ().Select ((e, i) => (e, newT(array.data, rank, i)).UnwrapArray (f, rank + 1, newT)).SelectMany (x => x)
                           : throw new Exception ($"Object was expected to be a collection, but it isn't. (Field {f.Field.Name}, rank {rank})");
        }

        public static string JoinSheetNames(this string parent, string child) => child.Contains ("{0}") ? string.Format (child, parent) : $"{parent} {child}";

        public static string GetReadRange(this MapClassAttribute type, string sheet, string a2First) =>
            $"'{sheet}'!{a2First}:{SpreadsheetsUtility.WriteA1 (type.Size.Add (SpreadsheetsUtility.ReadA1 (a2First)))}";
        
        public static V2Int GetHalf(this V2Int target, int rank) => new V2Int ((1 - (rank & 1)) * target.X, (rank & 1) * target.Y);
    }
}
