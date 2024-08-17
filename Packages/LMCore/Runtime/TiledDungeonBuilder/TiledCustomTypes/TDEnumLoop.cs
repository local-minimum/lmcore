using LMCore.TiledImporter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.Integration
{
    public enum TDEnumLoop { None, Bounce, Wrap, Unknown }

    public static class TDEnumLoopExtensions
    {
        public static TDEnumLoop Loop(this TiledCustomProperties props, string name = "Loop")
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Loop without specifying the enum key");
                return TDEnumLoop.Unknown;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Loop enum on key {name}, but it doesn't exist");
                return TDEnumLoop.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Loop")
            {
                Debug.LogError($"Attempting to access Loop enum on key {name}, but it is {stringEnum.TypeName}");
                return TDEnumLoop.Unknown;
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
