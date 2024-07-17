using LMCore.AbstractClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiledDungeon
{
    public class TiledConfiguration : Singleton<TiledConfiguration>
    {
        [Header("Tile Classes")]
        public string BaseTileClass = "BaseTile";
        public string GrateClass = "Grate";
        public string ObstructionClass = "Obstruction";
        public string DoorClass = "Door";
        public string LadderClass = "Ladder";
        public string TeleporterClass = "Teleporter";
        public string SpinnerClass = "Spinner";
        public string RampClass = "Ramp";

        [Header("String Enums")]
        public string OrientationKey = "Orientation";
        public string WalkabilityKey = "Walkability";
        public string FlyabilityKey = "Flyability";
        public string AnchorKey = "Anchor";
        public string RotationKey = "Rotation";
        public string ElevationKey = "Elevation";
        public string DownDirectionKey = "DownDirection";
        public string InteractionKey = "Interaction";
        public string TransitionKey = "Transition";

        [Header("Booleans")]
        public string TrapKey = "Trap";
        public string ConusumesKeyKey = "ConsumesKey";

        [Header("Ints")]
        public string TeleporterIdProperty = "Wormhole";

        [Header("Strings")]
        public string KeyKey;

        [Header("Classes")]
        [Tooltip("Name of custom properties class that has boolean fields for Down, Up, North, West, East, South")]
        public string SidesClassKey = "Sides";

        [Header("Object Layer Items")]
        public string LockItem = "Lock";

    }
}
