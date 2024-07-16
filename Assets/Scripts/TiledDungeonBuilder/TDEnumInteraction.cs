using System.Collections;
using System.Collections.Generic;
using TiledImporter;
using UnityEngine;

namespace TiledDungeon
{
    public enum TDEnumInteraction 
    {
        Open,
        Closed,
        Interactable,
        Locked,
        Obstruction,
        Unknown
    }

    public static class TDEnumInteractionExtensions
    {
        public static TDEnumInteraction Interaction(this TiledCustomProperties props, string name = "Interaction")
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Interaction without specifying the enum key");
                return TDEnumInteraction.Unknown;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Interaction enum on key {name}, but it doesn't exist");
                return TDEnumInteraction.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Interaction")
            {
                Debug.LogError($"Attempting to access Interaction enum on key {name}, but it is {stringEnum.TypeName}");
                return TDEnumInteraction.Unknown;
            }

            switch (stringEnum.Value)
            {
                case "Open":
                    return TDEnumInteraction.Open;
                case "Closed":
                    return TDEnumInteraction.Closed;
                case "Interactable":
                    return TDEnumInteraction.Interactable;
                case "Locked":
                    return TDEnumInteraction.Locked;
                case "Obstruction":
                    return TDEnumInteraction.Obstruction;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Interaction");
                    return TDEnumInteraction.Unknown;
            }
        }
    }

}
