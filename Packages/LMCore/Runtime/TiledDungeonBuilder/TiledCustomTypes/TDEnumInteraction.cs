using LMCore.TiledImporter;
using UnityEngine;

namespace LMCore.TiledDungeon.Integration
{
    public enum TDEnumInteraction 
    {
        Open,
        Closed,
        Interactable,
        Locked,
        Obstruction,
        Automatic,
        Unknown
    }

    public static class TDEnumInteractionExtensions
    {
        public static bool Obstructing(this TDEnumInteraction interaction) =>
            interaction == TDEnumInteraction.Closed || 
            interaction == TDEnumInteraction.Locked || 
            interaction == TDEnumInteraction.Obstruction;
            
        public static TDEnumInteraction InteractionOrDefault(this TiledCustomProperties props, string name = "Interaction", TDEnumInteraction defaultValue = TDEnumInteraction.Unknown)
        {
            if (!props.StringEnums.ContainsKey(name)) { return defaultValue; }
            return props.Interaction(name);
        }

        public static TDEnumInteraction Interaction(this TiledCustomProperties props, string name = "Interaction") =>
            props.Interaction(name, TDEnumInteraction.Unknown);

        public static TDEnumInteraction Interaction(this TiledCustomProperties props, string name, TDEnumInteraction defaultValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Interaction without specifying the enum key");
                return defaultValue;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Interaction enum on key {name}, but it doesn't exist");
                return defaultValue;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Interaction")
            {
                Debug.LogError($"Attempting to access Interaction enum on key {name}, but it is {stringEnum.TypeName}");
                return defaultValue;
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
                case "Automatic":
                    return TDEnumInteraction.Automatic;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Interaction");
                    return defaultValue;
            }
        }

    }

}
