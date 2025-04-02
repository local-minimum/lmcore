using LMCore.TiledImporter;
using UnityEngine;

namespace LMCore.TiledDungeon.Integration
{
    public enum TDEnumAspect
    {
        Always,
        Transient,
        Never,
        Sometimes,
        Unknown
    }

    public static class TDEnumAspecExtensions
    {
        public static TDEnumAspect Aspect(this TiledCustomProperties props, string name, TDEnumAspect defaultValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Aspect without specifying the enum key");
                return defaultValue;
            }

            if (props == null || !props.StringEnums.ContainsKey(name))
            {
                return defaultValue;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Aspect")
            {
                Debug.LogError($"Attempting to access Aspect enum on key {name}, but it is {stringEnum.TypeName}");
                return defaultValue;
            }

            switch (stringEnum.Value)
            {
                case "Always":
                    return TDEnumAspect.Always;
                case "Transient":
                    return TDEnumAspect.Transient;
                case "Never":
                    return TDEnumAspect.Never;
                case "Sometimes":
                    return TDEnumAspect.Sometimes;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Aspect");
                    return defaultValue;
            }
        }

        public static TDEnumAspect Aspect(this TiledCustomProperties props, string name = "Aspect")
        {
            var aspect = props.Aspect(name, TDEnumAspect.Unknown);

            if (aspect == TDEnumAspect.Unknown)
            {
                Debug.LogError($"Attempting to access Aspect enum on key {name}, probably it doesn't exist");
            }

            return aspect;
        }
    }
}
