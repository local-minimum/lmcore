using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledImporter;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class SideOverride
    {
        private Dictionary<Direction, HashSet<MonoBehaviour>> sideOverrides = new Dictionary<Direction, HashSet<MonoBehaviour>>();

        public void AddOverride(Direction direction, MonoBehaviour behaviour)
        {
            if (sideOverrides.ContainsKey(direction))
            {
                sideOverrides[direction].Add(behaviour);
            }
            else
            {
                sideOverrides[direction] = new HashSet<MonoBehaviour>() { behaviour };
            }
        }

        public void RemoveOverride(Direction direction, MonoBehaviour behaviour)
        {
            if (sideOverrides.ContainsKey(direction))
            {
                sideOverrides[direction].Remove(behaviour);
            }
        }

        public bool Overrides(Direction direction) =>
            sideOverrides.ContainsKey(direction) && sideOverrides[direction].Count > 0;

    }
    public delegate void NewOccupantEvent(TDNode node, GridEntity entity);

    public class TDNode : MonoBehaviour, IDungeonNode
    {
        public static event NewOccupantEvent OnNewOccupant;

        public static System.Func<TileModification, bool> IllusoryFilter = mod => mod.Tile.Type == TiledConfiguration.InstanceOrCreate().IllusoryTileClass;
        public static System.Func<TileModification, bool> DoorFilter = mod => mod.Tile.Type == TiledConfiguration.instance.DoorClass;
        public static System.Func<TileModification, bool> TrapDoorFilter = mod => mod.Tile.Type == TiledConfiguration.instance.TrapDoorClass;

        [HideInInspector]
        public TiledTile tile;

        [HideInInspector, SerializeField]
        public TDSidesClass _sides = new TDSidesClass();
        private SideOverride temporarySideNegator = new SideOverride();

        public enum SideCheckMode { Has, Entry, Exit };
        public bool HasSide(Direction direction) =>
            _sides.Has(direction) && !temporarySideNegator.Overrides(direction);
        public bool HasSide(Direction direction, SideCheckMode sideCheckMode)
        {
            switch (sideCheckMode)
            {
                case SideCheckMode.Has:
                    return HasSide(direction);
                case SideCheckMode.Entry:
                    if (temporaryEntryBlocker.Overrides(direction)) return true;
                    if (temporaryEntryNegator.Overrides(direction)) return false;
                    return HasSide(direction);
                case SideCheckMode.Exit:
                    if (temporaryExitBlocker.Overrides(direction)) return true;
                    if (temporaryExitNegator.Overrides(direction)) return false;
                    return HasSide(direction);
            }
            return false;
        }

        private SideOverride temporaryEntryBlocker = new SideOverride();
        private SideOverride temporaryEntryNegator = new SideOverride();
        private SideOverride temporaryExitBlocker = new SideOverride();
        private SideOverride temporaryExitNegator = new SideOverride();
        public void AddEntryBlocker(Direction direction, MonoBehaviour behaviour) =>
            temporaryEntryBlocker.AddOverride(direction, behaviour);
        public void RemoveEntryBlocker(Direction direction, MonoBehaviour behaviour) =>
            temporaryEntryBlocker.RemoveOverride(direction, behaviour);
        public void AddExitBlocker(Direction direction, MonoBehaviour behaviour) =>
            temporaryExitBlocker.AddOverride(direction, behaviour);
        public void RemoveExitBlocker(Direction direction, MonoBehaviour behaviour) =>
            temporaryExitBlocker.RemoveOverride(direction, behaviour);
        public void AddEntryNegator(Direction direction, MonoBehaviour behaviour) =>
            temporaryEntryNegator.AddOverride(direction, behaviour);
        public void RemoveEntryNegator(Direction direction, MonoBehaviour behaviour) =>
            temporaryEntryNegator.RemoveOverride(direction, behaviour);
        public void AddExitNegator(Direction direction, MonoBehaviour behaviour) =>
            temporaryExitNegator.AddOverride(direction, behaviour);
        public void RemoveExitNegator(Direction direction, MonoBehaviour behaviour) =>
            temporaryExitNegator.RemoveOverride(direction, behaviour);
        public void AddSideNegator(Direction direction, MonoBehaviour behaviour) =>
            temporarySideNegator.AddOverride(direction, behaviour);
        public void RemoveSideNegator(Direction direction, MonoBehaviour behaviour) =>
            temporarySideNegator.RemoveOverride(direction, behaviour);

        public void UpdateSide(Direction direction, bool value)
            => _sides.Set(direction, value);
        public void UpdateSides(TDSidesClass sides)
        {
            _sides = sides;
        }

        [HideInInspector]
        public TileModification[] modifications;

        [HideInInspector]
        public TiledObjectLayer.Point[] Points;

        [HideInInspector]
        public TiledObjectLayer.Rect[] Rects;

        TDNodeConfig _config;
        public TDNodeConfig Config
        {
            get
            {
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

        string SideInfo(Direction direction)
        {
            var info = "";
            if (_sides.Has(direction)) { info += "T"; } else { info += "F"; }
            if (temporarySideNegator.Overrides(direction)) { info += "-S"; }
            if (temporaryEntryBlocker.Overrides(direction)) { info += "En"; }
            if (temporaryEntryNegator.Overrides(direction)) { info += "-En"; }
            if (temporaryExitBlocker.Overrides(direction)) { info += "Ex"; }
            if (temporaryExitNegator.Overrides(direction)) { info += "-Ex"; }


            return info;
        }

        [ContextMenu("Info: General")]
        void InfoGeneral()
        {
            var occupants = Occupants.Count() == 0 ?
                "Empty tile" :
                $"Occupants: {string.Join(", ", Occupants.Select(e => $"{e.Identifier} ({e.EntityType})"))}";

            var reservations = _reservations.Count == 0 ? "No reservations" : $"Reservations: {string.Join(", ", _reservations.Select(e => $"{e.Identifier} ({e.EntityType})"))}";
            Debug.Log(PrefixLogMessage(
                $"{occupants}. {reservations}. " +
                $"Sides: {string.Join(" ", DirectionExtensions.AllDirections.Select(d => $"{d}({SideInfo(d)})"))}." +
                $"Active Style({NodeStyle}) {Points.Length} points and {Rects.Length} rects on this node"));
        }

        [ContextMenu("Info: Modifications")]
        void InfoMods()
        {
            Debug.Log(string.Join(", ", Config.Modifications.Select(v => v)));
        }

        public Vector3 CenterPosition => Coordinates.ToPosition(Dungeon.GridSize) + (Vector3.up * Dungeon.GridSize * 0.5f);

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
           HasSide(Direction.Down) &&
            !HasFloorHole &&
            (HasTrapDoor == false || Door?.FullyClosed == true) &&
            !HasIllusion(Direction.Down);

        public bool HasCubeFace(Direction direction) =>
            HasSide(direction);

        public bool HasIllusorySurface(Direction direction)
        {
            return GetComponentsInChildren<TDIllusoryCubeSide>()
                .Any(s => s.CubeFace == direction);
        }

        public bool HasCeiling => HasSide(Direction.Up);

        // Can't check doors this simply because they can be trapdoors
        public bool Obstructed =>
            (Spikes == null ? false : Spikes.BlockingEntry) ||
            (Container == null ? false : Container.BlockingPassage);


        public string NodeStyle => Config.FirstValue(
            props => props.String(TiledConfiguration.instance.ObjVariantStyleKey),
            v => v != null);

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

        public bool HasActiveTeleporter => modifications.Any(m =>
        {
            if (m.Tile.Type != TiledConfiguration.instance.TeleporterClass) return false;
            return m.Tile.CustomProperties.Transition(TiledConfiguration.instance.TransitionKey).HasEntry();
        });

        public int TeleporterWormholdId =>
            Config
            .FirstValue(
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

        public bool HasFloorHole => modifications.Any(mod => mod.Tile.Type == TiledConfiguration.instance.HoleClass);

        public TileModification StairsModification =>
            modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.InstanceOrCreate().StairsClass);

        public bool HasStairs => StairsModification != null;

        public TileModification PressurePlateModification =>
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

        bool HasWall(Direction direction, SideCheckMode sideCheckMode)
        {
            if (direction.IsPlanarCardinal()) return HasSide(direction, sideCheckMode);
            return false;
        }

        public MovementOutcome AllowsTransition(
            GridEntity entity,
            Vector3Int origin,
            Direction originAnchorDirection,
            Direction direction,
            out Vector3Int targetCoordinates,
            out Anchor targetAnchor,
            bool checkOccupancyRules = true,
            bool withPush = false
        )
        {
            // TODO: CanAnchorOn should consider entity abilities if entity can climb or not and such...
            var originAnchor = GetAnchor(originAnchorDirection);

            if (direction == Direction.None)
            {
                // This warning shouldn't be commented out because this truely should never happen
                Debug.LogWarning(PrefixLogMessage($"{entity.name} asked for a None transition"));
                targetAnchor = originAnchor;
                targetCoordinates = origin;
                return MovementOutcome.Refused;
            }

            if (HasBlockingDoor(direction))
            {
                Debug.LogWarning(PrefixLogMessage($"{entity.name} blocked by door in {direction}"));
                targetAnchor = originAnchor;
                targetCoordinates = origin;
                return MovementOutcome.Blocked;
            }

            if (originAnchor != null)
            {
                var constraint = originAnchor.Constraint;
                if (constraint != null && constraint.RefuseTranslation(direction))
                {
                    targetAnchor = originAnchor;
                    targetCoordinates = origin;
                    Debug.Log(PrefixLogMessage($"{direction} refused because of anchor contraints"));
                    return MovementOutcome.Refused;
                }

                targetAnchor = originAnchor.GetNeighbour(direction, entity, out var outcome);

                if (outcome.Either(MovementOutcome.Refused, MovementOutcome.Blocked))
                {
                    // Debug.LogWarning(PrefixLogMessage($"There is no valid neighbour ({outcome}) in the {direction} for {originAnchor}"));
                    targetCoordinates = targetAnchor == null ? origin : targetAnchor.Node.Coordinates;
                    return outcome;
                }
            }

            targetCoordinates = Neighbour(entity, originAnchorDirection, entity.Down, direction, out targetAnchor);
            var translationTarget = Dungeon[targetCoordinates];

            if (translationTarget == null) return MovementOutcome.Refused;

            if (checkOccupancyRules && !translationTarget.MayInhabit(entity, direction, withPush))
            {
                Debug.Log(PrefixLogMessage($"Occupancy rules refused target {translationTarget} with push({withPush})"));
                return MovementOutcome.Blocked;
            }

            if (targetCoordinates == origin)
            {

                if (targetAnchor == null || !CanAnchorOn(entity, targetAnchor.CubeFace))
                {
                    Debug.LogWarning(PrefixLogMessage($"{entity.name} blocked by refused anchor on {direction}"));
                    targetAnchor = originAnchor;
                    targetCoordinates = origin;
                    return MovementOutcome.Blocked;
                }

                return MovementOutcome.NodeInternal;
            }

            var simpleTranslationCoordinates = direction.Translate(origin);
            if (simpleTranslationCoordinates == targetCoordinates)
            {
                if (AllowExit(entity, direction, withPush) && (translationTarget?.AllowsEntryFrom(entity, direction.Inverse(), checkOccupancyRules, withPush: withPush) ?? false))
                {
                    return MovementOutcome.NodeExit;
                }


                /*
            if (withPush)
            {
                Debug.LogWarning(PrefixLogMessage(
                    $"Simple translation refused for {entity.name} {origin}({direction})->{targetCoordinates}, " +
                    $"AllowExit({AllowExit(entity, direction)}) Target({translationTarget !=null}) " +
                    $"AllowEntry({(translationTarget?.AllowsEntryFrom(entity, direction.Inverse(), checkOccupancyRules, withPush: withPush) ?? false)})"));
                Debug.LogWarning(PrefixLogMessage(
                    $"(!HasSide({!HasSide(direction, SideCheckMode.Exit)}) || " +
                    $"HasOpenTrapDoor({HasOpenTrapdoorDoor(direction)}) || " +
                    $"HasIllusion({HasIllusion(direction)}) || " +
                    $"{direction} == Direction.Down && HasFloorHole({HasFloorHole})) && " +
                    $"!temporaryExitBlocker.Overrides({direction}) [{!temporaryExitBlocker.Overrides(direction)}] && " +
                    $"!BlockEdgeTraversal(entity, {direction}, SideCheckMode.Exit) [{!BlockEdgeTraversal(entity, direction, SideCheckMode.Exit)}]"));
            }
                */

                targetAnchor = originAnchor;
                targetCoordinates = origin;
                return MovementOutcome.Blocked;
            }

            var secondaryTranslation = (targetCoordinates - simpleTranslationCoordinates).AsDirectionOrNone();

            if (secondaryTranslation == Direction.None)
            {
                // This logging should not be commented out because this truely should never happen
                Debug.LogError(PrefixLogMessage($"Translation {direction} from {origin} to {translationTarget} " +
                    $"caused unexpected secondary translation of {targetCoordinates - simpleTranslationCoordinates}"));

                targetAnchor = originAnchor;
                targetCoordinates = origin;
                return MovementOutcome.Refused;
            }

            var options = new List<List<Direction>>()
            {
                new List<Direction>() { direction, secondaryTranslation },
                new List<Direction>() { secondaryTranslation, direction },
            };

            if (options.Any(translations =>
            {
                var coordinates = origin;
                // Debug.Log(PrefixLogMessage($"Checking complex transition sequence {string.Join(", ", translations)}"));
                foreach (var direction in translations)
                {
                    if (!Dungeon[coordinates]?.AllowExit(entity, direction, withPush) ?? false)
                    {
                        // Debug.Log(PrefixLogMessage($"Exit from {coordinates} -> {direction} not allowed"));
                        return false;
                    }

                    coordinates = direction.Translate(coordinates);
                    if (
                        !(Dungeon[coordinates]?.AllowsEntryFrom(entity, direction.Inverse(), checkOccupancyRules, withPush: withPush) ?? false)
                    )
                    {
                        // Debug.Log(PrefixLogMessage($"Entry to {coordinates} -> {direction.Inverse()} not allowed"));
                        return false;
                    }
                }
                return true;
            }))
            {
                return MovementOutcome.NodeExit;
            }

            Debug.Log(PrefixLogMessage($"No complex movement allowed ({direction}, {secondaryTranslation}) to {translationTarget}"));
            targetAnchor = originAnchor;
            targetCoordinates = origin;
            return MovementOutcome.Blocked;
        }

        public bool HasOpenTrapdoorDoor(Direction direction)
        {
            var door = Door;

            if (door == null) return false;
            Debug.Log(PrefixLogMessage($"{door} Trapdoor({HasTrapDoor})"));

            if (HasTrapDoor)
            {
                return direction == Direction.Down && !door.BlockingPassage;
            }

            return false;
        }

        public bool HasBlockingDoor(Direction entryDirection)
        {
            var door = Door;

            if (door == null) return false;
            Debug.Log(PrefixLogMessage($"{door} Trapdoor({HasTrapDoor})"));

            if (HasTrapDoor)
            {
                return entryDirection == Direction.Down && door.BlockingPassage;
            }

            return door.BlocksEntryFrom(entryDirection);
        }

        private bool FallingEntityPushResque(GridEntity entity, Direction direction)
        {
            if (entity.Falling && direction == Direction.Up)
            {
                // Entity shouldn't really end up here but we'll need to try and push it to the side
                var pushDirection = DirectionExtensions.AllDirections.Where(d => d.IsPlanarCardinal()).Where(d =>
                {
                    if (AllowExit(entity, d, true))
                    {
                        var n = Dungeon[Neighbour(d)];
                        // We don't check occupancy rules because we'll have to push
                        // others if it comes to shove.
                        return n != null && n.AllowsEntryFrom(entity, d.Inverse(), false, true);
                    }
                    return false;
                }).ToList().GetRandomElementOrDefault();

                if (pushDirection != Direction.None)
                {
                    Debug.Log(PrefixLogMessage($"Rescue pushing {entity} {pushDirection}"));
                    entity.InjectForcedMovement(pushDirection.AsMovement(entity.LookDirection, entity.Down));
                    return true;
                }
            }

            return false;
        }

        public bool BlockEdgeTraversal(GridEntity entity, Direction direction, SideCheckMode sideCheckMode)
        {
            /*
            Debug.Log(PrefixLogMessage($"Entry Negators: {string.Join(", ", temporaryEntryNegator)}"));
            Debug.Log(PrefixLogMessage($"Exit Negators: {string.Join(", ", temporaryExitNegator)}"));
            Debug.Log(PrefixLogMessage($"Entry Blocker: {string.Join(", ", temporaryEntryBlocker)}"));
            Debug.Log(PrefixLogMessage($"Exit Blocker: {string.Join(", ", temporaryExitBlocker)}"));
            */

            if (sideCheckMode == SideCheckMode.Has && temporarySideNegator.Overrides(direction)) return false;

            if (sideCheckMode == SideCheckMode.Exit && temporaryExitNegator.Overrides(direction)) return false;
            if (sideCheckMode == SideCheckMode.Exit && temporaryExitBlocker.Overrides(direction)) return true;

            if (sideCheckMode == SideCheckMode.Entry && temporaryEntryNegator.Overrides(direction)) return false;
            if (sideCheckMode == SideCheckMode.Entry && temporaryEntryBlocker.Overrides(direction)) return !FallingEntityPushResque(entity, direction);

            var flying = entity != null && entity.TransportationMode.HasFlag(TransportationMode.Flying);

            if (modifications.Any(mod =>
            {
                var modDirection = mod.Tile.CustomProperties
                    .Direction(TiledConfiguration.InstanceOrCreate().DirectionKey, TDEnumDirection.None)
                    .AsDirection();

                if (direction != modDirection && modDirection != Direction.None) return false;

                if (flying)
                {
                    var flyability = mod.Tile.CustomProperties
                        .Aspect(TiledConfiguration.InstanceOrCreate().FlyabilityKey, TDEnumAspect.Always);
                    return flyability == TDEnumAspect.Never;
                }
                var walkability = mod.Tile.CustomProperties
                    .Aspect(TiledConfiguration.InstanceOrCreate().WalkabilityKey, TDEnumAspect.Always);
                /*
                if (walkability == TDEnumAspect.Never)
                {
                    Debug.Log(PrefixLogMessage($"Walkability never for aspect {TiledConfiguration.instance.WalkabilityKey}"));
                    var value = mod.Tile.CustomProperties.StringEnums.GetValueOrDefault(TiledConfiguration.instance.WalkabilityKey);
                    Debug.Log(PrefixLogMessage($"From tile {mod.Tile.Id}: {value.Value} ({value.TypeName})"));
                }
                Debug.Log(PrefixLogMessage($"Walkability: {walkability}"));
                */
                return walkability == TDEnumAspect.Never;
            })) return true && !FallingEntityPushResque(entity, direction);

            return Config.FirstValue(TiledConfiguration.instance.FenceClass, p =>
                p.Aspect(
                    flying ? TiledConfiguration.InstanceOrCreate().FlyabilityKey : TiledConfiguration.InstanceOrCreate().WalkabilityKey,
                    TDEnumAspect.Always
                )) == TDEnumAspect.Never && !FallingEntityPushResque(entity, direction);
        }

        public bool AllowExit(GridEntity entity, Direction direction, bool forced) =>
            (!HasSide(direction, SideCheckMode.Exit) || HasOpenTrapdoorDoor(direction) || HasIllusion(direction)
            || (direction == Direction.Down && HasFloorHole)) &&
            !temporaryExitBlocker.Overrides(direction) &&
            (forced || !BlockEdgeTraversal(entity, direction, SideCheckMode.Exit));

        bool CanPushEntity(GridEntity entity, Direction pushDirection, out Direction direction)
        {
            if (entity.Coordinates != Coordinates)
            {
                Debug.LogWarning(PrefixLogMessage($"{entity} isn't at my coordinates, should not be push checking it"));
            }

            var forbiddenDirection = pushDirection.Inverse();

            if (forbiddenDirection == Direction.None)
            {
                Debug.LogError(PrefixLogMessage($"We need a valid push source to allow pushing."));
                direction = Direction.None;
                return false;
            }

            // We don't push up if we don't have to but if we can push down we do
            foreach (var checkDirection in DirectionExtensions
                .AllDirections
                .OrderBy(d => d == pushDirection ? 0 : 1)
                .ThenBy(d => d == Direction.Up ? 1 : 0)
                .ThenBy(d => d == Direction.Down ? -1 : 0))
            {

                // We would have stack overflow if we are allowed pushing back and forth
                if (checkDirection == forbiddenDirection) continue;

                if (AllowsTransition(
                    entity,
                    entity.Coordinates,
                    entity.AnchorDirection,
                    checkDirection,
                    out var _,
                    out var _,
                    withPush: true) == MovementOutcome.NodeExit)
                {
                    direction = checkDirection;
                    return true;
                }
            }
            Debug.LogWarning(PrefixLogMessage($"Found no way to push entity {entity.name}"));
            direction = Direction.None;
            return false;
        }

        bool PushEntity(GridEntity pushed, Direction pushDirection)
        {
            if (CanPushEntity(pushed, pushDirection, out var direction))
            {
                var movement = direction.AsMovement(pushed.LookDirection, pushed.Down);
                if (movement.IsRotation())
                {
                    // Doing absolute movements if needed
                    movement = direction.AsMovement();
                }
                Debug.Log(PrefixLogMessage($"Pushing entity {pushed.name} {direction} as {movement}"));
                pushed.InjectForcedMovement(movement, ElasticGameClock.instance.baseTickDuration);
                return true;
            }
            Debug.LogWarning(PrefixLogMessage($"Found no way to push entity {pushed.name}"));
            return false;
        }

        /// <summary>
        /// Used if active entity must have higher precedence to the node. Typically when falling
        /// </summary>
        /// <param name="activeEntity">The entity attempting entry to the tile</param>
        /// <returns></returns>
        public bool PushOccupants(GridEntity activeEntity, Direction pushDirection)
        {
            // Since the action may mutate occupants we need to copy the collection first
            foreach (var occupant in Occupants.ToList())
            {
                if (occupant == activeEntity) continue;
                Debug.Log(PrefixLogMessage($"{activeEntity} needs to push {occupant} {pushDirection}"));
                if (!PushEntity(occupant, pushDirection)) return false;
            }
            return true;
        }

        /// <summary>
        /// Used if active entity must have higher precedence to the node. Typically when falling
        /// </summary>
        /// <param name="activeEntity">The entity attempting entry to the tile</param>
        /// <returns></returns>
        bool RefuseReservations(GridEntity activeEntity, Direction pushDirection)
        {
            // Since the action may mutate resevations we need to copy the collection first
            foreach (var reservation in _reservations.ToList())
            {
                if (reservation == activeEntity) continue;
                if (!PushEntity(reservation, pushDirection)) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if entity can inhabit tile.
        /// 
        /// If movement is prioritized, use checkPush to circumvent occupation
        /// and reservation rules as far as possible
        /// </summary>
        public bool MayInhabit(GridEntity entity, Direction pushDirection, bool checkPush)
        {
            if (!OccupationRules.MayCoexist(entity, Occupants, out var occupationConflicts))
            {

                if (!checkPush || !occupationConflicts.All(occupant => CanPushEntity(occupant, pushDirection, out var _)))
                {
                    var occupants = occupationConflicts.Select(o => o.name);
                    Debug.LogWarning(PrefixLogMessage($"{entity.name} may not enter because of occupants: {string.Join(", ", occupants)}"));
                    return false;
                }
            }
            if (!OccupationRules.MayCoexist(entity, _reservations, out var reservationConflicts))
            {
                if (!checkPush)
                {
                    var reserves = reservationConflicts.Select(r => r.name);
                    Debug.LogWarning(PrefixLogMessage($"{entity.name} may not enter because of reservations: {string.Join(", ", reserves)}"));
                    return false;
                }
            }

            return true;
        }

        public bool AllowsEntryFrom(
            GridEntity entity,
            Direction direction,
            bool checkOccupancyRules = true,
            bool withPush = false)
        {
            if (checkOccupancyRules && !MayInhabit(entity, direction.Inverse(), withPush))
            {
                return false;
            }

            if (HasWall(direction, SideCheckMode.Entry))
            {
                // Debug.Log(PrefixLogMessage($"Entry blocked by wall in {direction} for {entity.name}"));
                return false;
            }

            if (HasLadder(direction))
            {
                // Debug.Log(PrefixLogMessage($"Entry blocked by ladder in {direction} for {entity.name}"));
                return false;
            }

            if (HasBlockingDoor(direction))
            {
                // Debug.Log(PrefixLogMessage($"Entry blocked by door for {entity.name}"));
                return false;
            }

            if (Obstructed)
            {
                // Debug.Log(PrefixLogMessage($"Entry blocked by obstruction {entity.name}"));
                return false;
            }

            var ramp = RampModification;
            if (ramp != null && direction.IsPlanarCardinal())
            {
                var axis = direction.AsAxis();
                if (ramp.Tile.CustomProperties.Elevation(TiledConfiguration.instance.ElevationKey) != TDEnumElevation.Low &&
                    ramp.Tile.CustomProperties.Direction(TiledConfiguration.instance.DownDirectionKey).AsDirection().AsAxis() != axis)
                {
                    // Debug.LogWarning(PrefixLogMessage($"{entity.name} blocked by non-low ramp"));
                    return false;
                }
            }

            // Cannot check if there's any modification blocking walking here because could be mod doesn't affect the cube face
            // or edge transition we are interested in...

            if (BlockEdgeTraversal(entity, direction, SideCheckMode.Entry))
            {
                // Debug.Log(PrefixLogMessage($"Entry blocked by edge traversal for {entity.name}"));
                return false;
            }

            var platform = GetComponentInChildren<TDMovingPlatform>();
            if (platform != null)
            {
                var entry = platform.MayEnter(entity);
                if (!entry)
                {
                    // Debug.Log(PrefixLogMessage($"Entry blocked by moving platform for {entity.name}"));
                }
                return entity;
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
            if (movement != Movement.None)
            {
                Debug.Log(PrefixLogMessage($"Spinning {entity.name} {movement}"));
                entity.InjectForcedMovement(movement);
            }
        }

        void AddNewOccupant(GridEntity entity)
        {
            _occupants.Add(entity);
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
                        _occupants.Add(entity);
                        HandleSpinner(entity);
                    }
                }
            }

            if (target == null || target == (IDungeonNode)this)
            {
                AddNewOccupant(entity);
            }
            else
            {
                target.AddOccupant(entity, true);
            }
        }

        #region EntityOccupation
        HashSet<GridEntity> _occupants = new HashSet<GridEntity>();
        HashSet<GridEntity> _reservations = new HashSet<GridEntity>();

        public void AddOccupant(GridEntity entity, bool push)
        {
            // Debug.Log(PrefixLogMessage($"Handling occupancy of {entity.name}"));

            bool newOccupation = !_occupants.Contains(entity);

            if (newOccupation)
            {
                if (push)
                {
                    // We don't know the direction so lets be as neutral as possible
                    PushOccupants(entity, Direction.Down);
                }
                OccupationRules.HandleMeeting(entity, _occupants);
            }
            _reservations.Remove(entity);

            if (newOccupation) OnNewOccupant?.Invoke(this, entity);

            if (IsTrap)
            {
                HandleTraps(entity, newOccupation);
            }
            else
            {
                AddNewOccupant(entity);
            }

            if (newOccupation) entity.transform.SetParent(transform);
        }

        public IEnumerable<GridEntity> Occupants => _occupants;
        public IEnumerable<GridEntity> Reservations => _reservations;

        public void RemoveReservation(GridEntity entity)
        {
            if (_reservations.Contains(entity))
            {
                // Debug.Log(PrefixLogMessage($"Removing reservation from {entity}"));
                _reservations.Remove(entity);
            }
        }

        public void Reserve(GridEntity entity)
        {
            if (_occupants.Contains(entity)) return;

            // Debug.Log($"Reserving {entity.name} occupancy for {Coordinates}");
            _reservations.Add(entity);
        }

        public void RemoveOccupant(GridEntity entity)
        {
            // Debug.Log($"Removing {entity.name} occupancy from {Coordinates}");
            _occupants.Remove(entity);
            OccupationRules.HandleDeparture(entity, _occupants);
            if (entity.transform.parent == transform)
            {
                entity.transform.SetParent(Dungeon.transform);
            }
        }
        #endregion

        /// <summary>
        /// Answers if rotation is allowed
        /// </summary>
        public bool AllowsRotation(GridEntity entity, Movement movement)
        {
            if (!movement.IsRotation()) return false;

            var anchor = entity.NodeAnchor;

            // We are in the air, if we can be here and move we can rotate all we want
            if (anchor == null) return true;

            var constraint = anchor.Constraint;

            if (constraint == null) return true;

            return !constraint.RefuseLookDirection(entity.LookDirection.ApplyRotation(entity.Down, movement, out var _));
        }

        public bool CanAnchorOn(GridEntity entity, Direction anchor)
        {
            if (Obstructed) return false;

            if (anchor == Direction.None) return entity.TransportationMode.HasFlag(TransportationMode.Flying);

            if (anchor == Direction.Down && !HasFloor) return false;

            // Can't fly on a walls
            if (entity.TransportationMode.HasFlag(TransportationMode.Flying)) return false;

            if (entity.TransportationMode.HasFlag(TransportationMode.Walking))
            {
                if (modifications.Any(mod => mod.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey, TDEnumDirection.None).AsDirection() == anchor &&
                    mod.Tile.CustomProperties.Aspect(TiledConfiguration.instance.WalkabilityKey, TDEnumAspect.Always) == TDEnumAspect.Never)) return false;
            }

            return GetComponentsInChildren<Anchor>().Any(a => a.CubeFace == anchor && a.Traversal.CanBeTraversedBy(entity));
        }

        Vector3Int Neighbour(GridEntity entity, Direction anchorDirection, Direction down, Direction direction, out Anchor neighbourAnchor)
        {
            // Though flying or falling entities don't attach to down, they follow their own down
            var currentAnchorDirection = anchorDirection;
            if (currentAnchorDirection == Direction.None)
            {
                currentAnchorDirection = down;
            }

            var currentDownAnchor = GetAnchor(currentAnchorDirection);
            if (currentDownAnchor == null)
            {
                neighbourAnchor = null;
                return Neighbour(direction);
            }

            neighbourAnchor = currentDownAnchor.GetNeighbour(direction, entity, out var outcome);

            if (neighbourAnchor == null)
            {
                return Neighbour(direction);
            }
            else if (outcome.Either(MovementOutcome.Blocked, MovementOutcome.Refused))
            {
                return Coordinates;
            }

            return neighbourAnchor.Node.Coordinates;
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
