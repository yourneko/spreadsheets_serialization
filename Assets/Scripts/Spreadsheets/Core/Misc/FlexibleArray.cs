using System;
using System.Collections.Generic;
using System.Linq;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    // I have no idea what I am doing, but it seems THIS is doing good.
    // It is sort of  Either<A,B>  monad wrapped in other stuff in attempt to reduce the entity types count.
    class FlexibleArray<T> 
    {
        private readonly T value;
        private readonly IEnumerable<FlexibleArray<T>> dimension;
        public readonly DimensionInfo dimensionInfo;

#region Values

        public bool IsValue { get; private set; }
        public bool IsDimension => !IsValue;

        /// <summary> Returns all contained values, starting from all values of the first child. </summary>
        public IEnumerable<T> GetValues() => IsValue ? 
                                             new[] { value } : 
                                             dimension.SelectMany (e => e.GetValues ());

        public T FirstValue => IsValue ? 
                               value : 
                               dimension.First ().FirstValue;

        /// <summary> Returns elements of near-most dimension as separate arrays. </summary>
        /// <remarks> Use only if  IsDimension == true. </remarks>
        public IEnumerable<FlexibleArray<T>> Enumerate()
        {
            UnityEngine.Debug.Assert (!IsValue);
            return dimension;
        }

#endregion
#region Constructors

        /// <summary> Given collection of arrays becomes the closest dimension of the new array. </summary>
        public FlexibleArray(IEnumerable<FlexibleArray<T>> _arrays, DimensionInfo _dimensionInfo)
        {
            dimension = _arrays;
            IsValue = false;
            dimensionInfo = _dimensionInfo;
        }

        /// <summary> Create a 0-dimensional array. This means it has only a single value of type <typeparamref name="T"/>. </summary>
        public FlexibleArray(T _value)
        {
            value = _value;
            IsValue = true;
            dimensionInfo = DimensionInfo.placeholder;
        }

        public static FlexibleArray<T> CreateIndexed(Func<int[], T> create, int[] indices, int dimensions, int firstIndex)
        {
            return dimensions == indices.Length ?
                   new FlexibleArray<T> (create.Invoke (indices)) :
                   new FlexibleArray<T>(GenerateDimension (create, indices, dimensions, firstIndex), DimensionInfo.placeholder);
        }

        private static IEnumerable<FlexibleArray<T>> GenerateDimension(Func<int[], T> create, int[] indices, int dimensions, int firstIndex)
        {
            int i = firstIndex;
            while (true)
            {
                var next = CreateIndexed (create, indices.Concat (new[] { i++ }).ToArray (), dimensions, firstIndex);
                if (next != null)
                    yield return next;
                else
                    yield break;
            }
        }

#endregion
#region Transformations

        /// <returns> New array of identical structure and order. </returns>
        // This method is called through Reflection using the name "Bind"
        public FlexibleArray<O> Bind<O>(Func<T, O> func)
        {
            return IsValue ?
                    new FlexibleArray<O> (func.Invoke (value)) :
                    new FlexibleArray<O> (dimension.Select (x => x.Bind (func)), dimensionInfo);
        }

        /// <summary> Expands each value of the array into a new dimension. The number of dimensions goes 1 up. </summary>
        public FlexibleArray<O> Expand<O>(Func<T, IEnumerable<O>> func, DimensionInfo newDimension)
        {
            return IsDimension ?
                   new FlexibleArray<O> (dimension.Select (x => x.Expand (func, newDimension)), dimensionInfo) :
                   func.Invoke (value).ToFlexArray (newDimension);
        }

        /// <summary> Associates a corresponding part of _other array to each value in given array. </summary>
        public FlexibleArray<(T, FlexibleArray<O>)> Associate<O>(FlexibleArray<O> _other)
        {
            return IsValue ?
                   new FlexibleArray<(T, FlexibleArray<O>)> ((value, _other)) :
                   new FlexibleArray<(T, FlexibleArray<O>)> (dimension.Parallel (_other.dimension)
                                                                      .Select (x => x.first.Associate (x.second)), 
                                                             DimensionInfo.placeholder);
        }

        /// <summary> Returns a new array with the same order order of values, but only with Values matching the condition. Empty dimensions are skipped. </summary>
        public FlexibleArray<T> Filter(Predicate<T> _condition)
        {
            return IsValue ?
                   new FlexibleArray<T> (value) :
                   new FlexibleArray<T> (dimension.Where (_condition.ApplyCondition)
                                                  .Select (x => x.Filter (_condition)), 
                                         dimensionInfo);
        }

        /// <summary> Returns a new array with the same values, but dimensions end whenever condition is false. </summary>
        public FlexibleArray<T> TakeWhile(Predicate<T> _condition)
        {
            return IsValue ?
                   new FlexibleArray<T> (value) :
                   new FlexibleArray<T> (dimension.TakeWhile (_condition.ApplyCondition)
                                                  .Select(x => x.TakeWhile(_condition)), 
                                         dimensionInfo);
        }

        // it is static to define the targeted array as FlexArray<FlexArray<T>>
        public static FlexibleArray<T> Simplify(FlexibleArray<FlexibleArray<T>> _array)
        {
            return _array.IsValue ?
                   _array.value :
                   new FlexibleArray<T> (_array.dimension.Select (Simplify), _array.dimensionInfo);
        }
        
#endregion
    }
}