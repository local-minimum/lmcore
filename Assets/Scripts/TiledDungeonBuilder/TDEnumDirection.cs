using System.Collections;
using System.Collections.Generic;
using TiledImporter;
using UnityEngine;


namespace TiledDungeon
{
    public enum TDEnumDirection
    {
        None,
        North,
        South,
        West,
        East,
        Up,
        Down,
        Unknown
    }

    public static class TDEnumDirectionExtensions
    {
        public static TDEnumDirection Direction(this TiledCustomProperties props, string name = "Direction")
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Direction without specifying the enum key");
                return TDEnumDirection.Unknown;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Direction enum on key {name}, but it doesn't exist");
                return TDEnumDirection.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Direction")
            {
                Debug.LogError($"Attempting to access Direction enum on key {name}, but it is {stringEnum.TypeName}");
                return TDEnumDirection.Unknown;
            }

            switch (stringEnum.Value)
            {
                case "None":
                    return TDEnumDirection.None;
                case "North":
                    return TDEnumDirection.North;
                case "South":
                    return TDEnumDirection.South;
                case "West":
                    return TDEnumDirection.West;
                case "East":
                    return TDEnumDirection.East;
                case "Up":
                    return TDEnumDirection.Up;
                case "Down":
                    return TDEnumDirection.Down;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Direction");
                    return TDEnumDirection.Unknown;
            }
        }
    }
}
