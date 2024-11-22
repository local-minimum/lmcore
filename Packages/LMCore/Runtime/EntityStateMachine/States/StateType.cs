namespace LMCore.EntitySM.State
{
    public enum StateType
    {
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
}
