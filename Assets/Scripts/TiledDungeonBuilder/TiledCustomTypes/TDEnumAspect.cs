using LMCore.TiledImporter;
using UnityEngine;

namespace TiledDungeon.Integration {
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
        public static TDEnumAspect Aspect(this TiledCustomProperties props, string name = "Aspect")
        {

            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Aspect without specifying the enum key");
                return TDEnumAspect.Unknown;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Aspect enum on key {name}, but it doesn't exist");
                return TDEnumAspect.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Aspect")
            {
                Debug.LogError($"Attempting to access Aspect enum on key {name}, but it is {stringEnum.TypeName}");
                return TDEnumAspect.Unknown;
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
                    return TDEnumAspect.Unknown;
            }
        }
    }
}
