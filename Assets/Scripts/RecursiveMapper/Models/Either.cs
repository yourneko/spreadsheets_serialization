namespace RecursiveMapper
{
    public class Either<TLeft, TRight>
    {
        public TLeft Left { get; }
        public TRight Right { get; }

        public bool IsLeft { get; }

        public bool IsRight => !IsLeft;

        public Either(TRight value)
        {
            IsLeft = false;
            Right  = value;
        }

        public Either(TLeft value)
        {
            Left   = value;
            IsLeft = true;
        }

        public static Either<TLeft, TRight> Create(TLeft value, TRight alternateValue)
        {
            return value is null
                       ? new Either<TLeft, TRight> (alternateValue)
                       : new Either<TLeft, TRight> (value);
        }
    }
}