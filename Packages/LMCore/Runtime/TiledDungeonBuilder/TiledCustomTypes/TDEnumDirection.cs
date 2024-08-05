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
                    return Crawler.Direction.North;
                case TDEnumDirection.South:
                    return Crawler.Direction.South;
                case TDEnumDirection.West:
                    return Crawler.Direction.West;
                case TDEnumDirection.East:
                    return Crawler.Direction.East;
                case TDEnumDirection.Up:
                    return Crawler.Direction.Up;
                case TDEnumDirection.Down:
                    return Crawler.Direction.Down;
                default:
                    return Crawler.Direction.None;
            }
        }

        public static TDEnumDirection FromDirection(Direction direction)
        {
            switch (direction)
            {
                case Crawler.Direction.South:
                    return TDEnumDirection.South;
                case Crawler.Direction.West:
                    return TDEnumDirection.West;
                case Crawler.Direction.East:
                    return TDEnumDirection.East;
                case Crawler.Direction.North:
                    return TDEnumDirection.North;
                case Crawler.Direction.Up:
                    return TDEnumDirection.Up;
                case Crawler.Direction.Down:
                    return TDEnumDirection.Down;
                case Crawler.Direction.None:
                    return TDEnumDirection.None;
                default:
                    return TDEnumDirection.Unknown;
            }
        }

        public static TDEnumDirection Direction(this TiledCustomProperties props, string name, TDEnumDirection defaultValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Direction without specifying the enum key");
                return defaultValue;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                return defaultValue;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Direction")
            {
                Debug.LogError($"Attempting to access Direction enum on key {name}, but it is {stringEnum.TypeName}");
                return defaultValue;
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
                    Debug.LogError($"'{stringEnum.Value}' is not a known Direction, using {defaultValue}");
                    return defaultValue;
            }
        }

        public static TDEnumDirection Direction(this TiledCustomProperties props, string name = "Direction")
        {

            var direction = props.Direction(name, TDEnumDirection.Unknown);

            if (direction == TDEnumDirection.Unknown)
            {
                Debug.LogError($"Attempting to access Direction enum on key {name}, probably it doesn't exist");
            }

            return direction;
        }
    }
}
