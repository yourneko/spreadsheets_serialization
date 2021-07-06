using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class Chunked<T> : IEnumerable<IList<T>> where T : class
    {
        private readonly ChunkEnumerator<T> e;

        public Chunked(IEnumerable<T> source, int chunkSize)
        {
            e = new ChunkEnumerator<T> (source, chunkSize);
        }

        public IEnumerator<IList<T>> GetEnumerator() => e;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator ();

        private readonly struct ChunkEnumerator<T> : IEnumerator<IList<T>> where T : class
        {
            private readonly T[] array;
            private readonly IEnumerator<T> e;

            public ChunkEnumerator(IEnumerable<T> source, int chunkSize)
            {
                array = new T[chunkSize];
                e     = source.GetEnumerator ();
            }

            public bool MoveNext()
            {
                if (array[array.Length - 1] is null)
                    return false;
                for (int i = 0; i < array.Length; i++)
                    array[i] = e.MoveNext () ? e.Current : null;
                return array[0] != null;
            }

            public IList<T> Current => array.ToArray ();
            public void Reset() => e.Reset ();
            object IEnumerator.Current => Current;
            public void Dispose() => e.Dispose ();
        }
    }
}