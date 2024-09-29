using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledImporter;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.DungeonFeatures;

namespace LMCore.TiledDungeon
{
    public delegate void NewOccupantEvent(TDNode node, GridEntity entity);

    public class TDNode : MonoBehaviour, IDungeonNode
    {
        public static event NewOccupantEvent OnNewOccupant;

        public static System.Func<TileModification, bool> IllusoryFilter = mod => mod.Tile.Type == TiledConfiguration.InstanceOrCreate().IllusoryTileClass;
        public static System.Func<TileModification, bool> DoorFilter = mod => mod.Tile.Type == TiledConfiguration.instance.DoorClass;
        public static System.Func<TileModification, bool> TrapDoorFilter = mod => mod.Tile.Type == TiledConfiguration.instance.TrapDoorClass;

        [HideInInspector]
        public TiledTile tile;

        public TDSidesClass sides;
        
        public void UpdateSide(Direction direction, bool value) 
            => sides.Set(direction, value);

        [HideInInspector]
        public TileModification[] modifications;

        [HideInInspector]
        public TiledObjectLayer.Point[] Points;

        [HideInInspector]
        public TiledObjectLayer.Rect[] Rects;

        TDNodeConfig _config;
        public TDNodeConfig Config { 
            get { 
                if (_config == null)
                {
                    _config = new TDNodeConfig(modifications, Points, Rects);
                }

                return _config; 
            } 
        }

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

            set { _dungeon = value; }
        }

        IOccupationRules OccupationRules => TDOccupancyRules.instance;

        [SerializeField, HideInInspector]
        private Vector3Int _coordinates;
        public Vector3Int Coordinates
        {
            get => _coordinates;
            set => _coordinates = value;
        }
        public override string ToString() =>
                $"TDNode {name} @ {Coordinates}";

        public Vector3 CenterPosition => Coordinates.ToPosition(Dungeon.GridSize) + Vector3.up * Dungeon.GridSize * 0.5f;

        TDContainer _container;
        TDContainer Container 
        {
            get
            {
                if (_container == null)
                {
                    _container = GetComponentInChildren<TDContainer>();
                }
                return _container;
            }
        }

        TDDoor _door;
        public TDDoor Door
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

        TDSpikeTrap _spikes;
        TDSpikeTrap Spikes
        {
            get
            {
                if (_spikes == null)
                {
                    _spikes = GetComponentInChildren<TDSpikeTrap>();
                }
                return _spikes;
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

        public bool HasFloor => 
            sides.Down && (HasTrapDoor == false || Door?.FullyClosed == true) && !HasIllusion(Direction.Down);

        public bool HasIllusorySurface(Direction direction)
        {
            return GetComponentsInChildren<TDIllusoryCubeSide>()
                .Any(s => s.CubeFace == direction);
        }

        public bool HasCeiling => sides.Up;

        // Can't check doors this simply because they can be trapdoors
        public bool Obstructed =>
            (Spikes == null ? false : Spikes.BlockingEntry) ||
            (Container == null ? false : Container.BlockingPassage);


        public string NodeStyle => 
            Points.Select(pt => pt.CustomProperties.String(TiledConfiguration.instance.ObjVariantStyleKey)).FirstOrDefault(v => v != null) ??
            Rects.Select(r => r.CustomProperties.String(TiledConfiguration.instance.ObjVariantStyleKey)).FirstOrDefault(v => v != null);

        protected string PrefixLogMessage(string message) => $"Node @ {Coordinates}: {message}";
        public void Log(string message, System.Action<string> logFn) => logFn(PrefixLogMessage(message));


        public bool HasIllusion(Direction direction) =>
            modifications
                .Any(mod => IllusoryFilter(mod)
                    && mod.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().DirectionKey, TDEnumDirection.None).AsDirection() == direction);

        public bool IsSpinner => 
            modifications.Any(m => m.Tile.Type == TiledConfiguration.instance.SpinnerClass);

        public bool IsTrap => 
            modifications.Any(m => m.Tile.CustomProperties.Bool(TiledConfiguration.instance.TrapKey));

        public bool IsTeleporter => 
            modifications.Any(m => m.Tile.Type == TiledConfiguration.instance.TeleporterClass);

        public bool HasActiveTeleporter => modifications.Any(m => {
            if (m.Tile.Type != TiledConfiguration.instance.TeleporterClass) return false;
            return m.Tile.CustomProperties.Transition(TiledConfiguration.instance.TransitionKey).HasEntry();
        });

        public int TeleporterWormholdId => 
            Config
            .FirstObjectValue(
                TiledConfiguration.instance.TeleporterClass, 
                (props) => props == null ? 0 : props.Int(TiledConfiguration.instance.TeleporterIdProperty)
            );

        public bool HasSpikes(Direction direction) =>
            modifications.Any(m => 
                m.Tile.Type == TiledConfiguration.instance.SpikeTrapClass && 
                m.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() == direction);


        public TileModification RampModification => modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.RampClass);

        public TileModification TrapdoorModification =>
            modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.TrapDoorClass);

        public bool HasTrapDoor => TrapdoorModification != null;

        public TileModification StairsModification =>
            modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.InstanceOrCreate().StairsClass);

        public bool HasStairs => StairsModification != null;

        TileModification PressurePlateModification =>
            modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.PressurePlateClass);

        public bool HasPressurePlate => PressurePlateModification != null;


        private void OnDestroy()
        {
            Dungeon?.RemoveNode(this);
        }

        public bool HasLadder(Direction direction)
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
            if (HasBlockingDoor(direction)) return MovementOutcome.Blocked;

            if (entity == null && anchor == Direction.None)
            {
                return sides.Has(direction) ? MovementOutcome.Refused : MovementOutcome.NodeExit;
            }

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
                Debug.LogWarning(PrefixLogMessage($"{entity.name} is anchored on {anchor} wall but no implementation of movement {direction}"));
                return MovementOutcome.Refused;
            }

            return ExitOrFallback(direction, MovementOutcome.Blocked);
        }

        public bool HasBlockingDoor(Direction direction)
        {
            var door = Door;

            if (door == null) return false;
            Debug.Log(PrefixLogMessage($"{door} Trapdoor({HasTrapDoor})"));

            if (HasTrapDoor)
            {
                return direction == Direction.Down && door.BlockingPassage;
            }

            if (door.BlockingPassage) return true;

            var doorAxis = door.TraversalAxis;

            if (doorAxis == DirectionAxis.None)
            {
                Debug.LogWarning(PrefixLogMessage($"Door lacks axis"));
                return false;
            }

            if (doorAxis != DirectionAxis.None && doorAxis != direction.AsAxis())
            {
                Debug.LogWarning(PrefixLogMessage($"Trying to enter door by the wrong axis (door {doorAxis} trying to enter {direction.AsAxis()})"));
                return true;
            }

            return door.BlockingPassage;
        }

        bool BlockEdgeTraversal(GridEntity entity, Direction direction) =>
            modifications.Any(mod => {
                var modDirection = mod.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().DirectionKey, TDEnumDirection.None).AsDirection();
                if (direction != modDirection && modDirection != Direction.None) return false;
                if (entity.TransportationMode.HasFlag(TransportationMode.Flying))
                {
                    var flyability = mod.Tile.CustomProperties.Aspect(TiledConfiguration.InstanceOrCreate().FlyabilityKey, TDEnumAspect.Always);
                    return flyability == TDEnumAspect.Never;
                }
                var walkability = mod.Tile.CustomProperties.Aspect(TiledConfiguration.InstanceOrCreate().WalkabilityKey, TDEnumAspect.Always);
                return walkability == TDEnumAspect.Never;
            });

        public bool AllowExit(GridEntity entity, Direction direction) => !sides.Has(direction) &&
            !BlockEdgeTraversal(entity, direction);

        public bool AllowsEntryFrom(GridEntity entity, Direction direction)
        {
            if (HasWall(direction) || HasLadder(direction)) return false;

            if (HasBlockingDoor(direction)) return false;

            if (Obstructed) return false;

            var ramp = RampModification;
            if (ramp != null && direction.IsPlanarCardinal())
            {
                var axis = direction.AsAxis();
                if (ramp.Tile.CustomProperties.Elevation(TiledConfiguration.instance.ElevationKey) != TDEnumElevation.Low &&
                    ramp.Tile.CustomProperties.Direction(TiledConfiguration.instance.DownDirectionKey).AsDirection().AsAxis() != axis)
                {
                    return false;
                }
            }

            if (BlockEdgeTraversal(entity, direction)) return false;

            var platform = GetComponentInChildren<TDMovingPlatform>();
            if (platform != null)
            {
                Debug.Log(PrefixLogMessage("We have a platform"));
                return platform.MayEnter(entity);
            }

            return true;
        }

        IDungeonNode HandleTeleporter(GridEntity entity)
        {
            if (entity.TransportationMode.HasFlag(TransportationMode.Teleporting))
            {
                Debug.Log(PrefixLogMessage($"Ignoring teleportation because {entity.name} was already teleporting here"));
                entity.TransportationMode = entity.TransportationMode.RemoveFlag(TransportationMode.Teleporting);
                return null;
            }

            if (HasActiveTeleporter)
            {
                var outlet = Dungeon
                    .FindTeleportersById(TeleporterWormholdId)
                    .FirstOrDefault(n => n.Coordinates != Coordinates);

                if (outlet == null)
                {
                    Debug.LogWarning(PrefixLogMessage($"teleporter doesn't have a partner in their wormhole {TeleporterWormholdId}; ignoring teleportation"));
                    return null;
                }

                Debug.Log(PrefixLogMessage($"Teleporting {entity.name} to {outlet.Coordinates}"));
                entity.Coordinates = outlet.Coordinates;
                entity.AnchorDirection = Direction.Down;
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
            if (movement != IO.Movement.None)
            {
                Debug.Log(PrefixLogMessage($"Spinning {entity.name} {movement}"));
                entity.Input.InjectMovement(movement);
            }
        }

        void AddNewOccupant(GridEntity entity)
        {
            _occupants.Add(entity);
            
            if (entity.NodeAnchor != null)
            {
                var platform = entity.NodeAnchor.GetComponentInChildren<TDMovingPlatform>();
                if (platform != null)
                {
                    platform.ConstrainEntity(entity);
                }
            }
        }

        void HandleTraps(GridEntity entity, bool newOccupation)
        {
            IDungeonNode target = null;

            if (IsTeleporter && newOccupation)
            {
                target = HandleTeleporter(entity);
            }

            if (entity.AnchorDirection == Direction.Down)
            {
                if (IsSpinner)
                {
                    if (_occupants.Contains(entity))
                    {
                        Debug.Log(PrefixLogMessage($"Skip spinning '{entity.name}' because don't know if already spun"));
                    }
                    else
                    {
                        HandleSpinner(entity);
                    }
                }
            }

            if (target == null || target == (IDungeonNode)this)
            {
                AddNewOccupant(entity);
            } else
            {
                target.AddOccupant(entity);
            }
        }

        #region EntityOccupation
        HashSet<GridEntity> _occupants = new HashSet<GridEntity>();
        HashSet<GridEntity> _reservations = new HashSet<GridEntity> ();

        public void AddOccupant(GridEntity entity)
        {
            Debug.Log(PrefixLogMessage($"Handling occupancy of {entity.name}"));

            bool newOccupation = !_occupants.Contains(entity);

            if (newOccupation)
            {
                OccupationRules.HandleMeeting(entity, _occupants);
            }
            _reservations.Remove(entity);

            if (newOccupation) OnNewOccupant?.Invoke(this, entity);

            if (IsTrap) {
                HandleTraps(entity, newOccupation);
            } else {
                AddNewOccupant(entity);
            }

            if (newOccupation) entity.transform.SetParent(transform);
        }

        public IEnumerable<GridEntity> Occupants => _occupants;

        public void Reserve(GridEntity entity)
        {
            if (_occupants.Contains(entity)) return;

            Debug.Log($"Reserving {entity.name} occupancy for {Coordinates}");
            _reservations.Add(entity);
        }

        public void RemoveOccupant(GridEntity entity)
        {
            Debug.Log($"Removing {entity.name} occupancy from {Coordinates}");
            _occupants.Remove(entity);
            OccupationRules.HandleDeparture(entity, _occupants);
            if (entity.transform.parent == transform)
            {
                entity.transform.SetParent(Dungeon.transform);
            }
        }
        #endregion

        public bool AllowsRotating(GridEntity entity)
        {
            if (entity.AnchorDirection.IsPlanarCardinal())
            {
                if (HasLadder(entity.AnchorDirection)) return false;

                Debug.LogWarning(PrefixLogMessage($"Unhandled wall situation for {entity.name}"));
            }
            return true;
        }

        public bool CanAnchorOn(GridEntity entity, Direction anchor)
        {
            if (Obstructed) return false;

            if (anchor == Direction.None) return entity.TransportationMode.HasFlag(TransportationMode.Flying);

            if (anchor == Direction.Down) return HasFloor || entity.TransportationMode.HasFlag(TransportationMode.Flying);

            // Can't fly on a walls
            if (entity.TransportationMode.HasFlag(TransportationMode.Flying)) return false;

            if (anchor.IsPlanarCardinal())
            {
                return HasLadder(anchor);
            }

            Debug.LogWarning(PrefixLogMessage(
                $"Can't allow {entity.name} to anchor on {anchor} because no ladder"
            ));

            return false;
        }

        public Vector3Int Neighbour(Direction direction)
        {
            return direction.Translate(Coordinates);
        }

        public Anchor GetAnchor(Direction direction) => 
            GetComponentsInChildren<Anchor>()
                .FirstOrDefault(a => a.CubeFace == direction);

        public Vector3 GetEdge(Direction anchor)
        {
            var a = GetAnchor(anchor);
            if (a != null) return a.CenterPosition;

            return CenterPosition + anchor.AsLookVector3D().ToDirection(Dungeon.GridSize * 0.5f);
        }

        public Vector3 GetEdge(Direction anchor, Direction edge)
        {
            var a = GetAnchor(anchor);
            if (a != null) return a.GetEdgePosition(edge);

            return CenterPosition 
                + anchor.AsLookVector3D().ToDirection(Dungeon.GridSize * 0.5f)
                + edge.AsLookVector3D().ToDirection(Dungeon.GridSize * 0.5f);
        }
    }   
}
