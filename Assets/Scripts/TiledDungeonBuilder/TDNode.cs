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

        [SerializeField, Tooltip("Name of custom properties class that has boolean fields for Down, Up, North, West, East, South")]
        string SidesClass = "Sides";

        [SerializeField]
        GameObject floor;

        [SerializeField]
        GameObject roof;

        [SerializeField]
        GameObject northWall;

        [SerializeField]
        GameObject southWall;

        [SerializeField]
        GameObject westWall;

        [SerializeField]
        GameObject eastWall;

        [SerializeField]
        TDDoor doorNS;

        [SerializeField]
        TDDoor doorWE;

        public bool Walkable => 
            !Obstructed 
            && tile.CustomProperties
            .Aspect(TiledConfiguration.instance.WalkabilityKey) == TDEnumAspect.Always;

        public bool Flyable => 
            !Obstructed 
            && tile.CustomProperties
            .Aspect(TiledConfiguration.instance.FlyabilityKey) == TDEnumAspect.Always;

        public bool HasFloor => floor != null && floor.activeSelf;
        public bool HasCeiling => roof != null && roof.activeSelf;

        public bool Obstructed => 
            modifications.Any(mod => 
            mod
            .Tile
            .CustomProperties
            .InteractionOrDefault(TiledConfiguration.instance.InteractionKey)
            .Obstructing()) ||
            (doorNS != null && doorNS.BlockingPassage) ||
            (doorWE != null && doorWE.BlockingPassage);

        void ConfigureOriented(
            TileModification[] modifications,
            GameObject vertical,
            GameObject horizontal,
            System.Func<TileModification, bool> modFilter
        )
        {
            var featureMods = modifications.Where(modFilter).ToList();

            vertical?.SetActive(false);
            horizontal?.SetActive(false);

            if (featureMods
                .Where(g => g.Tile.CustomProperties
                    .Orientation(TiledConfiguration.instance.OrientationKey) == TDEnumOrientation.Vertical
                ).Count() > 0) {
                if (vertical != null)
                {
                    vertical.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"Tile @ {Coordinates} doesn't support north<->south entity");
                }
            }

            if (featureMods
                .Where(g => g.Tile.CustomProperties
                .Orientation(TiledConfiguration.instance.OrientationKey) == TDEnumOrientation.Horizontal
                ).Count() > 0) {
                if (horizontal != null)
                {
                    horizontal.SetActive(true);
                } else
                {
                    Debug.LogWarning($"Tile @ {Coordinates} doesn't support west<->east entity");
                }
            }
        }

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

        void ConfigureDoors(TileModification[] modifications)
        {
            System.Func<TileModification, bool> filter =
                mod => mod.Tile.Type == TiledConfiguration.instance.DoorClass;

            ConfigureOriented(
                modifications,
                doorNS.gameObject,
                doorWE.gameObject,
                filter
            );

            if (!doorNS.gameObject.activeSelf)
            {
                DestroyImmediate(doorNS.gameObject);
                doorNS = null;
            }

            if (!doorWE.gameObject.activeSelf)
            {
                DestroyImmediate(doorWE.gameObject);
                doorWE = null;
            }

            foreach (TDDoor door in new[] { doorNS, doorWE })
            {
                if (door == null || !door.gameObject.activeSelf) continue;

                door.Configure(
                    Coordinates, 
                    modifications.Where(filter).ToArray(),
                    Points,
                    Rects
                );
            }
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


            var sides = tile.CustomProperties.Classes[SidesClass];
            if (sides == null)
            {
                Debug.LogError($"{tile} as {Coordinates} lacks a sides class, can't be used for layouting");
            } else
            {
                if (sides.Bool("Down"))
                {
                    floor.SetActive(true);
                } else
                {
                    DestroyImmediate(floor);
                    floor = null;
                }
                if (roofRule == TiledNodeRoofRule.CustomProps ? sides.Bool("Up") : roofRule == TiledNodeRoofRule.ForcedSet)
                {
                    roof.SetActive(true);
                } else
                {
                    DestroyImmediate(roof);
                    roof = null;
                }
                if (sides.Bool("West"))
                {
                    westWall.SetActive(true);
                } else
                {
                    DestroyImmediate(westWall);
                    westWall = null;
                }
                if (sides.Bool("South"))
                {
                    southWall.SetActive(true);
                } else
                {
                    DestroyImmediate(southWall);
                    southWall = null;
                }
                if (sides.Bool("North"))
                {
                    northWall.SetActive(true);
                } else
                {
                    DestroyImmediate(northWall);
                    northWall = null;
                }
                if (sides.Bool("East"))
                {
                    eastWall.SetActive(true);
                } else
                {
                    DestroyImmediate(eastWall);
                    eastWall = null;
                }
            }

            transform.localPosition = Coordinates.ToPosition(dungeon.Scale);
            name = $"TileNode Elevation {Coordinates.y} ({Coordinates.x}, {Coordinates.z})";

            ConfigureGrates();
            ConfigureObstructions();
            ConfigureDoors(modifications);
            ConfigureLadders();
            ConfigureTeleporter();
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
            switch (direction)
            {
                case Direction.North: return northWall != null && northWall.activeSelf;
                case Direction.South: return southWall != null && southWall.activeSelf;
                case Direction.West: return westWall != null && westWall.activeSelf;
                case Direction.East: return eastWall != null && eastWall.activeSelf;
                default: return false;
            }
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
            var axis = direction.AsAxis();

            if (doorNS?.gameObject.activeSelf ?? false)
            {
                if (axis != DirectionAxis.NorthSouth) return true;

                return doorNS.BlockingPassage;
            }

            if (doorWE?.gameObject.activeSelf ?? false)
            {
                if (axis != DirectionAxis.WestEast) return true;

                return doorWE.BlockingPassage;
            }

            return false;
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
