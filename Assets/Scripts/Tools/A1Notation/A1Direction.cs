namespace Mimimi.Tools.A1Notation
{
    public enum A1Direction
    {
        /// <summary> Incorrect value, used by default. </summary>
        Undefined = 0,

        /// <summary> The direction down the column. The column coordinate is numeric in A1 format. </summary>
        Column = 'Y',

        /// <summary> The right hand direction along the row. The row coordinate is letters in A1 format. </summary>
        Row = 'X',
    }
}
