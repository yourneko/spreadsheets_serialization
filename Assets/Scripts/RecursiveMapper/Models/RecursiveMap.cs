using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class RecursiveMap<T> : Either<T, IEnumerable<RecursiveMap<T>>>, IEnumerable<T>
    {
        public readonly DimensionInfo DimensionInfo;

        private IEnumerable<T> Elements => IsLeft
                                               ? new[] {Left}
                                               : Right.SelectMany (element => element.Elements);

        public RecursiveMap(IEnumerable<RecursiveMap<T>> value, DimensionInfo info)
            : base (value)
        {
            DimensionInfo = info;
        }

        public RecursiveMap(T value, DimensionInfo info)
            : base (value)
        {
            DimensionInfo = info;
        }

        public RecursiveMap<TResult> Cast<TResult>(Func<T, TResult> func)
        {
            return IsLeft
                       ? new RecursiveMap<TResult> (func.Invoke (Left), DimensionInfo.Copy())
                       : new RecursiveMap<TResult> (Right.Select (element => element.Cast (func)), DimensionInfo.Copy());
        }

        public RecursiveMap<TResult> Cast<TResult>(Func<T, DimensionInfo, RecursiveMap<TResult>> func)
        {
            return IsLeft
                       ? func.Invoke (Left, this.DimensionInfo)
                       : new RecursiveMap<TResult> (Right.Select (element => element.Cast (func)), DimensionInfo.Copy());
        }

        public IEnumerator<T> GetEnumerator() => Elements.GetEnumerator ();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator ();
    }
}