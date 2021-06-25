using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class RecursiveMap<T> : Either<T, IEnumerable<RecursiveMap<T>>>
    {
        public readonly Meta Meta;

        public RecursiveMap(IEnumerable<RecursiveMap<T>> value, Meta meta)
            : base (value)
        {
            Meta = meta;
        }

        public RecursiveMap(T value, Meta meta)
            : base (value)
        {
            Meta = meta;
        }

        public RecursiveMap<TResult> Cast<TResult>(Func<T, Meta, RecursiveMap<TResult>> func)
        {
            return IsLeft
                       ? func.Invoke (Left, this.Meta)
                       : new RecursiveMap<TResult> (Right.Select (element => element.Cast (func)), this.Meta);
        }
    }
}