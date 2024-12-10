using LMCore.Crawler;
using LMCore.TiledImporter;
using UnityEngine;

namespace LMCore.TiledDungeon.Integration
{
    public enum TDEnumOrientation
    {
        None,
        Vertical,
        Horizontal,
        Transverse,
        Unknown
    }

    public static class TDEnumOrientationExtensions
    {
        public static TDEnumOrientation Inverse(this TDEnumOrientation orientation)
        {
            switch (orientation)
            {
                case TDEnumOrientation.Vertical:
                    return TDEnumOrientation.Horizontal;
                case TDEnumOrientation.Horizontal:
                    return TDEnumOrientation.Vertical;
                default:
                    return orientation;
            }
        }

        public static DirectionAxis AsAxis(this TDEnumOrientation orientation)
        {
            switch (orientation)
            {
                case TDEnumOrientation.Vertical:
                    return DirectionAxis.NorthSouth;
                case TDEnumOrientation.Horizontal:
                    return DirectionAxis.WestEast;
                case TDEnumOrientation.Transverse:
                    return DirectionAxis.UpDown;
                default:
                    return DirectionAxis.None;
            }
        }

        public static TDEnumOrientation Orientation(this TiledCustomProperties props, string name, TDEnumOrientation defaultValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Orientation without specifying the enum key");
                return defaultValue;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                return defaultValue;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Orientation") {
                Debug.LogError($"Attempting to access Orientation enum on key {name}, but it is of type {stringEnum.TypeName}");
                return defaultValue;
            }

            switch (stringEnum.Value)
            {
                case "None":
                    return TDEnumOrientation.None;
                case "Vertical":
                    return TDEnumOrientation.Vertical;
                case "Horizontal":
                    return TDEnumOrientation.Horizontal;
                case "Transverse":
                    return TDEnumOrientation.Transverse;
                default:
                    return defaultValue;
            }

        }

        public static TDEnumOrientation Orientation(this TiledCustomProperties props, string name = "Orientation")
        {
            var orientation = Orientation(props, name, TDEnumOrientation.Unknown);

            if (orientation == TDEnumOrientation.Unknown)
            {
                Debug.LogError($"Attempting to access Orientation enum on key {name}, but probably doesn't exist");
            }
            return orientation;
        }
    }
}
