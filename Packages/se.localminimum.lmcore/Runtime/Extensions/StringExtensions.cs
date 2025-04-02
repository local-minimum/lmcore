using System;

namespace LMCore.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Capitalize initial letter of string
        /// </summary>
        public static string Capitalize(this string str) =>
            $"{str.Substring(0, 1).ToUpper()}{str.Substring(1)}";

        /// <summary>
        /// Returns a string which final length is not longer than requested length.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <param name="ellipsis">If said to true adds ellipsis sign</param>
        /// <param name="trimWhitepace">If whitespace at the end should be trimmed</param>
        /// <returns></returns>
        public static string Truncate(this string str, int length, bool ellipsis = false, bool trimWhitepace = true)
        {
            if (str.Length <= length) return str;
            if (!ellipsis) return trimWhitepace ? str.Substring(0, length).TrimEnd() : str.Substring(0, length);

            return trimWhitepace ? $"{str.Substring(0, length - 1).TrimEnd()}…" : $"{str.Substring(0, length - 1)}…";

        }

        public static int LevenshteinDistance(this string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
            {
                return 0;
            }

            if (string.IsNullOrEmpty(a))
            {
                return b.Length;
            }

            if (string.IsNullOrEmpty(b))
            {
                return a.Length;
            }

            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];

            for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (int i = 1; i <= lengthA; i++)
            {
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = b[j - 1] == a[i - 1] ? 0 : 1;

                    distances[i, j] = Math.Min(
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost
                    );
                }
            }

            return distances[lengthA, lengthB];
        }

        /// <summary>
        /// If string is non-empty puts prefix and suffix around it.
        /// </summary>
        public static string DecorateNonEmpty(this string input, string prefix = "", string suffix = "") =>
            string.IsNullOrEmpty(input) ? input : $"{prefix}{input}{suffix}";

    }
}
