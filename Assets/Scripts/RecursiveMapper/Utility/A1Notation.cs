using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper.Utility
{
    static class A1Notation
	{
        // In Google Spreadsheets notation, upper boundary of the range may be missing. It's interpreted as 'up to the big number'
        const int BIG_NUMBER = 999;
        const int LETTERS_COUNT = 26;

        public static (int x, int y) Read(string a1)
        {
            var letters = a1.Where (char.IsLetter).Select (char.ToUpperInvariant).Reverse ().ToArray ();
            var digits = a1.Where (char.IsDigit).Reverse ().ToArray ();
            return  (Evaluate (letters, 'A', LETTERS_COUNT),
                     Evaluate(digits, '0', 10) - 1);
        }

        public static string Write(int x, int y) => ToLetters (x) + (y + 1);

        static string ToLetters(int number)    // so bad
        {
            List<int> chars = new List<int> { number };
            while (chars.Last() >= LETTERS_COUNT)
            {
                chars.Add (chars.Last () / LETTERS_COUNT);
                chars[chars.Count - 2] -= chars[chars.Count - 1] * LETTERS_COUNT;
            }
            return new string(chars.Select (x => (char)('A' + x)).Reverse().ToArray ());
        }

        static int Evaluate(char[] digits, char zero, int @base) => digits.Any ()
                                                                        ? (int)digits.Select ((c, i) => (c - zero) * Math.Pow (@base, i)).Sum ()
                                                                        : BIG_NUMBER;
    }
}