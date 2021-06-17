using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class RecursiveMap<T> : Either<T, IEnumerable<RecursiveMap<T>>>
    {
        public readonly DimensionInfo DimensionInfo;

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
                       ? new RecursiveMap<TResult> (func.Invoke (Left), this.DimensionInfo)
                       : new RecursiveMap<TResult> (Right.Select (element => element.Cast (func)), this.DimensionInfo);
        }

        public RecursiveMap<TResult> Expand<TResult>(Func<T, IEnumerable<TResult>> func, DimensionInfo info)
        {
            return IsLeft
                       ? new RecursiveMap<TResult> (func.Invoke (Left).Select (x => new RecursiveMap<TResult> (x, info)), this.DimensionInfo)
                       : new RecursiveMap<TResult> (Right.Select (element => element.Expand (func, info)), this.DimensionInfo);
        }
    }
}