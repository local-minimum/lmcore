using LMCore.AbstractClasses;
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
        public string TrapDoorClass = "TrapDoor";
        public string WallButtonClass = "Button";
        public string AlcoveClass = "Alcove";
        public string WallSpikeTrapClass = "WallSpikes";
        public string SpikeTrapClass = "Spikes";
        public string PressurePlateClass = "PressurePlate";

        [Header("String Enums")]
        public string OrientationKey = "Orientation";
        public string TraversalAxisKey = "Traversal";
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
        public string OpenKey = "Open";

        [Header("Ints")]
        public string TeleporterIdProperty = "Wormhole";

        [Header("Strings")]
        public string KeyKey = "Key";

        [Header("Classes")]
        [Tooltip("Name of custom properties class that has boolean fields for Down, Up, North, West, East, South")]
        public string SidesClassKey = "Sides";

        [Header("Object Layer Items")]
        public string ObjLockItemClass = "Lock";
        public string ObjInitialClass = "Initial";
        public string ObjToggleGroupClass = "ToggleGroup";
        public string ObjSequencerGroupClass = "Sequence";
        public string ObjActuatorClass = "Actuator";
        public string ObjVariantStyleKey = "Style";
        public string ObjGroupKey = "Group";
        public string ObjPhaseKey = "Phase";
        public string ObjSequenceStarter = "Start";
        public string ObjNextPhaseKey = "NextPhase";
        public string ObjRepeatableKey = "Repeatable";
        public string ObjAutomaticKey = "Automatic";
        public string ObjSpikelessKey = "Spikeless";
        public string ObjDelayTimeKey = "Delay";
    }
}
