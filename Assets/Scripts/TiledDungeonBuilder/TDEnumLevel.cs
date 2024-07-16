using System.Collections;
using System.Collections.Generic;
using TiledImporter;
using UnityEngine;

namespace TiledDungeon
{
    public enum TDEnumLevel
    {
        Low,
        Middle, 
        High,
        Unknown
    }

    public static class TDEnumLevelExtensions
    {
        public static TDEnumLevel Level(this TiledCustomProperties props, string name = "Level")
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Level without specifying the enum key");
                return TDEnumLevel.Unknown;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Level enum on key {name}, but it doesn't exist");
                return TDEnumLevel.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Level")
            {
                Debug.LogError($"Attempting to access Level enum on key {name}, but it is {stringEnum.TypeName}");
                return TDEnumLevel.Unknown;
            }

            switch (stringEnum.Value)
            {
                case "High":
                    return TDEnumLevel.High;
                case "Middle":
                    return TDEnumLevel.Middle;
                case "Low":
                    return TDEnumLevel.Low;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Level");
                    return TDEnumLevel.Unknown;
            }
        }
    }
}
