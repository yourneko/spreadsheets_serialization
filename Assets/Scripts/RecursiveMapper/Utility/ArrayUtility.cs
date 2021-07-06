using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    static class ArrayUtility
    {
        private static readonly MethodInfo AddMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.Instance | BindingFlags.Public);

        public static void FindValidArrayIndices(this Predicate<int[]> validate, int count) // IMPORTANT: indices start from 1, not 0
        {
            var indices = Enumerable.Repeat (1, count + 1).ToArray (); // 0 element is a pointer to current value.
            while (indices[0] >= 0)
            {
                if (validate (indices.Skip(1).ToArray()))
                    indices[0] = indices.Length - 1;
                else
                    indices[indices[0]--] = 1; // todo - fix this infinite loop  by complete rewriting it.  don't go in until testing all values in current array
                indices[indices[0]] += 1;
            }
        }

        public static IEnumerable<(string name, object  obj)> ToCollection(this object o, string name, int repeats)
        {
            return repeats == 0
                       ? new[]{(name,  o)}
                       : o is ICollection c
                           ? repeats > 1
                                 ? c.Cast<object> ().SelectMany ((e, i) => ToCollection (e, $"{name} {i}", repeats - 1))
                                 : c.Cast<object> ().Select ((e, i) => ($"{name} {i}", e))
                           : throw new Exception ();
        }

        public static Action<object, object> AddContent(this Type type) => (o, e) => AddMethodInfo.MakeGenericMethod (type).Invoke (o, new[] {e});

    }
}