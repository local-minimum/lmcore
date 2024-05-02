using System.Collections.Generic;
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
    }
}