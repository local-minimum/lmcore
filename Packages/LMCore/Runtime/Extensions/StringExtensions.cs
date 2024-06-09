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
            if (str.Length <= length) return str ;
            if (!ellipsis ) return trimWhitepace ? str.Substring(0, length).TrimEnd() : str.Substring(0, length);

            return trimWhitepace ? $"{str.Substring(0, length - 1).TrimEnd()}…" : $"{str.Substring(0, length - 1)}…";

        }
    }
}
