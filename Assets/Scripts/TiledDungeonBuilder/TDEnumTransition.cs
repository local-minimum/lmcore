using System.Collections;
using System.Collections.Generic;
using TiledImporter;
using UnityEngine;

namespace TiledDungeon
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
        public static TDEnumTransition Transition(this TiledCustomProperties props, string name = "Transition")
        {
            if (!props.StringEnums.ContainsKey(name))
            {
                Debug.LogError($"Attempting to access Transition enum on key {name}, but doesn't exist");
                return TDEnumTransition.Unknown;
            }

            var stringEnum = props.StringEnums[name];
            if (stringEnum.TypeName != "Transition") {
                Debug.Log($"Attempting to access Transition enum on key {name}, but it is of type {stringEnum.TypeName}");
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
                    return TDEnumTransition.Unknown;
            }
        }
    }
}
