using System;
using System.Collections.Generic;

namespace RecursiveMapper
{
    public class FuncCache<TValue, TResult>
    {
        private readonly Func<TValue, TResult> f;
        private readonly Dictionary<TValue, TResult> dictionary;

        public FuncCache(Func<TValue, TResult> func)
        {
            f      = func ?? throw new NullReferenceException ();
            dictionary = new Dictionary<TValue, TResult> ();
        }

        public TResult Invoke(TValue parameter)
        {
            if (dictionary.ContainsKey (parameter))
                return dictionary[parameter];

            var result = f.Invoke (parameter);
            dictionary.Add (parameter, result);
            return result;
        }

        public void Clear() => dictionary.Clear ();
    }
}