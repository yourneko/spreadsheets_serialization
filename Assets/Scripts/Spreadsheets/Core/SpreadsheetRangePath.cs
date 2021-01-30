using System;
using System.Collections.Generic;
using System.Linq;
using Mimimi.Tools.A1Notation;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    class SpreadsheetRangePath
    {
        private const string OPENING_SEQUENCE = "##";

        private readonly System.Text.StringBuilder sb;
        private readonly Stack<DimensionLine> dimensions;

        private static bool IsCorrectChar(char c) => Enum.IsDefined (typeof (PathAction), (int)c);

        public SpreadsheetRangePath()
        {
            sb = new System.Text.StringBuilder (OPENING_SEQUENCE);
            dimensions = new Stack<DimensionLine> ();
            // Adding line zero. It will contain all required info to define a range
            dimensions.Push (new DimensionLine (A1Point.zero, A1Direction.Row));
        }

#region Export result

        public A1Point NextPoint => dimensions.Peek ().Next;

        public string GetPath() => sb.ToString ();
        public A1Range GetRange() => new A1Range (A1Point.zero, dimensions.Pop ().FarPoint);

#endregion
#region Create sequence while mapping

        // when dimension opens, the next point does not change
        public void OpenDimension(A1Direction _direction)
        {
            dimensions.Push (new DimensionLine (NextPoint, _direction));
            sb.Append ((char)(_direction == A1Direction.Row ? PathAction.OpenX : PathAction.OpenY));
        }

        // removes the top dimension. uses the FarPoint of it to adjust the next point of the previous one
        public void CloseLastDimension()
        {
            DimensionLine closed = dimensions.Pop ();
            dimensions.Peek ().MovePointer (closed);
            sb.Append ((char)(closed.line.direction == A1Direction.Row ? PathAction.CloseX : PathAction.CloseY));
        }

        // moves the next point 1 position forward 
        public void WriteTitle()
        {
            dimensions.Peek ().MovePointer ();
            sb.Append ((char)(PathAction.Header));
        }

        // moves the next point 1 position forward 
        public void WriteValue()
        {
            dimensions.Peek ().MovePointer ();
            sb.Append ((char)(PathAction.Value));
        }

#endregion
#region Import sequence and read a sheet

        public static IEnumerable<PathAction> ReadActionsSequence(string _actionSequence) // can go with coordinates
        {
            foreach (var c in _actionSequence.ToCharArray ().Where (IsCorrectChar))
                yield return (PathAction)c;
        }

        public static FlexibleArray<string> ReadSheet(IList<IList<object>> _sheet)
        {
            SpreadsheetRangePath path = new SpreadsheetRangePath ();
            Stack<List<FlexibleArray<string>>> stack = new Stack<List<FlexibleArray<string>>> ();
            stack.Push (new List<FlexibleArray<string>> ());

            foreach (var a in ReadActionsSequence ((string)_sheet[0][0]))
            {
                switch (a)
                {
                    case PathAction.OpenX:
                    case PathAction.OpenY:
                        path.OpenDimension (a == PathAction.OpenX ? A1Direction.Row : A1Direction.Column);
                        stack.Push (new List<FlexibleArray<string>> ());
                        break;
                    case PathAction.CloseX:
                    case PathAction.CloseY:
                        path.CloseLastDimension ();
                        var list = stack.Pop ();
                        var dimensionInfo = new DimensionInfo (a == PathAction.CloseX ? A1Direction.Row : A1Direction.Column);
                        stack.Peek ().Add (new FlexibleArray<string> (list, dimensionInfo));
                        break;
                    case PathAction.Value:
                        var value = _sheet[path.NextPoint.y][path.NextPoint.x];
                        stack.Peek ().Add (new FlexibleArray<string> (value.ToString ()));
                        path.WriteValue ();
                        break;
                    case PathAction.Header:
                        path.WriteTitle ();
                        break;
                }
            }
            UnityEngine.Debug.Assert (stack.Count == 1);
            return stack.Pop ()[0];
        }

#endregion
    }
}