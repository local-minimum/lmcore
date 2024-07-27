using LMCore.TiledImporter;
using UnityEngine;

namespace LMCore.TiledDungeon.Integration
{
    public enum TDEnumTransition { 
        None,
        Entry,
        Exit,
        Intermediary,
        EntryAndExit,
        Unknown,
    }

    public static class TDEnumTransitionExtensions
    {
        public static bool HasEntry(this TDEnumTransition transition) =>
            transition == TDEnumTransition.Entry ||
            transition == TDEnumTransition.EntryAndExit;

        public static bool HasExit(this TDEnumTransition transition) =>
            transition == TDEnumTransition.Exit ||
            transition == TDEnumTransition.EntryAndExit;

        public static TDEnumTransition Transition(this TiledCustomProperties props, string name = "Transition")
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Cannot construct a Transition without specifying the enum key");
                return TDEnumTransition.Unknown;
            }

            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Transition enum on key {name}, but doesn't exist");
                return TDEnumTransition.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Transition") {
                Debug.LogError($"Attempting to access Transition enum on key {name}, but it is of type {stringEnum.TypeName}");
                return TDEnumTransition.Unknown;
            }

            switch (stringEnum.Value)
            {
                case "None":
                    return TDEnumTransition.None;
                case "Entry":
                    return TDEnumTransition.Entry;
                case "Exit":
                    return TDEnumTransition.Exit;
                case "Intermediary":
                    return TDEnumTransition.Intermediary;
                case "EntryAndExit":
                    return TDEnumTransition.EntryAndExit;
                default:
                    Debug.LogError($"'{stringEnum.Value}' is not a known Transition");
                    return TDEnumTransition.Unknown;
            }
        }
    }
}
