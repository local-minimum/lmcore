using LMCore.Crawler;
using UnityEngine;
using LMCore.TiledDungeon.Integration;
using UnityEditor.Graphs;

namespace LMCore.TiledDungeon.Style
{
    public class DungeonStyle : AbsDungeonStyle
    {
        [SerializeField]
        bool warnIfPrefabIsMissing;

        [Header("Cube Side")]
        [SerializeField]
        GameObject Wall;

        [SerializeField]
        GameObject Floor;

        [SerializeField]
        GameObject Celing;

        [SerializeField]
        GameObject IllusoryWall;

        [SerializeField]
        GameObject IllusoryCeiling;

        [SerializeField]
        GameObject IllusoryFloor;

        [SerializeField]
        GameObject Fence;

        [SerializeField]
        GameObject Alcove;

        [SerializeField, Tooltip("Replacing a floor")]
        GameObject TrapDoor;

        [SerializeField]
        GameObject Shooter;

        [SerializeField]
        GameObject WallSpikes;

        [SerializeField]
        GameObject PressurePlate;

        [SerializeField]
        GameObject Spikes;

        [Header("Objects")]
        [SerializeField]
        GameObject Ladder;
        
        [SerializeField]
        GameObject LadderTop;

        [SerializeField]
        GameObject Teleporter;

        [SerializeField]
        GameObject Chest;

        [Header("Doors and Obstructions")]
        [SerializeField]
        GameObject ButtonDoor;

        [SerializeField]
        GameObject KeyDoor;

        [SerializeField]
        GameObject Grate;

        [SerializeField]
        GameObject Obstruction;

        [SerializeField]
        GameObject Pillar;

        [SerializeField]
        GameObject Pedistal;

        [Header("Ramps")]
        [SerializeField]
        GameObject LowerRamp;

        [SerializeField]
        GameObject MiddleRamp;

        [SerializeField]
        GameObject UpperRamp;

        [SerializeField]
        GameObject Stairs;

        [SerializeField]
        GameObject WallButton;

        protected string PrefixLogMessage(string message) => $"Dungeon Style {name}: {message}";

        protected void LogErrorUnkownId(string classId) => Debug.LogError(PrefixLogMessage($"Don't know what a '{classId}' is."));
        protected void LogWarningUnknownDirection(string classId, Direction direction, string variant) {
            if (warnIfPrefabIsMissing) Debug.LogWarning($"Unknown direction {direction} of '{classId}' variant '{variant}'");
        }
        
        public override GameObject Get(Transform parent, string classId, string variant = null)
        {
            if (classId == TiledConfiguration.InstanceOrCreate().PressurePlateClass && PressurePlate != null) { return Instantiate(PressurePlate, parent); }
            if (classId == TiledConfiguration.InstanceOrCreate().PillarClass && Pillar != null) { return Instantiate(Pillar, parent); }
            if (classId == TiledConfiguration.InstanceOrCreate().PedistalClass && Pedistal != null) { return Instantiate(Pedistal, parent); }
            if (classId == TiledConfiguration.InstanceOrCreate().ChestClass && Chest != null) { return Instantiate(Chest, parent); }

            LogErrorUnkownId(classId);
            return null;
        }

        private GameObject InstantiateWithRotation(Transform parent, GameObject prefab, Direction direction)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, parent);
            go.transform.rotation = direction.AsQuaternion(Direction.Down, true);
            return go;
        }

        private GameObject InstantiateCubeSide(Transform parent, Direction direction, string variant)
        {
            if (direction.IsPlanarCardinal())
            {
                return InstantiateWithRotation(parent, Wall, direction);
            }

            if (direction == Direction.Up && Celing != null) { return Instantiate(Celing, parent); }
            if (direction == Direction.Down && Floor != null) { return Instantiate(Floor, parent); }

            LogWarningUnknownDirection(TiledConfiguration.InstanceOrCreate().BaseTileClass, direction, variant);
            return null;
        }


        public override GameObject Get(Transform parent, string classId, Direction direction, string variant = null)
        {
            if (classId == TiledConfiguration.InstanceOrCreate().BaseTileClass) return InstantiateCubeSide(parent, direction, variant);
            if (classId == TiledConfiguration.InstanceOrCreate().LadderClass) return InstantiateWithRotation(parent, Ladder, direction);
            if (classId == TiledConfiguration.InstanceOrCreate().LadderTopClass) return InstantiateWithRotation(parent, LadderTop, direction);
            if (classId == TiledConfiguration.InstanceOrCreate().WallButtonClass) return InstantiateWithRotation(parent, WallButton, direction);
            if (classId == TiledConfiguration.InstanceOrCreate().AlcoveClass) return InstantiateWithRotation(parent, Alcove, direction);
            if (classId == TiledConfiguration.InstanceOrCreate().FenceClass) return InstantiateWithRotation(parent, Fence, direction);
            if (classId == TiledConfiguration.InstanceOrCreate().ShooterClass) return InstantiateWithRotation(parent, Shooter, direction);
            if (classId == TiledConfiguration.InstanceOrCreate().WallSpikeTrapClass)
            {
                if (direction.IsPlanarCardinal()) return InstantiateWithRotation(parent, WallSpikes, direction);
                LogWarningUnknownDirection(classId, direction, variant);
                return null;
            }

            if (classId == TiledConfiguration.InstanceOrCreate().StairsClass) return InstantiateWithRotation(parent, Stairs, direction);
            if (classId == TiledConfiguration.InstanceOrCreate().SpikeTrapClass) return InstantiateWithRotation(parent, Spikes, direction);
            if (classId == TiledConfiguration.InstanceOrCreate().PillarClass) { return InstantiateWithRotation(parent, Pillar, direction); }
            if (classId == TiledConfiguration.InstanceOrCreate().PedistalClass) { return InstantiateWithRotation(parent, Pedistal, direction); }
            if (classId == TiledConfiguration.InstanceOrCreate().ChestClass) { return InstantiateWithRotation(parent, Chest, direction); }
            if (classId == TiledConfiguration.InstanceOrCreate().IllusoryTileClass)
            {
                if (direction.IsPlanarCardinal()) return InstantiateWithRotation(parent, IllusoryWall, direction);
                if (direction == Direction.Up) return Instantiate(IllusoryCeiling, parent);
                if (direction == Direction.Down) return Instantiate(IllusoryFloor, parent);
                LogWarningUnknownDirection(classId, direction, variant);
                return null;
            }

            LogErrorUnkownId(classId);
            return null;
        }

        public override GameObject Get(Transform parent, string classId, string fallbackClassId, Direction direction, string variant = null)
        {
            return Get(parent, classId, direction, variant) ?? Get(parent, fallbackClassId, direction, variant);
        }

        private GameObject InstantiateWithOrientation(Transform parent, GameObject prefab, TDEnumOrientation orientation)
        {
            if (prefab == null) return null;

            var go = Instantiate(prefab, parent);
            if (orientation == TDEnumOrientation.Horizontal) return go;

            go.transform.rotation = Direction.East.AsQuaternion(Direction.Down);

            return go;
        }

        public override GameObject Get(Transform parent, string classId, TDEnumOrientation orientation, string variant = null)
        {
            if (classId == TiledConfiguration.InstanceOrCreate().GrateClass) return InstantiateWithOrientation(parent, Grate, orientation);
            if (classId == TiledConfiguration.InstanceOrCreate().ObstructionClass) return InstantiateWithOrientation(parent, Obstruction, orientation);
            if (classId == TiledConfiguration.InstanceOrCreate().TrapDoorClass) return InstantiateWithOrientation(parent, TrapDoor, orientation);

            LogErrorUnkownId(classId);
            return null;
        }
        public override GameObject Get(Transform parent, string classId, TDEnumOrientation orientation, TDEnumInteraction interaction, string variant = null)
        {
            if (classId == TiledConfiguration.InstanceOrCreate().DoorClass)
            {
                switch (interaction)
                {
                    case TDEnumInteraction.Locked:
                        return InstantiateWithOrientation(parent, KeyDoor, orientation);
                    case TDEnumInteraction.Closed:
                    case TDEnumInteraction.Open:
                    case TDEnumInteraction.Interactable:
                        return InstantiateWithOrientation(parent, ButtonDoor, orientation);
                }

                Debug.LogError(PrefixLogMessage($"Don't know what a {interaction} '{classId}' is."));
                return null;
            }

            LogErrorUnkownId(classId);
            return null;
        }

        private GameObject InstantiateRamp(Transform parent, TDEnumElevation elevation)
        {
            switch (elevation)
            {
                case TDEnumElevation.Low:
                    return LowerRamp == null ? null : Instantiate(LowerRamp, parent);
                case TDEnumElevation.Middle:
                    return MiddleRamp == null ? null : Instantiate(MiddleRamp, parent);
                case TDEnumElevation.High:
                    return UpperRamp == null ? null : Instantiate(UpperRamp, parent);
            }

            Debug.LogError(PrefixLogMessage($"Don't know what a '{elevation}' ramp is."));
            return null;
        }

        private GameObject InstantiateRamp(Transform parent, TDEnumElevation elevation, Direction downDirection)
        {
            var go = InstantiateRamp(parent, elevation);
            if (go == null) return go;

            if (downDirection == Direction.North) return go;

            go.transform.rotation = downDirection.Inverse().AsQuaternion(Direction.Down);
            return go;
        }

        public override GameObject Get(Transform parent, string classId, TDEnumElevation elevation, Direction direction, string variant = null)
        {
            if (classId == TiledConfiguration.InstanceOrCreate().RampClass) return InstantiateRamp(parent, elevation, direction);
           
            LogErrorUnkownId(classId);
            return null;
        }


        public override GameObject Get(Transform parent, string classId, TDEnumTransition transition, string variant = null)
        {
            if (classId == TiledConfiguration.InstanceOrCreate().TeleporterClass)
            {
                if (transition == TDEnumTransition.Entry || transition == TDEnumTransition.EntryAndExit)
                {
                    if (Teleporter == null) { return null; }
                    return Instantiate(Teleporter, parent);
                }
                return null;
            }

            LogErrorUnkownId(classId);
            return null;
        }
    }
}
