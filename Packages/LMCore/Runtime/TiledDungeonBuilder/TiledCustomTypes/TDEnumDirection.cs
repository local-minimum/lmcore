using LMCore.Crawler;
using LMCore.TiledImporter;
using UnityEngine;


namespace LMCore.TiledDungeon.Integration
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
        public static readonly TDEnumDirection[] PlanarDirections = new TDEnumDirection[] { 
            TDEnumDirection.North,
            TDEnumDirection.South,
            TDEnumDirection.West,
            TDEnumDirection.East,
        };

        public static Direction AsDirection(this TDEnumDirection direction)
        {
            switch (direction)
            {
                case TDEnumDirection.North:
                    return LMCore.Crawler.Direction.North;
                case TDEnumDirection.South:
                    return LMCore.Crawler.Direction.South;
                case TDEnumDirection.West:
                    return LMCore.Crawler.Direction.West;
                case TDEnumDirection.East:
                    return LMCore.Crawler.Direction.East;
                case TDEnumDirection.Up:
                    return LMCore.Crawler.Direction.Up;
                case TDEnumDirection.Down:
                    return LMCore.Crawler.Direction.Down;
                default:
                    return LMCore.Crawler.Direction.None;
            }
        }

        public static TDEnumDirection FromDirection(Direction direction)
        {
            switch (direction)
            {
                case LMCore.Crawler.Direction.South:
                    return TDEnumDirection.South;
                case LMCore.Crawler.Direction.West:
                    return TDEnumDirection.West;
                case LMCore.Crawler.Direction.East:
                    return TDEnumDirection.East;
                case LMCore.Crawler.Direction.North:
                    return TDEnumDirection.North;
                case LMCore.Crawler.Direction.Up:
                    return TDEnumDirection.Up;
                case LMCore.Crawler.Direction.Down:
                    return TDEnumDirection.Down;
                case LMCore.Crawler.Direction.None:
                    return TDEnumDirection.None;
                default:
                    return TDEnumDirection.Unknown;
            }
        }

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
