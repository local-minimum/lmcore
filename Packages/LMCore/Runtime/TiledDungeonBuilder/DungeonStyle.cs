using LMCore.Crawler;
using UnityEngine;
using LMCore.TiledDungeon.Integration;

namespace LMCore.TiledDungeon
{
    public class DungeonStyle : AbsDungeonStyle
    {
        [Header("Cube Side")]
        [SerializeField]
        GameObject Wall;

        [SerializeField]
        GameObject Floor;

        [SerializeField]
        GameObject Celing;

        [SerializeField]
        GameObject Alcove;

        [SerializeField, Tooltip("Replacing a floor")]
        GameObject TrapDoor;

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
        GameObject WallButton;
        
        public override GameObject Get(Transform parent, string classId, string variant = null)
        {
            if (classId == TiledConfiguration.instance.PressurePlateClass && PressurePlate != null) { return Instantiate(PressurePlate, parent); }
            if (classId == TiledConfiguration.instance.PillarClass && Pillar != null) { return Instantiate(Pillar, parent); }
            if (classId == TiledConfiguration.instance.PedistalClass && Pedistal != null) { return Instantiate(Pedistal, parent); }
            if (classId == TiledConfiguration.instance.ChestClass && Chest != null) { return Instantiate(Chest, parent); }

            Debug.LogError($"Don't know what a '{classId}' is.");
            return null;
        }

        private GameObject InstantiateWithRotation(Transform parent, GameObject prefab, Direction direction)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, parent);
            go.transform.rotation = direction.AsQuaternion(Direction.Down, true);
            return go;
        }

        private GameObject InstantiateCubeSide(Transform parent, Direction direction)
        {
            if (direction.IsPlanarCardinal())
            {
                return InstantiateWithRotation(parent, Wall, direction);
            }

            if (direction == Direction.Up && Celing != null) { return Instantiate(Celing, parent); }
            if (direction == Direction.Down && Floor != null) { return Instantiate(Floor, parent); }
            
            return null;
        }


        public override GameObject Get(Transform parent, string classId, Direction direction, string variant = null)
        {
            if (classId == TiledConfiguration.instance.BaseTileClass) return InstantiateCubeSide(parent, direction);
            if (classId == TiledConfiguration.instance.LadderClass) return InstantiateWithRotation(parent, Ladder, direction);
            if (classId == TiledConfiguration.instance.WallButtonClass) return InstantiateWithRotation(parent, WallButton, direction);
            if (classId == TiledConfiguration.instance.AlcoveClass) return InstantiateWithRotation(parent, Alcove, direction);
            if (classId == TiledConfiguration.instance.WallSpikeTrapClass)
            {
                if (direction.IsPlanarCardinal()) return InstantiateWithRotation(parent, WallSpikes, direction);
                Debug.LogWarning($"Don't know how to instantiate {direction} spikes");
                return null;
            }
            if (classId == TiledConfiguration.instance.SpikeTrapClass) return InstantiateWithRotation(parent, Spikes, direction);
            if (classId == TiledConfiguration.instance.PillarClass) { return InstantiateWithRotation(parent, Pillar, direction); }
            if (classId == TiledConfiguration.instance.PedistalClass) { return InstantiateWithRotation(parent, Pedistal, direction); }
            if (classId == TiledConfiguration.instance.ChestClass) { return InstantiateWithRotation(parent, Chest, direction); }

            Debug.LogError($"Don't know what a '{classId}' is.");
            return null;
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
            if (classId == TiledConfiguration.instance.GrateClass) return InstantiateWithOrientation(parent, Grate, orientation);
            if (classId == TiledConfiguration.instance.ObstructionClass) return InstantiateWithOrientation(parent, Obstruction, orientation);
            if (classId == TiledConfiguration.instance.TrapDoorClass) return InstantiateWithOrientation(parent, TrapDoor, orientation);

            Debug.LogError($"Don't know what a '{classId}' is.");
            return null;
        }
        public override GameObject Get(Transform parent, string classId, TDEnumOrientation orientation, TDEnumInteraction interaction, string variant = null)
        {
            if (classId == TiledConfiguration.instance.DoorClass)
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

                Debug.LogError($"Don't know what a {interaction} '{classId}' is.");
                return null;
            }

            Debug.LogError($"Don't know what a '{classId}' is.");
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

            Debug.LogError($"Don't know what a '{elevation}' ramp is.");
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
            if (classId == TiledConfiguration.instance.RampClass) return InstantiateRamp(parent, elevation, direction);
           
            Debug.LogError($"Don't know what a '{classId}' is.");
            return null;
        }


        public override GameObject Get(Transform parent, string classId, TDEnumTransition transition, string variant = null)
        {
            if (classId == TiledConfiguration.instance.TeleporterClass)
            {
                if (transition == TDEnumTransition.Entry || transition == TDEnumTransition.EntryAndExit)
                {
                    if (Teleporter == null) { return null; }
                    return Instantiate(Teleporter, parent);
                }
                return null;
            }

            Debug.LogError($"Don't know what a '{classId}' is.");
            return null;
        }
    }
}