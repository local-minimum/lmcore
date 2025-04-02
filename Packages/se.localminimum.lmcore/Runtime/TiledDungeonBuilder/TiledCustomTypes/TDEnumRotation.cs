using LMCore.IO;
using LMCore.TiledImporter;
using UnityEngine;

namespace LMCore.TiledDungeon.Integration
{
    public enum TDEnumRotation
    {
        ClockWise,
        CounterClockWise,
        None,
        Unknown
    }
    public static class TDEnumRotationExtensions
    {
        public static TDEnumRotation Rotation(this TiledCustomProperties props, string name = "Rotation")
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Rotation without specifying the enum key");
                return TDEnumRotation.Unknown;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Rotation enum on key {name}, but it doesn't exist");
                return TDEnumRotation.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Rotation")
            {
                Debug.LogError($"Attempting to access Rotation enum on key {name}, but it is {stringEnum.TypeName}");
                return TDEnumRotation.Unknown;
            }

            switch (stringEnum.Value)
            {
                case "Clock-Wise":
                    return TDEnumRotation.ClockWise;
                case "Counter Clock-Wise":
                    return TDEnumRotation.CounterClockWise;
                case "None":
                    return TDEnumRotation.None;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Rotation");
                    return TDEnumRotation.Unknown;
            }
        }

        public static Movement AsMovement(this TDEnumRotation rotation)
        {
            switch (rotation)
            {
                case TDEnumRotation.ClockWise:
                    return Movement.YawCW;
                case TDEnumRotation.CounterClockWise:
                    return Movement.YawCCW;
                default:
                    return Movement.None;
            }
        }
    }
}
