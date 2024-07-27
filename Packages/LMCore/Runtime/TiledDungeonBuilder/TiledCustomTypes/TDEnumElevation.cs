using LMCore.TiledImporter;
using UnityEngine;

namespace LMCore.TiledDungeon.Integration
{
    public enum TDEnumElevation
    {
        Low,
        Middle, 
        High,
        Unknown
    }

    public static class TDEnumElevationExtensions
    {
        public static TDEnumElevation Elevation(this TiledCustomProperties props, string name = "Elevation")
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Elevation without specifying the enum key");
                return TDEnumElevation.Unknown;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Elevation enum on key {name}, but it doesn't exist");
                return TDEnumElevation.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Elevation")
            {
                Debug.LogError($"Attempting to access Elevation enum on key {name}, but it is {stringEnum.TypeName}");
                return TDEnumElevation.Unknown;
            }

            switch (stringEnum.Value)
            {
                case "High":
                    return TDEnumElevation.High;
                case "Middle":
                    return TDEnumElevation.Middle;
                case "Low":
                    return TDEnumElevation.Low;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Elevation");
                    return TDEnumElevation.Unknown;
            }
        }
    }
}
