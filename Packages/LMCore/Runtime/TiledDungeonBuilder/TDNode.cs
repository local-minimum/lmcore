using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.TiledImporter;
using LMCore.TiledDungeon.Integration;

namespace LMCore.TiledDungeon
{

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

        TDContainer _container;
        TDContainer container 
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

        TDSpikeTrap _spikes;
        TDSpikeTrap spikes
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

        public bool HasFloor => sides.Down && (HasTrapDoor == false || door?.BlockingPassage == true);
        public bool HasCeiling => sides.Up;

        public bool Obstructed =>
            modifications.Any(mod =>
            mod
            .Tile
            .CustomProperties
            .InteractionOrDefault(TiledConfiguration.instance.InteractionKey)
            .Obstructing()) && 
            (door == null ? true : door.BlockingPassage) || 
            (spikes == null ? false : spikes.BlockingEntry) ||
            (container == null ? false : container.BlockingPassage);


        string NodeStyle => 
            Points.Select(pt => pt.CustomProperties.String(TiledConfiguration.instance.ObjVariantStyleKey)).FirstOrDefault(v => v != null) ??
            Rects.Select(r => r.CustomProperties.String(TiledConfiguration.instance.ObjVariantStyleKey)).FirstOrDefault(v => v != null);

        void ConfigureGrates()
        {
            var grate = modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.GrateClass);
            if (grate == null) return;

            Dungeon.Style.Get(
                transform, 
                TiledConfiguration.instance.GrateClass, 
                grate.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey),
                NodeStyle
            );
        }

        void ConfigureObstructions()
        {
            var obstruction = modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.ObstructionClass);
            if (obstruction == null) return;

            Dungeon.Style.Get(
                transform, 
                TiledConfiguration.instance.ObstructionClass, 
                obstruction.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey),
                NodeStyle
            );
        }

        static System.Func<TileModification, bool> doorFilter = mod => mod.Tile.Type == TiledConfiguration.instance.DoorClass;

        void ConfigureDoors()
        {

            var doorInfo = modifications.FirstOrDefault(doorFilter);
            if (doorInfo == null) return;

            Dungeon.Style.Get(
                transform, 
                TiledConfiguration.instance.DoorClass, 
                doorInfo.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey),
                doorInfo.Tile.CustomProperties.InteractionOrDefault(TiledConfiguration.instance.InteractionKey, TDEnumInteraction.Closed),
                NodeStyle
            );

            door?.Configure(
                this,
                Coordinates, 
                modifications.Where(doorFilter).ToArray()
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
                    direction,
                    NodeStyle
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
            Config
            .FirstObjectValue(
                TiledConfiguration.instance.TeleporterClass, 
                (props) => props == null ? 0 : props.Int(TiledConfiguration.instance.TeleporterIdProperty)
            );

        bool HasSpikes(Direction direction) =>
            modifications.Any(m => 
                m.Tile.Type == TiledConfiguration.instance.SpikeTrapClass && 
                m.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() == direction);

        void ConfigureTeleporter()
        {
            var teleporterMod = modifications.FirstOrDefault(m => m.Tile.Type == TiledConfiguration.instance.TeleporterClass);

            if (teleporterMod != null)
            {
                Dungeon.Style.Get(
                    transform,
                    TiledConfiguration.instance.TeleporterClass,
                    teleporterMod.Tile.CustomProperties.Transition(TiledConfiguration.instance.TransitionKey),
                    NodeStyle
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

            var hasTrapDoor = HasTrapDoor;
            var aboveNode = Coordinates + Vector3Int.up;
            foreach (var direction in DirectionExtensions.AllDirections)
            {
                if (direction == Direction.Down)
                {
                    if (hasTrapDoor)
                    {
                        var trapdoor = Dungeon.Style.Get(
                            transform,
                            TiledConfiguration.instance.TrapDoorClass,
                            TrapdoorModification.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey),
                            NodeStyle
                        );
                        if (trapdoor != null)
                        {
                            trapdoor.name = $"TrapDoor ({direction})";

                            var door = trapdoor.GetComponent<TDDoor>();

                            door?.Configure(
                                this,
                                Coordinates,
                                modifications.Where(doorFilter).ToArray()
                            );
                            continue;
                        }
                    }
                    else if (hasPressurePlate) {
                        var plate = Dungeon.Style.Get(
                            transform,
                            TiledConfiguration.instance.PressurePlateClass,
                            NodeStyle
                        );

                        if (plate != null)
                        {
                            plate.name = $"Pressure Plate ({direction})";

                            var pressurePlate = plate.GetComponent<TDActuator>();

                            pressurePlate?.Configure(this);
                            continue;
                        }
                    }
                }


                if (!sides.Has(direction)) continue;

                if (HasSpikes(direction))
                {
                    var spikes = Dungeon.Style.Get(
                        transform,
                        TiledConfiguration.instance.SpikeTrapClass,
                        direction,
                        NodeStyle
                    );

                    if (spikes != null)
                    {
                        spikes.name = $"Spikes ({direction})";

                        spikes.GetComponent<TDSpikeTrap>()?.Configure(this, Coordinates, modifications);
                        continue;
                    }
                }

                if (direction == Direction.Up && Dungeon.HasNodeAt(aboveNode) && Dungeon[aboveNode].HasTrapDoor) continue;

                if (direction.IsPlanarCardinal())
                {
                    var neighbour = direction.Translate(Coordinates);
                    var neighbourConfig = Dungeon.GetNodeConfig(neighbour);
                    var hasAlcove = neighbourConfig 
                        ?.Modifications
                        .Any(nMod => 
                            nMod.Tile.Type == TiledConfiguration.instance.AlcoveClass
                            && nMod.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() == direction.Inverse()
                        ) ?? false;

                    if (hasAlcove)
                    {
                        // TODO: Possibly it should get its styling from the neighbour tile rather than this
                        var alcove = Dungeon.Style.Get(transform, TiledConfiguration.instance.AlcoveClass, direction, NodeStyle);
                        alcove.name = direction.ToString();

                        ConfigureContainer(alcove, direction, direction.Inverse(), TiledConfiguration.instance.AlcoveClass, neighbourConfig, false);

                        continue;
                    } else if (ConfigureWallSpike(direction))
                    {
                        continue;
                    }
                }
                var go = Dungeon.Style.Get(transform, TiledConfiguration.instance.BaseTileClass, direction, NodeStyle);
                go.name = direction.ToString();
            }
        }

        TileModification RampModification => modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.RampClass);
        public bool IsHighRamp
        {
            get
            {
                var ramp = RampModification;
                if (ramp == null) return false;

                return ramp.Tile.CustomProperties.Elevation(TiledConfiguration.instance.ElevationKey) == TDEnumElevation.High;
            }
        }

        public bool IsRamp => RampModification != null;

        void ConfigureRamps()
        {
            var ramp = RampModification;
            if (ramp == null) return;

            Dungeon.Style.Get(
                transform, 
                TiledConfiguration.instance.RampClass, 
                ramp.Tile.CustomProperties.Elevation(TiledConfiguration.instance.ElevationKey),
                ramp.Tile.CustomProperties.Direction(TiledConfiguration.instance.DownDirectionKey).AsDirection(),
                NodeStyle
            );

        }

        void ConfigureWallButtons()
        {
            var button = modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.WallButtonClass);

            if (button == null) return;

            var go = Dungeon.Style.Get(
                transform,
                TiledConfiguration.instance.WallButtonClass,
                button.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection(),
                NodeStyle
            );

            if (go == null) return;

            go.GetComponent<TDActuator>()?.Configure(this);
        }

        bool ConfigureWallSpike(Direction directection)
        {
            var spikes = modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.WallSpikeTrapClass);

            if (spikes == null || spikes.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() != directection) return false; 

            var go = Dungeon.Style.Get(
                transform,
                TiledConfiguration.instance.WallSpikeTrapClass,
                directection,
                NodeStyle
            );

            if (go == null) return false;

            go.GetComponent<TDSpikeTrap>()?.Configure(this, Coordinates, modifications);

            return true;
        }


        GameObject ConfigurePotentiallyRotated(string className, string rotationClassName, TDNodeConfig config, out Direction direction)
        {
            if (config.Modifications.Any(mod => mod.Tile.Type == className))
            {
                direction = config.FirstObjectValue(
                    rotationClassName, 
                    props => props == null ? Direction.None : props.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection()
                );
                if (direction == Direction.None)
                {
                    direction = config.FirstObjectValue(
                        className, 
                        props => props == null ? Direction.None : props.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection()
                    );
                }

                if (direction == Direction.None)
                {
                    Debug.Log($"Node @ {Coordinates}: Getting Un-Rotated version of {className}");
                    return Dungeon.Style.Get(
                        transform,
                        className,
                        NodeStyle);
                } else
                {
                    Debug.Log($"Node @ {Coordinates}: Getting Rotated {direction} version of {className}");
                    return Dungeon.Style.Get(
                        transform,
                        className,
                        direction,
                        NodeStyle);

                }
            }

            direction = Direction.None;
            return null;
        }

        void ConfigurePillar(TDNodeConfig config) => ConfigurePotentiallyRotated(
            TiledConfiguration.instance.PillarClass, 
            TiledConfiguration.instance.PillarClass, 
            config, 
            out Direction _);

        void ConfigurePedistal(TDNodeConfig nodeConfig)
        {
            var pedistal = ConfigurePotentiallyRotated(
                TiledConfiguration.instance.PedistalClass, 
                TiledConfiguration.instance.ObjContainerClass,
                nodeConfig, 
                out Direction direction
            );
            ConfigureContainer(pedistal, Direction.Down, direction, TiledConfiguration.instance.PedistalClass, nodeConfig, true);
        }

        void ConfigureChest(TDNodeConfig nodeConfig)
        {
            var chest = ConfigurePotentiallyRotated(
                TiledConfiguration.instance.ChestClass,
                TiledConfiguration.instance.ObjContainerClass,
                nodeConfig,
                out Direction direction);

            ConfigureContainer(chest, Direction.Down, direction, TiledConfiguration.instance.ChestClass, nodeConfig, true);
        }

        void ConfigureContainer(GameObject tile, Direction anchor, Direction facingDirection, string containerClass, TDNodeConfig nodeConfig, bool blockingPassage)
        {
            if (tile == null) return;

            var container = tile.GetComponent<TDContainer>();

            if (container == null) {
                // Only error if it seems Tiled assumes there should be a chest
                if (facingDirection != Direction.None) Debug.LogError($"Chest @ {Coordinates}: Lacks script to configure");
                return;
            };

            container.Configure(
                nodeConfig, 
                Coordinates, 
                anchor,
                facingDirection, 
                containerClass, 
                modifications, 
                blockingPassage);

        }

        TileModification TrapdoorModification =>
            modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.TrapDoorClass);

        public bool HasTrapDoor => TrapdoorModification != null;

        TileModification PressurePlateModification =>
            modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.PressurePlateClass);

        public bool hasPressurePlate => PressurePlateModification != null;

        public void Configure(
            TiledTile tile, 
            TDNodeConfig config,
            TiledDungeon dungeon
        )
        {
            this.tile = tile;
            modifications = config.Modifications;
            Points = config.Points;
            Rects = config.Rects;

            Dungeon = dungeon;
            sides = TDSidesClass.From(
                tile.CustomProperties.Classes[TiledConfiguration.instance.SidesClassKey],
                config.RoofRule 
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
            ConfigureWallButtons();
            ConfigurePillar(config);
            ConfigurePedistal(config);
            ConfigureChest(config);

            Debug.Log($"Node @ {Coordinates}: Generated");
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

        bool RampOutcome(Direction direction, out MovementOutcome outcome)
        {
            if (!IsHighRamp) {
                outcome = MovementOutcome.Refused;
                return false;
            }

            var ramp = RampModification;
            if (direction != ramp.Tile.CustomProperties.Direction(TiledConfiguration.instance.DownDirectionKey).AsDirection())
            {
                outcome = MovementOutcome.NodeExit;
                return true;
            }

            outcome = MovementOutcome.Refused;
            return false;
        }


        public MovementOutcome AllowsMovement(GridEntity entity, Direction anchor, Direction direction)
        {
            if (entity.TransportationMode.HasFlag(TransportationMode.Flying))
            {
                if (RampOutcome(direction, out MovementOutcome outcome))
                {
                    return outcome;
                }

                return ExitOrFallback(direction, MovementOutcome.Blocked);
            }

            if (anchor == Direction.Down)
            {
                if (RampOutcome(direction, out MovementOutcome outcome))
                {
                    return outcome;
                }
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

            if (HasTrapDoor)
            {
                return direction == Direction.Down && door.BlockingPassage;
            }

            if (door.BlockingPassage) return true;

            var doorAxis = door.TraversalAxis;

            if (doorAxis == DirectionAxis.None)
            {
                Debug.LogWarning($"Door @ {Coordinates} lacks an axis");
                return false;
            }

            if (doorAxis != DirectionAxis.None && doorAxis != direction.AsAxis())
            {
                Debug.LogWarning($"Trying to enter door @ {Coordinates} by the wrong axis (door {doorAxis} trying to enter {direction.AsAxis()})");
                return true;
            }

            return false;
        }

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
            if (movement != IO.Movement.None)
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

        public IEnumerable<GridEntity> Occupants => _occupants;

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

        public static Vector3 DefaultAnchorOffset(Direction anchor, bool rotationRespectsAnchorDirection, float gridSize)
        {
            // TODO: Place magic number to be somewhat below ceiling somewhere 
            if (anchor == Direction.Up) return rotationRespectsAnchorDirection ?
                    Vector3.up * gridSize : Vector3.up * gridSize * 0.9f;


            if (rotationRespectsAnchorDirection)
            {
                return Vector3.up * gridSize * 0.5f + anchor.AsLookVector3D().ToDirection() * gridSize * 0.5f;
            }

            // TODO: Place magic number to not get too close to wall somewhere
            return Vector3.up * gridSize * 0.45f 
                + anchor.AsLookVector3D().ToDirection() * gridSize * 0.45f;

        }

        public Vector3 AnchorOffset(Direction anchor, bool rotationRespectsAnchorDirection)
        {
            if (anchor == Direction.Down)
            {
                var ramp = RampModification;
                if (ramp == null) return Vector3.zero;

                switch (ramp.Tile.CustomProperties.Elevation(TiledConfiguration.instance.ElevationKey))
                {
                    case TDEnumElevation.Low:
                        return Vector3.up * Dungeon.GridSize / 6f;
                    case TDEnumElevation.Middle:
                        return Vector3.up * Dungeon.GridSize * 0.5f;
                    case TDEnumElevation.High:
                        return Vector3.up * Dungeon.GridSize * 5f / 6f;
                    default:
                        return Vector3.zero;
                }
            }

            return DefaultAnchorOffset(anchor, rotationRespectsAnchorDirection, Dungeon.GridSize);
        }

        public Vector3Int Neighbour(Direction direction)
        {
            if (direction.IsPlanarCardinal() && IsHighRamp)
            {
                var downDirection = RampModification.Tile.CustomProperties
                    .Direction(TiledConfiguration.instance.DownDirectionKey)
                    .AsDirection();

                if (downDirection != direction)
                {
                    return direction.Translate(Coordinates) + Vector3Int.up;
                }
            }

            return direction.Translate(Coordinates);
        }
    }   
}
