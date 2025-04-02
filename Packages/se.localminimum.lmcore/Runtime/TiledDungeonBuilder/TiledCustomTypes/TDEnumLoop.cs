using LMCore.TiledImporter;
using UnityEngine;

namespace LMCore.TiledDungeon.Integration
{
    public enum TDEnumLoop { None, Bounce, Wrap, Unknown }

    public static class TDEnumLoopExtensions
    {
        public static TDEnumLoop Loop(this TiledCustomProperties props, string name = "Loop")
        {
            var loop = Loop(props, name, TDEnumLoop.Unknown);

            if (loop == TDEnumLoop.Unknown)
            {
                Debug.LogError($"Could not parse a loop by using key '{name}'");
            }

            return loop;
        }

        public static TDEnumLoop Loop(this TiledCustomProperties props, string name, TDEnumLoop defaultValue = TDEnumLoop.Unknown)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Loop without specifying the enum key");
                return defaultValue;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                return defaultValue;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Loop")
            {
                Debug.LogWarning($"Attempting to access Loop enum on key {name}, but it is {stringEnum.TypeName}");
                return defaultValue;
            }

            switch (stringEnum.Value)
            {
                case "None":
                    return TDEnumLoop.None;
                case "Bounce":
                    return TDEnumLoop.Bounce;
                case "Wrap":
                    return TDEnumLoop.Wrap;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Loop");
                    return TDEnumLoop.Unknown;
            }

        }
    }
}
