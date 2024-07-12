using LMCore.AbstractClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiledDungeon
{
    public class TiledConfiguration : Singleton<TiledConfiguration>
    {
        [Header("Tile Classes")]
        public string GrateClass = "Grate";
        public string ObstructionClass = "Obstruction";
        public string DoorClass = "Door";
        public string LadderClass = "Ladder";
        public string TeleporterClass = "Teleporter";
        public string SpinnerClass = "Spinner";

        [Header("String Enums")]
        public string OrientationKey = "Orientation";
        public string WalkabilityKey = "Walkability";
        public string FlyabilityKey = "Flyability";
        public string AnchorKey = "Anchor";
        public string RotationKey = "Rotation";

        [Header("Booleans")]
        public string TrapKey = "Trap";
    }
}
