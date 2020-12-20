using System.Collections.Generic;
using System.Linq;

namespace Mimimi.Tools.A1Notation
{
    public static class A1Notation
	{
        const int LETTERS_COUNT = 26;
        const int BIG_LETTER_INDEX = 999;
        const int BIG_DIGIT_INDEX = 999;

        /// <summary> 
        /// In Google Spreadsheets notation, numbers or letters or both can be missing in closing boundary.
        /// This can be roughly counted as 'Infinity' or some big number.
        /// </summary>
        public static (int x, int y) Read(string _A1)
        {
            char[] digits = _A1.Reverse ()
                               .TakeWhile (char.IsDigit)
                               .ToArray ();
            char[] letters = _A1.Reverse ()
                                .Skip (digits.Length)
                                .Select(char.ToUpperInvariant)
                                .ToArray ();
            return  (letters.Length > 0 ? Evaluate (letters, 'A', LETTERS_COUNT) : BIG_LETTER_INDEX, 
                     digits.Length > 0 ? Evaluate(digits, '0', 10) - 1 : BIG_DIGIT_INDEX);
        }

        public static string Write(int x, int y) => ToLetters (x) + ToDigits (y);

        public static string ToLetters(int _number)
        {
            List<int> chars = new List<int> () { _number };
            while (chars.Last() >= LETTERS_COUNT)
            {
                chars.Add (chars.Last () / LETTERS_COUNT);
                chars[chars.Count - 2] -= chars[chars.Count - 1] * LETTERS_COUNT;
            }
            return new string(chars.Select (x => (char)('A' + x)).Reverse().ToArray ());
        }

        public static string ToDigits(int _number) => (_number + 1).ToString ();

        private static int Evaluate(char[] _sequence, char _zero, int _countBase)
        {
            int currentRank = 1;
            int current = 0;
            foreach (var c in _sequence)
            {
                UnityEngine.Debug.Assert (c - _zero < _countBase && c  >= _zero, $"Invalid character {c} in sequence");
                current += (c - _zero) * currentRank;
                currentRank *= _countBase;
            }
            return current;
        }
    }
}