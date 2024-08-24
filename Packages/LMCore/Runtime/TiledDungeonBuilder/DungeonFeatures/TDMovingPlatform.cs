using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDMovingPlatform : MonoBehaviour
    {
        public enum Phase { 
            Initial, 
            WaitingStart, 
            Moving, 
            WaitingEnd,
            Ended 
        };

        Phase phase;

        [SerializeField, Tooltip("Time waiting when invoking loop condition")]
        float loopDelay = 2f;

        [SerializeField, Tooltip("As Tile distance per second")]
        float moveSpeed = 1f;

        [SerializeField, HideInInspector]
        Vector3Int OriginCoordinates;

        Vector3Int CurrentCoordinates => GetComponentInParent<TDNode>().Coordinates;

        TiledDungeon _dungeon;
        TiledDungeon Dungeon { 
            get { 
                if (_dungeon == null)
                {
                    _dungeon = GetComponentInParent<TiledDungeon>(); 
                }
                return _dungeon;
            } 
        }

        [SerializeField, HideInInspector]
        Direction MoveDirection = Direction.None;

        [SerializeField, HideInInspector]
        TDEnumLoop Loop = TDEnumLoop.None;

        [SerializeField, HideInInspector]
        TDEnumInteraction Interaction = TDEnumInteraction.Automatic;

        protected string PrefixLogMessage(string message) => $"Moving Platform {CurrentCoordinates} (origin {OriginCoordinates}): {message}";
        
        public void Configure(TDNodeConfig conf)
        {
            OriginCoordinates = conf.Coordinates;

            var platform = conf.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.MovingPlatformClass); 
            if (platform == null)
            {
                Debug.LogWarning(PrefixLogMessage("Could not find any configuration"));
                return;
            }

            MoveDirection = platform.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection();
            Loop = platform.Loop(TiledConfiguration.instance.LoopKey);
            Interaction = platform.Interaction(TiledConfiguration.instance.InteractionKey, TDEnumInteraction.Automatic);
            moveSpeed = platform.Float(TiledConfiguration.instance.VelocityKey, moveSpeed);
            loopDelay = platform.Float(TiledConfiguration.instance.PauseKey, loopDelay);
        }

        [SerializeField, HideInInspector]
        SerializableDictionary<Vector3Int, Direction> managedOffsetSides = new SerializableDictionary<Vector3Int, Direction>();

        public bool IsSamePlatform(Vector3Int coordinates, Direction anchor)
        {
            var offset = coordinates - CurrentCoordinates;
            if (offset == Vector3Int.zero && anchor == Direction.Down) return true;

            return managedOffsetSides.Any(kvp => kvp.Key == offset && kvp.Value == anchor);
        }

        ConstraintSource constraintSource => new ConstraintSource() { sourceTransform = transform, weight = 1 };
        public void AddAttachedObject(Transform attached, Direction cubeSide)
        {
            var constraint = attached.gameObject.AddComponent<PositionConstraint>();
            constraint.AddSource(constraintSource);
            constraint.constraintActive = true;

            if (cubeSide == Direction.None) return;

            var otherNode = attached.GetComponentInParent<TDNode>();
            var offset = otherNode.Coordinates - GetComponentInParent<TDNode>().Coordinates;

            managedOffsetSides.Add(offset, cubeSide);
        }

        public bool MayEnter(GridEntity entity) { 
            if (entity.TransportationMode.HasFlag(TransportationMode.Flying)) return true;

            if (entity.Anchor == Direction.Down) {
                var myCoordinates = CurrentCoordinates;
                foreach (var dependant in managedOffsetSides)
                {
                    // We're part of the same platform!
                    if (myCoordinates + dependant.Key == entity.Position && dependant.Value == Direction.Down)
                    {
                        return true;
                    }
                }
            }

            return AlignedWithGrid;
        }

        HashSet<GridEntity> constrainedEntities = new HashSet<GridEntity>();

        public bool ConstrainEntity(GridEntity entity)
        {
            var constraint = entity.GetComponent<PositionConstraint>();
            if (constraint == null)
            {
                constraint = entity.gameObject.AddComponent<PositionConstraint>();
            }

            while (constraint.sourceCount > 0)
            {
                constraint.RemoveSource(0);
            }

            constraint.AddSource(constraintSource);
            constraint.weight = 0;
            constraint.constraintActive = true;

            constrainedEntities.Add(entity);

            return true;
        }

        public bool FreeEntity(GridEntity entity)
        {
            constrainedEntities.Remove(entity);

            var constraint = entity.GetComponent<PositionConstraint>();
            if (constraint == null) { return false; }

            for (int i = 0, l = constraint.sourceCount; i < l; i++)
            {
                var source = constraint.GetSource(i);
                if (source.sourceTransform == transform)
                {
                    constraint.RemoveSource(i);
                    constraint.constraintActive = constraint.sourceCount == 0;
                    if (l == 1)
                    {
                        constraint.constraintActive = false;
                    }
                    return true;
                }
            }

            return false;
        }

        float nextPhase;

        private void Start()
        {
            if (phase == Phase.Initial && Interaction == TDEnumInteraction.Automatic)
            {
                Debug.Log(PrefixLogMessage($"Starting platform Interaction({Interaction}) Loop({Loop}) MoveDirection({MoveDirection})"));
                InitWaitToStart();
            }
        }

        System.Action ActivePhaseFunction;

        void InitWaitToStart()
        {
            Debug.Log(PrefixLogMessage($"Init start wait before move {MoveDirection}"));
            phase = Phase.WaitingStart;
            nextPhase = Time.timeSinceLevelLoad + loopDelay / 2;
            ActivePhaseFunction = HandleWaitToStart;
        }


        void HandleWaitToStart()
        {
            if (phase != Phase.WaitingStart)
            {
                Debug.LogError(PrefixLogMessage($"Unexpected phase {phase} while waiting to start"));
                return;
            }

            if (Time.timeSinceLevelLoad > nextPhase)
            {
                ActivePhaseFunction = null;
                InitMoveStep();
            }
        }

        void InitWaitEnd()
        {
            Debug.Log(PrefixLogMessage($"Init move end wait at {CurrentCoordinates}"));
            phase = Phase.WaitingEnd;
            nextPhase = Time.timeSinceLevelLoad + loopDelay / 2;

            ActivePhaseFunction = HandleWaitToEnd;
        }

        void HandleWaitToEnd()
        {
            if (phase != Phase.WaitingEnd)
            {
                Debug.LogError(PrefixLogMessage($"Unexpected phase {phase} while waiting to start"));
                return;
            }

            if (Time.timeSinceLevelLoad > nextPhase)
            {
                switch (Loop)
                {
                    case TDEnumLoop.None:
                        phase = Phase.Ended;
                        ActivePhaseFunction = null;
                        Debug.Log(PrefixLogMessage("Movements completed"));
                        break;
                    case TDEnumLoop.Bounce:
                        MoveDirection = MoveDirection.Inverse();
                        InitWaitToStart();
                        break;
                    case TDEnumLoop.Wrap:
                        if (BecomeTile(OriginCoordinates, true))
                        {
                            InitWaitToStart();
                        }
                        break;
                }
            }
        }

        bool CanTranslate(Direction direction)
        {
            // TODO: Handle offsets too!
            var source = Dungeon[CurrentCoordinates];
            if (source.AllowsMovement(null, Direction.None, MoveDirection) != MovementOutcome.NodeExit)
            {
                return false;
            }

            var target = direction.Translate(CurrentCoordinates);
            if (Dungeon.HasNodeAt(target))
            {
                var node = Dungeon[target];

                return !node.HasFloor;
            }

            return true;
        }

        bool BecomeTile(Vector3Int coordinates, bool translate = false)
        {
            var currentNode = GetComponentInParent<TDNode>();

            if (currentNode.Coordinates == coordinates)
            {
                Debug.LogWarning(PrefixLogMessage($"I'm already at {coordinates}"));

                if (translate)
                {
                    transform.localPosition = Vector3.zero;
                }
                return true;
            }

            currentNode.UpdateSide(Direction.Down, false);
            foreach (var dependent in managedOffsetSides)
            {
                var otherCoordinates = dependent.Key + currentNode.Coordinates;
                if (!Dungeon.HasNodeAt(otherCoordinates)) continue;

                Dungeon[otherCoordinates].UpdateSide(dependent.Value, false);
            }

            if (Dungeon.HasNodeAt(coordinates))
            {
                Debug.Log(PrefixLogMessage($"I'm becomming {coordinates}"));
                var newNode = Dungeon[coordinates];

                transform.SetParent(newNode.transform);

                newNode.UpdateSide(Direction.Down, true);

                foreach (var dependent in managedOffsetSides)
                {
                    var otherCoordinates = dependent.Key + currentNode.Coordinates;
                    if (!Dungeon.HasNodeAt(otherCoordinates))
                    {
                        Debug.LogWarning(PrefixLogMessage($"Could not set dependent side {dependent.Value} because dungeon lacks node at {otherCoordinates}"));
                        continue;
                    }

                    Dungeon[otherCoordinates].UpdateSide(dependent.Value, true);
                }

                foreach (var entity in constrainedEntities)
                {
                    var offset = entity.Position - currentNode.Coordinates;
                    var newEntityCoordinates = coordinates + offset;

                    if (Dungeon.HasNodeAt(entity.Position)) {
                        Dungeon[entity.Position].RemoveOccupant(entity);
                    }

                    entity.Position = newEntityCoordinates;

                    if (Dungeon.HasNodeAt(entity.Position)) {
                        Dungeon[entity.Position].AddOccupant(entity);
                    }
                }
            } else
            {
                Debug.LogWarning(PrefixLogMessage($"Could not become {coordinates} because dungeon lacks node"));
            }

            if (translate)
            {
                transform.localPosition = Vector3.zero;
            }
            return false;
        }

        public bool AlignedWithGrid { get; private set; }

        void InitMoveStep()
        {
            Debug.Log(PrefixLogMessage($"Init move {MoveDirection}"));

            if (!CanTranslate(MoveDirection))
            {
                Debug.Log(PrefixLogMessage($"I've reached end of my movement at, can't move {MoveDirection} to {MoveDirection.Translate(CurrentCoordinates)}"));
                InitWaitEnd();
                return;
            }

            var startCoordinates = CurrentCoordinates;
            var startPosition = startCoordinates.ToPosition(Dungeon.GridSize);
            var targetCoordinates = MoveDirection.Translate(CurrentCoordinates);
            var targetPosition = targetCoordinates.ToPosition(Dungeon.GridSize);
            var t0 = Time.timeSinceLevelLoad;

            ActivePhaseFunction = () =>
            {
                var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - t0) / moveSpeed);

                AlignedWithGrid = progress < 0.1f || progress > 0.9f;

                if (progress > 0.5f && CurrentCoordinates == startCoordinates)
                {
                    BecomeTile(targetCoordinates);
                }

                transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

                if (progress == 1)
                {
                    ActivePhaseFunction = InitMoveStep;
                }
            };
        }

        private void Update()
        {
            ActivePhaseFunction?.Invoke();
        }
    }
}
