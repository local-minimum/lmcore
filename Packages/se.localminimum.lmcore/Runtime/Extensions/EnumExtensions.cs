using System;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Enumerate all set flag values of enum value
        /// </summary>
        public static IEnumerable<T> AllFlags<T>(this T flags) where T : Enum
        {
            foreach (Enum value in Enum.GetValues(typeof(T)))
            {
                if (flags.HasFlag(value)) yield return (T)value;
            }
        }

        /// <summary>
        /// If value any of the submitted options.
        /// 
        /// Note: It doesn't check for set flags (see Enum.HasAnyFlag extension)
        /// </summary>
        public static bool Either<T>(this T value, params T[] options) where T : Enum =>
            options.Contains(value);

        public static bool HasAllFlags<T>(this T value, params T[] flags) where T : Enum =>
            flags.All(flag => value.HasFlag(flag));
        public static bool HasAnyFlag<T>(this T value, params T[] flags) where T : Enum =>
            flags.Any(flag => value.HasFlag(flag));
    }
}
