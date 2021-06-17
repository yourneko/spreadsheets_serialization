using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper.Utility
{
    static class A1Notation
	{
        const int LETTERS_COUNT = 26;
        const int BIG_LETTER_INDEX = 999;
        const int BIG_DIGIT_INDEX = 999;

        /// <summary> 
        /// In Google Spreadsheets notation, numbers or letters or both can be missing in closing boundary.
        /// This can be roughly counted as 'Infinity' or some big number.
        /// </summary>
        public static (int x, int y) Read(string a1)
        {
            char[] digits = a1.Reverse ()
                               .TakeWhile (char.IsDigit)
                               .ToArray ();
            char[] letters = a1.Reverse ()
                                .Skip (digits.Length)
                                .Select(char.ToUpperInvariant)
                                .ToArray ();
            return  (letters.Length > 0 ? Evaluate (letters, 'A', LETTERS_COUNT) : BIG_LETTER_INDEX, 
                     digits.Length > 0 ? Evaluate(digits, '0', 10) - 1 : BIG_DIGIT_INDEX);
        }

        public static string Write(int x, int y) => ToLetters (x) + ToDigits (y);

        private static string ToLetters(int number)
        {
            List<int> chars = new List<int> { number };
            while (chars.Last() >= LETTERS_COUNT)
            {
                chars.Add (chars.Last () / LETTERS_COUNT);
                chars[chars.Count - 2] -= chars[chars.Count - 1] * LETTERS_COUNT;
            }
            return new string(chars.Select (x => (char)('A' + x)).Reverse().ToArray ());
        }

        private static string ToDigits(int number) => (number + 1).ToString ();

        private static int Evaluate(IEnumerable<char> sequence, char zero, int countBase)
        {
            int currentRank = 1;
            int current = 0;
            foreach (var c in sequence)
            {
                current += (c - zero) * currentRank;
                currentRank *= countBase;
            }
            return current;
        }
    }
}