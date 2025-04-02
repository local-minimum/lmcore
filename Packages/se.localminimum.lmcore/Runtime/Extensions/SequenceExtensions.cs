using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Extensions
{
    public static class SequenceExtensions
    {
        /// <summary>
        /// Returns nth item or last
        /// </summary>
        /// <param name="index">sought index</param>
        public static T GetNthOrLast<T>(this T[] sequence, int index) => sequence[Mathf.Min(index, sequence.Length - 1)];

        /// <summary>
        /// Returns nth item or last
        /// </summary>
        /// <param name="index">sought index</param>
        public static T GetNthOrLast<T>(this List<T> sequence, int index) => sequence[Mathf.Min(index, sequence.Count - 1)];

        /// <summary>
        /// Returns nth item or default value
        /// </summary>
        /// <param name="index">sought index</param>
        public static T GetNthOrDefault<T>(this T[] sequence, int index, T defaultValue) => index >= sequence.Length ? defaultValue : sequence[index];

        /// <summary>
        /// Returns nth item or default value
        /// </summary>
        /// <param name="index">sought index</param>
        public static T GetNthOrDefault<T>(this List<T> sequence, int index, T defaultValue) => index >= sequence.Count ? defaultValue : sequence[index];

        /// <summary>
        /// Returns nth item wrapping around to first if longer than sequence
        /// </summary>
        /// <param name="index">sought index</param>
        public static T GetWrappingNth<T>(this T[] sequence, int index) => sequence[index % sequence.Length];

        /// <summary>
        /// Returns nth item wrapping around to first if longer than sequence
        /// </summary>
        /// <param name="index">sought index</param>
        public static T GetWrappingNth<T>(this List<T> sequence, int index) => sequence[index % sequence.Count];

        /// <summary>
        /// Returns random item in sequence
        /// </summary>
        public static T GetRandomElement<T>(this T[] sequence) => sequence[Random.Range(0, sequence.Length)];

        /// <summary>
        /// Returns random item in sequence
        /// </summary>
        public static T GetRandomElement<T>(this List<T> sequence) => sequence[Random.Range(0, sequence.Count)];

        /// <summary>
        /// Returns random item in sequence
        /// </summary>
        public static T GetRandomElementOrDefault<T>(this T[] sequence)
        {
            var n = sequence?.Length ?? 0;
            if (n == 0) return default(T);

            return sequence[Random.Range(0, n)];
        }

        /// <summary>
        /// Returns random item in sequence
        /// </summary>
        public static T GetRandomElementOrDefault<T>(this List<T> sequence)
        {
            var n = sequence?.Count ?? 0;
            if (n == 0) return default(T);

            return sequence[Random.Range(0, n)];
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> sequence)
        {
            return sequence.OrderBy(_ => Random.value);
        }

        /// <summary>
        /// Return first value equal or greater than start value not present in sequence
        /// </summary>
        /// <param name="sequence">The known values</param>
        /// <param name="startValue">Lowest allowed value, default 0</param>
        /// <returns></returns>
        public static int FirstUnusedValue(this IEnumerable<int> sequence, int startValue = 0)
        {
            int nextId = startValue;
            int keyIndex = 0;
            var keys = sequence.OrderBy(k => k).ToList();
            int nKeys = keys.Count;

            while (true)
            {
                if (keyIndex < nKeys)
                {
                    var val = keys[keyIndex];
                    if (nextId < val)
                    {
                        return nextId;
                    }
                    else if (nextId == val)
                    {
                        nextId++;
                    }
                    keyIndex++;
                    continue;
                }

                return nextId;
            }
        }
    }
}