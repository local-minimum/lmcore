using Codice.CM.Common.Purge;
using LMCore.AbstractClasses;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TiledConfiguration : Singleton<TiledConfiguration, TiledConfiguration>
    {
        [Header("Tile Classes")]
        public string BaseTileClass = "BaseTile";
        public string IllusoryTileClass = "IllusoryWall";
        public string GrateClass = "Grate";
        public string ObstructionClass = "Obstruction";
        public string DoorClass = "Door";
        public string LadderClass = "Ladder";
        public string LadderTopClass = "Ladder-Top";
        public string TeleporterClass = "Teleporter";
        public string SpinnerClass = "Spinner";
        public string RampClass = "Ramp";
        public string StairsClass = "Stairs";
        public string TrapDoorClass = "TrapDoor";
        public string WallButtonClass = "Button";
        public string AlcoveClass = "Alcove";
        public string FenceClass = "Fence";
        public string NPCClass = "NPC";
        public string ShooterClass = "Shooter";
        public string WallSpikeTrapClass = "WallSpikes";
        public string SpikeTrapClass = "Spikes";
        public string PressurePlateClass = "PressurePlate";
        public string PillarClass = "Pillar";
        public string PedistalClass = "Pedistal";
        public string ChestClass = "Chest";
        public string FirePlaceClass = "Fireplace";
        public string MovingPlatformClass = "MovingPlatform";

        [Header("String Enums")]
        public string OrientationKey = "Orientation";
        public string TraversalAxisKey = "TraversalAxis";
        public string WalkabilityKey = "Walkability";
        public string FlyabilityKey = "Flyability";
        public string AnchorKey = "Anchor";
        public string DirectionKey = "Direction";
        public string RotationKey = "Rotation";
        public string ElevationKey = "Elevation";
        public string DownDirectionKey = "DownDirection";
        public string InteractionKey = "Interaction";
        public string TransitionKey = "Transition";
        public string LoopKey = "Loop";

        [Header("Booleans")]
        public string TrapKey = "Trap";
        public string ConusumesKeyKey = "ConsumesKey";
        public string OpenKey = "Open";
        public string ClaimAlwaysAlignedKey = "ClaimAlwaysAligned";

        [Header("Ints")]
        public string TeleporterIdProperty = "Wormhole";
        public string LayerElevationKey = "Elevation";

        [Header("Floats")]
        public string VelocityKey = "Velocity";
        public string PauseKey = "Pause";

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
        public string ObjRelayClass = "Relay";
        public string ObjPassivePlatformClass = "PassivePlatform";
        public string ObjContainerClass = "Container";
        public string ObjVariantStyleKey = "Style";
        public string ObjGroupKey = "Group";
        public string ObjManagedByGroupKey = "ManagedByGroup";
        public string ObjToggleEffectKey = "ToggleEffect";
        public string ObjPhaseKey = "Phase";
        public string ObjSequenceStarter = "Start";
        public string ObjNextPhaseKey = "NextPhase";
        public string ObjRepeatableKey = "Repeatable";
        public string ObjAutomaticUnsetKey = "AutomaticUnset";
        public string ObjAutomaticKey = "Automatic";
        public string ObjSpikelessKey = "Spikeless";
        public string ObjDelayTimeKey = "Delay";
        public string ObjIdKey = "Id";
        public string ObjBelongsToKey = "BelongsTo";
        public string ObjCapacityKey = "Capacity";
        public string ObjItemPatternKey = "Item%";
        public string ObjItemStackSizePatternKey = "Item%-StackSize";
        public string ObjBidirectionalKey = "Bidirectional";
        public string ObjRestKey = "Rest";
        public string ObjInDirectionPatternKey = "In%";
        public string ObjOutDirectionPatternKey = "Out%";

        [Header("Other")]
        public string LayoutLayerPrefix = "dungeon";
    }
}
