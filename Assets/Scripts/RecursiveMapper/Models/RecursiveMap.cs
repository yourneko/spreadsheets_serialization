using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class RecursiveMap<T> // todo  - remove legacy
    {
        public T Value { get; }
        public IEnumerable<RecursiveMap<T>> Collection { get; }
        public string Name { get; }
        public bool IsValue { get; }

        public RecursiveMap(T value, string name)
        {
            Name    = name;
            Value   = value;
            IsValue = true;
        }

        public RecursiveMap(IEnumerable<RecursiveMap<T>> collection, string name)
        {
            Name       = name;
            Collection = collection;
            IsValue    = false;
        }

        public RecursiveMap<T2> Cast<T2>(Func<T, string, RecursiveMap<T2>> func) => IsValue
                                                                                      ? func.Invoke (Value, Name)
                                                                                      : new RecursiveMap<T2> (Collection.Select (e => e.Cast (func)), Name);
    }
}