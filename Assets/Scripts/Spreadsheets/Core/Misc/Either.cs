namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class Either<A, B>
    {
        public A Left { get; private set; }
        public B Right { get; private set; }

        public bool IsLeft { get; private set; }

        public bool IsRight => !IsLeft;

        public Either(B _value)
        {
            IsLeft = false;
            Right = _value;
        }

        public Either(A _value)
        {
            Left = _value;
            IsLeft = true;
        }

        public static Either<A, B> Create(A _value, B _alternateValue)
        {
            return _value == null ? new Either<A, B> (_alternateValue) : new Either<A, B> (_value);
        }
    }
}