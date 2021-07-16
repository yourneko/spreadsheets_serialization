using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SheetsIO
{
    static class ExtensionMethods
    {
        static readonly MethodInfo addMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.Instance | BindingFlags.Public);
        public static IOFieldAttribute GetIOAttribute(this FieldInfo field)
        {
            var attribute = (IOFieldAttribute)Attribute.GetCustomAttribute (field, typeof(IOFieldAttribute));
            if (attribute != null && !attribute.Initialized)
                attribute.CacheMeta (field);
            return attribute;
        }

        public static IOMetaAttribute GetIOAttribute(this Type type)
        {
            var attribute = (IOMetaAttribute)Attribute.GetCustomAttribute (type, typeof(IOMetaAttribute));
            if (attribute != null && !attribute.Initialized)
                attribute.CacheMeta (type);
            return attribute;
        }

        public static bool TryGetElement<T>(this IList<T> target, int index, out T result)
        {
            bool exists = (target?.Count ?? 0) > index;
            result = exists ? target[index] : default; // safe, index >= 0
            return exists;
        }
        
#region Writing 
        public static void ForEachChild(this object parent, IEnumerable<IOPointer> pointers, Action<IOPointer, object> action)
        {
            using var e = pointers.GetEnumerator();
            while (e.MoveNext() && parent.TryGetChild(e.Current, out var child))
                action.Invoke(e.Current, child);
        }

        static bool TryGetChild(this object parent, IOPointer p, out object child)
        {
            if (parent != null && p.Rank == 0)
            {
                child = p.Field.FieldInfo.GetValue(parent);
                return true;
            }
            if (parent is IList list && list.Count > p.Index)
            {
                child = list[p.Index];
                return true;
            }
            child = null;
            return !p.IsFreeSize;
        }
#endregion
#region Reading
        public static bool CreateChildren(this object parent, IEnumerable<IOPointer> pointers, Func<IOPointer, object, bool> func)
        { 
            foreach (var p in pointers)
                if (!func.Invoke(p, p.CreateObject(parent)) && !p.Optional) // todo: AddChild after testing (func.Invoke || Optional)
                    return p.IsFreeSize;
            return true;
        }

        public static object AddChild(this IOPointer p, object parent, object child) 
        {
            Debug.Log(p);
            if (p.Rank == 0)
                p.Field.FieldInfo.SetValue(parent, child);
            else if (parent is IList array)
                if (p.IsArray) array[p.Index] = child;
                else           array.Add(child);
            else
                addMethodInfo.MakeGenericMethod(p.Field.Types[p.Rank]).Invoke(parent, new[] {child});
            return child;
        }

        static object CreateObject(this IOPointer p, object o) => p.AddChild(o, p.IsArray
                                                                                    ? Array.CreateInstance(p.Field.Types[p.Rank + 1], p.Field.MaxCount(p.Rank))
                                                                                    : Activator.CreateInstance(p.TargetType));
#endregion
        
        public static IEnumerable<T> RepeatAggregated<T>(this T start, int max, Func<T, int, T> func)
        {
            int rank = max;
            var value = start;
            do yield return value;
            while(--rank >= 0 && (value = func.Invoke(value, rank)) != null);
        }
    }
}
