using UnityEngine;

namespace LMCore.EntitySM.State
{
    public enum StateType
    {
        /// <summary>
        /// No decided state
        /// </summary>
        None,

        /// <summary>
        /// Following a set path or pattern in "home" area
        /// </summary>
        Patrolling,

        /// <summary>
        /// Alert stationary activity in "home" area
        /// </summary>
        Guarding,

        /// <summary>
        /// Stationary or near stationary activity with low 
        /// awareness of surroundings in "home" area
        /// </summary>
        Loitering,

        /// <summary>
        /// Walking between random points in "home" area
        /// </summary>
        Ambling,

        /// <summary>
        /// Approaching or looking for player with no 
        /// commitment to agression
        /// </summary>
        Investigating,

        /// <summary>
        /// Upon abandoning investigation outside "home" area
        /// deciding to get back to "home" area.
        /// 
        /// Not likely to return to investigating
        /// </summary>
        Retracing,

        /// <summary>
        /// Searching for player and committed to agression
        /// </summary>
        Hunting,

        /// <summary>
        /// Giving up on hunting but eager to hunt again
        /// </summary>
        Retreating,

        /// <summary>
        /// Within attack range and attacking player
        /// </summary>
        Fighting,

        /// <summary>
        /// Avoiding player, either directly upon detecting them
        /// or as a result of prior fighting.
        /// Unless pushed away from "home" or into a corner, will
        /// not commit to hunting / fighting
        /// </summary>
        Flee,
    }

    public static class StateTypeExtensions
    {
        public static StateType From(string stateType)
        {
            switch (stateType.Trim().ToLower())
            {
                case null:
                case "":
                case "none":
                    return StateType.None;
                case "patroll":
                case "patrol":
                case "patrolling":
                    return StateType.Patrolling;
                case "guard":
                case "guarding":
                    return StateType.Guarding;
                case "loiter":
                case "loitering":
                    return StateType.Loitering;
                case "amble":
                case "ambling":
                    return StateType.Ambling;
                case "investigate":
                case "investigating":
                    return StateType.Investigating;
                case "retrace":
                case "retracing":
                    return StateType.Retracing;
                case "hunt":
                case "hunting":
                    return StateType.Hunting;
                case "retreat":
                case "retreating":
                    return StateType.Retreating;
                case "fight":
                case "fighting":
                    return StateType.Fighting;
                case "fleeing":
                case "flee":
                    return StateType.Flee;
                default:
                    Debug.LogError($"'{stateType}' is not a known enemy-state");
                    return StateType.None;
            }
        }
    }
}
