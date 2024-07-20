using LMCore.Crawler;
using System.Collections;
using System.Collections.Generic;
using TiledImporter;
using UnityEngine;

namespace TiledDungeon.Integration
{
    public enum TDEnumOrientation
    {
        None,
        Vertical,
        Horizontal,
        Unknown
    }

    public static class TDEnumOrientationExtensions
    {
        public static DirectionAxis AsAxis(this TDEnumOrientation orientation)
        {
            switch (orientation)
            {
                case TDEnumOrientation.Vertical:
                    return DirectionAxis.NorthSouth;
                case TDEnumOrientation.Horizontal:
                    return DirectionAxis.WestEast;
                default:
                    return DirectionAxis.None;
            }
        }

        public static TDEnumOrientation Orientation(this TiledCustomProperties props, string name = "Orientation")
        {

            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Orientation without specifying the enum key");
                return TDEnumOrientation.Unknown;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Orientation enum on key {name}, but doesn't exist");
                return TDEnumOrientation.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Orientation") {
                Debug.LogError($"Attempting to access Orientation enum on key {name}, but it is of type {stringEnum.TypeName}");
                return TDEnumOrientation.Unknown;
            }

            switch (stringEnum.Value)
            {
                case "None":
                    return TDEnumOrientation.None;
                case "Vertical":
                    return TDEnumOrientation.Vertical;
                case "Horizontal":
                    return TDEnumOrientation.Horizontal;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Orientation");
                    return TDEnumOrientation.Unknown;
            }
        }
    }
}
