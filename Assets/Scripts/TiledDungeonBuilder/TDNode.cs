using LMCore.Crawler;
using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using TiledImporter;
using UnityEngine;

namespace TiledDungeon
{
    public enum TiledNodeRoofRule
    {
        CustomProps,
        ForcedSet,
        ForcedNotSet,
    }

    public class TDNode : MonoBehaviour, IDungeonNode
    {
        [SerializeField, HideInInspector]
        TiledTile tile;

        [SerializeField]
        TDSidesClass sides;

        [SerializeField, HideInInspector]
        TileModification[] modifications;

        [SerializeField, HideInInspector]
        TiledObjectLayer.Point[] Points;

        [SerializeField, HideInInspector]
        TiledObjectLayer.Rect[] Rects;

        TiledDungeon _dungeon;
        public TiledDungeon Dungeon
        {
            get
            {
                if (_dungeon == null)
                {
                    _dungeon = GetComponentInParent<TiledDungeon>();
                }
                return _dungeon;
            }

            private set { _dungeon = value; }
        }

        IOccupationRules OccupationRules => TDOccupancyRules.instance;

        [SerializeField, HideInInspector]
        private Vector3Int _coordinates;
        public Vector3Int Coordinates
        {
            get => _coordinates;
            set => _coordinates = value;
        }

        TDDoor _door;
        TDDoor door
        {
            get
            {
                if (_door == null)
                {
                    _door = GetComponentInChildren<TDDoor>();
                }
                return _door;
            }
        }

        public bool Walkable => 
            !Obstructed 
            && tile.CustomProperties
            .Aspect(TiledConfiguration.instance.WalkabilityKey) == TDEnumAspect.Always;

        public bool Flyable => 
            !Obstructed 
            && tile.CustomProperties
            .Aspect(TiledConfiguration.instance.FlyabilityKey) == TDEnumAspect.Always;

        public bool HasFloor => sides.Down;
        public bool HasCeiling => sides.Up;

        public bool Obstructed =>
            modifications.Any(mod =>
            mod
            .Tile
            .CustomProperties
            .InteractionOrDefault(TiledConfiguration.instance.InteractionKey)
            .Obstructing()) ||
            (door?.BlockingPassage == true);


        void ConfigureGrates()
        {
            var grate = modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.GrateClass);
            if (grate == null) return;

            Dungeon.Style.Get(
                transform, 
                TiledConfiguration.instance.GrateClass, 
                grate.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey)
            );
        }

        void ConfigureObstructions()
        {
            var obstruction = modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.ObstructionClass);
            if (obstruction == null) return;

            Dungeon.Style.Get(
                transform, 
                TiledConfiguration.instance.ObstructionClass, 
                obstruction.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey)
            );
        }

        void ConfigureDoors()
        {
            System.Func<TileModification, bool> filter = mod => mod.Tile.Type == TiledConfiguration.instance.DoorClass;

            var doorInfo = modifications.FirstOrDefault(filter);
            if (doorInfo == null) return;

            Dungeon.Style.Get(
                transform, 
                TiledConfiguration.instance.DoorClass, 
                doorInfo.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey),
                doorInfo.Tile.CustomProperties.InteractionOrDefault(TiledConfiguration.instance.InteractionKey, TDEnumInteraction.Closed)
            );

            door?.Configure(
                Coordinates, 
                modifications.Where(filter).ToArray(),
                Points,
                Rects
            );
        }

        void ConfigureLadders()
        {
            foreach (var tdDirection in TDEnumDirectionExtensions.PlanarDirections)
            {
                var direction = tdDirection.AsDirection();
                if (!HasLadder(direction)) continue;

                Dungeon.Style.Get(
                    transform,
                    TiledConfiguration.instance.LadderClass,
                    direction 
                );
            }
        }

        bool IsSpinner => 
            modifications.Any(m => m.Tile.Type == TiledConfiguration.instance.SpinnerClass);

        bool IsTrap => 
            modifications.Any(m => m.Tile.CustomProperties.Bool(TiledConfiguration.instance.TrapKey));

        bool IsTeleporter => 
            modifications.Any(m => m.Tile.Type == TiledConfiguration.instance.TeleporterClass);

        bool HasActiveTeleporter => modifications.Any(m => {
            if (m.Tile.Type != TiledConfiguration.instance.TeleporterClass) return false;
            return m.Tile.CustomProperties.Transition(TiledConfiguration.instance.TransitionKey).HasEntry();
        });

        int teleporterWormholdId => 
            FirstObjectValue(
                TiledConfiguration.instance.TeleporterClass, 
                (props) => props == null ? 0 : props.Int(TiledConfiguration.instance.TeleporterIdProperty)
            );

        void ConfigureTeleporter()
        {
            var teleporterMod = modifications.FirstOrDefault(m => m.Tile.Type == TiledConfiguration.instance.TeleporterClass);

            if (teleporterMod != null)
            {
                Dungeon.Style.Get(
                    transform,
                    TiledConfiguration.instance.TeleporterClass,
                    teleporterMod.Tile.CustomProperties.Transition(TiledConfiguration.instance.TransitionKey)
                    );
                
                Debug.Log($"{Coordinates} has teleporter Entry({HasActiveTeleporter}) Id({teleporterWormholdId})");
            }
        }

        void ConfigureCube()
        {
            if (sides == null)
            {
                Debug.LogError($"{tile} as {Coordinates} lacks a sides class, can't be used for layouting");
                return;
            }

            foreach (var direction in DirectionExtensions.AllDirections)
            {
                if (!sides.Has(direction)) continue;

                var go = Dungeon.Style.Get(transform, TiledConfiguration.instance.BaseTileClass, direction);
                go.name = direction.ToString();
            }
        }

        void ConfigureRamps()
        {
            var ramp = modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.RampClass);
            if (ramp == null) return;

            Dungeon.Style.Get(
                transform, 
                TiledConfiguration.instance.RampClass, 
                ramp.Tile.CustomProperties.Elevation(TiledConfiguration.instance.ElevationKey),
                ramp.Tile.CustomProperties.Direction(TiledConfiguration.instance.DownDirectionKey).AsDirection()
            );

        }

        public void Configure(
            TiledTile tile, 
            TiledNodeRoofRule roofRule,
            TiledDungeon dungeon,
            TileModification[] modifications,
            TiledObjectLayer.Point[] points,
            TiledObjectLayer.Rect[] rects
        )
        {
            this.tile = tile;
            this.modifications = modifications;
            Dungeon = dungeon;
            Points = points;
            Rects = rects;
            sides = TDSidesClass.From(
                tile.CustomProperties.Classes[TiledConfiguration.instance.SidesClassKey],
                roofRule
            );

            transform.localPosition = Coordinates.ToPosition(dungeon.Scale);
            name = $"TileNode Elevation {Coordinates.y} ({Coordinates.x}, {Coordinates.z})";

            ConfigureCube();
            ConfigureGrates();
            ConfigureObstructions();
            ConfigureDoors();
            ConfigureLadders();
            ConfigureTeleporter();
            ConfigureRamps();
        }

        private void OnDestroy()
        {
            Dungeon?.RemoveNode(this);
        }

        bool HasLadder(Direction direction)
        {
            var tdDirection = TDEnumDirectionExtensions.FromDirection(direction);

            return modifications.Any(mod => 
                    mod.Tile.Type == TiledConfiguration.instance.LadderClass 
                    && mod.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey) == tdDirection
                );
        }

        bool HasWall(Direction direction)
        {
            if (direction.IsPlanarCardinal()) return sides.Has(direction);
            return false;
        }

        private MovementOutcome ExitOrFallback(Direction direction, MovementOutcome fallback)
        {
            if (direction == Direction.Up)
            {
                return HasCeiling ? fallback : MovementOutcome.NodeExit;
            } else if (direction == Direction.Down)
            {
                return HasFloor ? fallback : MovementOutcome.NodeExit;
            } else
            {
                return HasWall(direction) || HasLadder(direction) ? fallback: MovementOutcome.NodeExit;
            }

        }

        MovementOutcome PlanarOutcome(Direction direction)
        {
            if (HasLadder(direction))
            {
                return MovementOutcome.NodeInternal;
            }

            if (HasWall(direction))
            {
                return MovementOutcome.Blocked;
            }

            return MovementOutcome.NodeExit;
        }

        public MovementOutcome AllowsMovement(GridEntity entity, Direction anchor, Direction direction)
        {
            if (entity.TransportationMode.HasFlag(TransportationMode.Flying))
            {
                return ExitOrFallback(direction, MovementOutcome.Blocked);
            }

            if (anchor == Direction.Down)
            {
                return PlanarOutcome(direction);
            } 

            if (anchor == Direction.Up)
            {
                return PlanarOutcome(direction.Inverse());
            }

            if (anchor.IsPlanarCardinal())
            {
                if (HasLadder(anchor))
                {
                    if (direction == Direction.Up)
                    {
                        if (HasCeiling)
                        {
                            return CanAnchorOn(entity, Direction.Up) ? MovementOutcome.NodeInternal : MovementOutcome.Blocked;
                        }

                        return MovementOutcome.NodeExit;
                    }

                    if (direction == Direction.Down)
                    {
                        if (HasFloor)
                        {
                            return CanAnchorOn(entity, Direction.Down) ? MovementOutcome.NodeInternal : MovementOutcome.Blocked;
                        }
                        return MovementOutcome.NodeExit;
                    }

                }

                // TODO: Other types of wall actions
                Debug.LogWarning($"{entity.name} is anchored on {anchor} wall but no implementation of movement {direction} at {Coordinates}");
                return MovementOutcome.Refused;
            }

            Debug.LogWarning($"{entity.name} is anchored {anchor} and {Coordinates} doesn't know how to handle that");
            return MovementOutcome.Refused;
        }

        public bool HasBlockingDoor(Direction direction)
        {
            var door = this.door;

            if (door == null) return false;

            if (door.BlockingPassage) return true;

            var doorAxis = door.Axis;

            if (doorAxis == DirectionAxis.None)
            {
                Debug.LogWarning($"Door @ {Coordinates} lacks an axis");
                return false;
            }
            return doorAxis != direction.AsAxis();
        }

        public bool AllowsEntryFrom(GridEntity entity, Direction direction)
        {
            if (HasWall(direction) || HasLadder(direction)) return false;

            if (HasBlockingDoor(direction)) return false;

            if (Obstructed) return false;

            if (_occupants.Count == 0) return true;

            return OccupationRules.MayCoexist(entity, _occupants);

        }

        HashSet<GridEntity> _occupants = new HashSet<GridEntity>();
        HashSet<GridEntity> _reservations = new HashSet<GridEntity> ();


        IDungeonNode HandleTeleporter(GridEntity entity)
        {
            if (entity.TransportationMode.HasFlag(TransportationMode.Teleporting))
            {
                Debug.Log($"Ignoring teleportation because {entity.name} was already teleporting here");
                entity.TransportationMode = entity.TransportationMode.RemoveFlag(TransportationMode.Teleporting);
                return null;
            }

            if (HasActiveTeleporter)
            {
                var outlet = Dungeon
                    .FindTeleportersById(teleporterWormholdId)
                    .FirstOrDefault(n => n.Coordinates != Coordinates);

                if (outlet == null)
                {
                    Debug.LogWarning($"{name} teleporter doesn't have a partner in their wormhole {teleporterWormholdId}; ignoring teleportation");
                    return null;
                }

                Debug.Log($"Teleporting {entity.name} to {outlet.Coordinates}");
                entity.Position = outlet.Coordinates;
                entity.Anchor = Direction.Down;
                entity.TransportationMode = entity.TransportationMode.RemoveFlag(TransportationMode.Climbing).AddFlag(TransportationMode.Teleporting);
                entity.Sync();
                return outlet;
            }

            return this;
        }

        void HandleSpinner(GridEntity entity)
        {

            var spinMod = modifications.FirstOrDefault(m => m.Tile.Type == TiledConfiguration.instance.SpinnerClass);

            if (spinMod == null) { return; }

            var movement = spinMod.Tile.CustomProperties.Rotation().AsMovement();
            if (movement != LMCore.IO.Movement.None)
            {
                Debug.Log($"Spinning {entity.name} {movement}");
                entity.Input.InjectMovement(movement);
            }
        }

        void HandleTraps(GridEntity entity)
        {
            IDungeonNode target = null;

            if (IsTeleporter)
            {
                target = HandleTeleporter(entity);
            }

            if (entity.Anchor == Direction.Down && IsSpinner)
            {
                HandleSpinner(entity);
            }

            if (target == null || target == (IDungeonNode)this)
            {
                _occupants.Add(entity);
            } else
            {
                target.AddOccupant(entity);
            }
        }

        public void AddOccupant(GridEntity entity)
        {
            Debug.Log($"Handle {Coordinates} occupancy of {entity.name}");

            OccupationRules.HandleMeeting(entity, _occupants);
            _reservations.Remove(entity);

            if (IsTrap) {
                HandleTraps(entity);
            } else {
                _occupants.Add(entity);
            }
        }

        public void Reserve(GridEntity entity)
        {
            _reservations.Add(entity);
        }

        public void RemoveOccupant(GridEntity entity)
        {
            _occupants.Remove(entity);
            OccupationRules.HandleDeparture(entity, _occupants);
        }

        public bool AllowsRotating(GridEntity entity)
        {
            if (entity.Anchor.IsPlanarCardinal())
            {
                if (HasLadder(entity.Anchor)) return false;

                Debug.LogWarning($"Unhandled wall situation for {entity.name} as {Coordinates}");
            }
            return true;
        }

        public bool CanAnchorOn(GridEntity entity, Direction anchor)
        {
            if (Obstructed) return false;

            if (anchor == Direction.Down) return HasFloor || entity.TransportationMode.HasFlag(TransportationMode.Flying);

            // Can't fly on a walls
            if (entity.TransportationMode.HasFlag(TransportationMode.Flying)) return false;

            if (anchor.IsPlanarCardinal())
            {
                return HasLadder(anchor);
            }

            Debug.LogWarning(
                $"Can't allow {entity.name} to anchor on {anchor} @ {Coordinates} because no ladder"
            );

            return false;
        }

        public T FirstObjectPointValue<T>(string name, System.Func<TiledCustomProperties, T> predicate) =>
            predicate(Points.FirstOrDefault(pt => pt.Name == name).CustomProperties);

        public T FirstObjectRectValue<T>(string name, System.Func<TiledCustomProperties, T> predicate) =>
            predicate(Rects.FirstOrDefault(pt => pt.Name == name).CustomProperties);

        public T FirstObjectValue<T>(string name, System.Func<TiledCustomProperties, T> predicate)
        {
            return predicate(
                Points.FirstOrDefault(pt => pt.Name == name)?.CustomProperties ??
                    Rects.FirstOrDefault(pt => pt.Name == name)?.CustomProperties
            );
        }

        public bool HasObjectPoint(string name, System.Func<TiledCustomProperties, bool> predicate) =>
            Points.Any(pt => pt.Name == name && predicate(pt.CustomProperties));

        public bool HasObjectRect(string name, System.Func<TiledCustomProperties, bool> predicate) =>
            Rects.Any(pt => pt.Name == name && predicate(pt.CustomProperties));

        public bool HasObject(string name, System.Func<TiledCustomProperties, bool> predicate) =>
            HasObjectPoint(name, predicate) || HasObjectRect(name, predicate);
    }   
}
