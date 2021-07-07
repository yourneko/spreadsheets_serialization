using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    static class ArrayUtility
    {
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

        public static IEnumerable<(object obj, T data)> UnwrapArray<T>(this (object obj, T data) array, MapFieldAttribute f, int rank, Func<T, int, int, T> newT)
        {
            return rank > f.CollectionSize.Count// todo - maybe check number of elements in fixed sized collections
                       ? new[]{array}
                       : array.obj is ICollection c
                           ? c.Cast<object> ().Select ((e, i) => (e, newT(array.data, rank, i)).UnwrapArray (f, rank + 1, newT)).SelectMany (x => x)
                           : throw new Exception ();
        }

        public static V2Int GetScale(int count, int rank) => new V2Int ((int)Math.Pow (count, rank & 1), (int)Math.Pow (count, 1 - (rank & 1)));

        public static V2Int GetHalf(this V2Int target, int rank) => new V2Int ((1 - (rank & 1)) * target.X, (rank & 1) * target.Y);

    }
}