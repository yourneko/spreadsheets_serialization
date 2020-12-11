using System;
using System.Linq;
using System.Collections.Generic;

namespace Mimimi.Tools
{
    public class MultiIndexNumerator<T>
    {
        private int[] indices;
        private int startingValue;
        private Func<int[], T> create;
        private Predicate<T> predicate;

        private int maxIndex;
        private T current;

        private int FirstPositiveIndex => maxIndex - indices.Reverse ().TakeWhile (x => x == startingValue).Count ();

        private void ResetUpTo(int index)
        {
            for (int i = maxIndex; i > index; i--)
                indices[i] = startingValue;
        }

        private IEnumerable<T> Count()
        {
            while (true)
            {
                current = create.Invoke (indices);
                if (predicate.Invoke (current))
                {
                    yield return current;
                    indices[maxIndex] += 1;
                }
                else
                {
                    int next = FirstPositiveIndex - 1;
                    if (next < 0)
                        yield break;
                    indices[next] += 1;
                    ResetUpTo (next);
                }
            }
        }

        private MultiIndexNumerator() { }

        public static IEnumerable<T> Enumerate<T>(int _indices, int _startingValue, Func<int[], T> _create, Predicate<T> _predicate)
        {
            var numerator = new MultiIndexNumerator<T> ()
            {
                indices = Enumerable.Repeat (_startingValue, _indices).ToArray (),
                startingValue = _startingValue,
                predicate = _predicate,
                maxIndex = _indices - 1,
            };
            return numerator.Count ();

        }
    }
}