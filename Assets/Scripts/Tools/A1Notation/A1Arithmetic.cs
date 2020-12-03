using System.Collections.Generic;
using System;
using System.Linq;

namespace Mimimi.Tools.A1Notation
{
    public static class A1Arithmetic
    {
#region Ranges
                        
        public static bool Intersects(this A1Range _range, A1Range _other) => Math.Max (_range.first.x, _other.first.x) <= Math.Min (_range.last.x, _other.last.x) &&
                                                                              Math.Max (_range.first.y, _other.first.y) <= Math.Min (_range.last.y, _other.last.y);

        /// <remarks> To be sure the ranges are actually intersecting, call method <code>A1Range.Intersects(A1Range)</code> </remarks>
        public static A1Range Intersection(this A1Range _range, A1Range _other) => new A1Range (_range.first.Max (_other.first),
                                                                                                _range.last.Max (_other.last));

        public static A1Range TranslateTo(this A1Range _range, A1Point _newFirstPoint)
        {
            return new A1Range (_newFirstPoint,
                                new A1Point (_range.last.x + (_newFirstPoint.x - _range.first.x),
                                             _range.last.y + (_newFirstPoint.y - _range.first.y)));
        }

#endregion
#region Points

        public static int X(this A1Point _point) => _point.x;
        public static int Y(this A1Point _point) => _point.y;
        public static bool NotNegative(this A1Point _point) => _point.x >= 0 && _point.y >= 0;
        public static A1Point Min(this IEnumerable<A1Point> _points) => new A1Point (_points.Min (X), _points.Min (Y));
        public static A1Point Max(this IEnumerable<A1Point> _points) => new A1Point (_points.Max (X), _points.Max (Y));
        public static A1Point Max(this A1Point _point, A1Point _other) => new A1Point (Math.Max(_point.x, _other.x), 
                                                                                        Math.Max(_point.y, _other.y));
        public static A1Point Min(this A1Point _point, A1Point _other) => new A1Point (Math.Min (_point.x, _other.x),
                                                                                        Math.Min (_point.y, _other.y));

        public static int Coordinate(this A1Point _point, A1Direction _direction)
        {
            switch (_direction)
            {
                case A1Direction.Row:      return _point.x;
                case A1Direction.Column:   return _point.y;
                default: throw new Exception ();
            }
        }

        public static A1Line CreateLine(this A1Point _point, A1Direction _direction) => new A1Line (_direction, _point.Coordinate (_direction.Opposite()));

        public static A1Point Translate(this A1Point _point, A1Direction _direction, int _length)
        {
            switch (_direction)
            {
                case A1Direction.Row:      return new A1Point(_point.x + _length, _point.y);
                case A1Direction.Column:   return new A1Point (_point.x, _point.y + _length);
                default:                   return _point;
            }
        }

#endregion
#region Lines

        public static A1Point PointOnLine(this A1Line _line, int _position)
        {
            return new A1Point (_line.x ?? _position, 
                                _line.y ?? _position);
        }

        public static A1Point GetProjection(this A1Line _line, A1Point _point, int _offset = 0)
        {
            return new A1Point(_line.x ?? _point.x + _offset,
                               _line.y ?? _point.y + _offset);
        }

#endregion

        public static A1Direction Opposite(this A1Direction _direction)
        {
            switch (_direction)
            {
                case A1Direction.Row:      return A1Direction.Column;
                case A1Direction.Column:   return A1Direction.Row;
                default:                   return _direction;
            }
        }
    }
}