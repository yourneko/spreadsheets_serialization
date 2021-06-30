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

        public RecursiveMap<T2> Cast<T2>(Func<T, Meta, RecursiveMap<T2>> func) => IsValue
                                                                                      ? func.Invoke (Value, Meta)
                                                                                      : new RecursiveMap<T2> (Collection.Select (e => e.Cast (func)), Meta);
    }
}