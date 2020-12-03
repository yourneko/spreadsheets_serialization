using System;
using System.Collections.Generic;
using System.Linq;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    // I have no idea what I am doing, but it seems THIS is doing good.
    // It is sort of  Either<A,B>  monad wrapped in other stuff in attempt to reduce the entity types count.
    public class FlexibleArray<T> 
    {
        private readonly T value;
        private readonly IEnumerable<FlexibleArray<T>> dimension;

        public readonly DimensionInfo dimensionInfo;

#region Values

        public bool IsValue { get; private set; }
        public bool IsDimension => !IsValue;

        /// <summary> Returns all contained values, starting from all values of the first child. </summary>
        public IEnumerable<T> GetValues() => (IsValue) ? new[] { value } : dimension.SelectMany (e => e.GetValues ());

        public T FirstValue => IsValue ? value : dimension.First ().FirstValue;

        /// <summary> Returns elements of near-most dimension as separate arrays. </summary>
        public IEnumerable<FlexibleArray<T>> Enumerate()
        {
            UnityEngine.Debug.Assert (!IsValue);
            return dimension;
        }

#endregion
#region Create

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
        }

        /// <summary> Expands each value of the array into a new dimension. The number of dimensions goes 1 up. </summary>
        public FlexibleArray<O> Expand<O>(Func<T, ICollection<O>> expandFunction, DimensionInfo _expandedDimensionInfo)
        {
            return IsDimension ?
                   new FlexibleArray<O> (dimension.Select (x => x.Expand (expandFunction, _expandedDimensionInfo)), dimensionInfo) :
                   expandFunction.Invoke (value).ToFlexArray (_expandedDimensionInfo);
        }

        /// <summary> 
        /// Collapses each collection of the deep-most dimension to a single object created with the given function. 
        /// The number of dimensions is reduced by 1.
        /// </summary>
        /// <remarks> Works only for arrays with flat bottom dimension. </remarks>
        public FlexibleArray<O> Collapse<O>(Func<IEnumerable<T>, O> collapseFunction)
        {
            UnityEngine.Debug.Assert (!IsValue);
            return dimension.All (x => x.IsValue) ?
                    new FlexibleArray<O> (collapseFunction.Invoke (GetValues ())) :
                    new FlexibleArray<O> (dimension.Select (x => x.Collapse (collapseFunction)), dimensionInfo);
        }

        /// <returns> New array of identical structure and order. </returns>
        // This method is called through Reflection using the name "Bind"
        public FlexibleArray<O> Bind<O>(Func<T, O> function)
        {
            return IsValue ?
                    new FlexibleArray<O> (function.Invoke (value)) :
                    new FlexibleArray<O> (dimension.Select (x => x.Bind (function)), dimensionInfo);
        }

        /// <summary> Combines two arrays of exactly same shape to new array with the same shape. </summary>
        public FlexibleArray<R> Combine<O, R>(FlexibleArray<O> _other, Func<T, O, R> combineFunction)
        {
            return IsValue ?
                   new FlexibleArray<R> (combineFunction.Invoke (value, _other.value)) :
                   new FlexibleArray<R> (_arrays: dimension.Parallel (_other.dimension)
                                                           .Select (ab => ab.first.Combine (ab.second, combineFunction)),
                                         _dimensionInfo: dimensionInfo);
        }

        /// <summary> Associates a corresponding part of _other array to each value in given array. </summary>
        public FlexibleArray<(T, FlexibleArray<O>)> Associate<O>(FlexibleArray<O> _other)
        {
            return (IsValue) ?
                 new FlexibleArray<(T, FlexibleArray<O>)> ((value, _other)) :
                 new FlexibleArray<(T, FlexibleArray<O>)> (dimension.Parallel (_other.dimension)
                                                                    .Select (x => x.first.Associate (x.second)), 
                                                           new DimensionInfo(Tools.A1Notation.A1Direction.Row));
        }

        // it is static to define the targeted array as FlexArray<FlexArray<T>>
        public static FlexibleArray<T> Simplify(FlexibleArray<FlexibleArray<T>> _array)
        {
            return _array.IsValue ?
                   _array.value :
                   new FlexibleArray<T> (_array.dimension.Select (x => Simplify (x)), _array.dimensionInfo);
        }

        public FlexibleArray<T> Filter(Predicate<T> _condition)
        {
            return IsValue ?
                   new FlexibleArray<T> (value) :
                   new FlexibleArray<T> (dimension.Where (_condition.ExtendCondition)
                                                  .Select (x => x.Filter (_condition)), 
                                         dimensionInfo);
        }
        
#endregion
    }
}
