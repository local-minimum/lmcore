namespace LMCore.Extensions
{
    public static class ScalarExtensions
    {
        /// <summary>
        /// Return sign of integer
        /// </summary>
        /// <returns>-1, 0, or 1</returns>
        public static int Sign(this int value)
        {
            if (value < 0) return -1;
            return value > 0 ? 1 : 0;
        }
    }
}