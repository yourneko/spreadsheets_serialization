using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class RecursiveMap<T>
    {
        public T Value { get; }
        public IEnumerable<RecursiveMap<T>> Collection { get; }
        public Meta Meta { get; }
        public bool IsValue { get; }
        public bool IsCollection => !IsValue;

        public RecursiveMap(IEnumerable<RecursiveMap<T>> collection, Meta meta)
        {
            Meta       = meta;
            Collection = collection;
            IsValue    = false;
        }

        public RecursiveMap(T value, Meta meta)
        {
            Meta    = meta;
            Value   = value;
            IsValue = true;
        }

        public RecursiveMap<TResult> Cast<TResult>(Func<T, Meta, RecursiveMap<TResult>> func)
        {
            return IsValue
                       ? func.Invoke (Value, Meta)
                       : new RecursiveMap<TResult> (Collection.Select (element => element.Cast (func)), Meta);
        }
    }
}