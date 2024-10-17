using Codice.Client.Common.TreeGrouper;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDMovingPlatform : MonoBehaviour, IMovingCubeFace
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

        [SerializeField, HideInInspector]
        bool alwaysClaimToBeAligned;

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
        Direction OriginalDirection;

        [SerializeField, HideInInspector]
        TDEnumLoop Loop = TDEnumLoop.None;

        [SerializeField, HideInInspector]
        TDEnumInteraction Interaction = TDEnumInteraction.Automatic;

        [SerializeField, HideInInspector]
        int managedByGroup = -1;

        [SerializeField, HideInInspector]
        TDEnumLoop managedToggleEffect = TDEnumLoop.None;

        /// <summary>
        /// World position of a virtual node center misaligned with the dungeon grid
        /// </summary>
        public Vector3 VirtualNodeCenter => transform.position + (Dungeon?.GridSize ?? 3f) * 0.5f * Vector3.up;

        protected string PrefixLogMessage(string message) => $"{Interaction} Moving Platform {CurrentCoordinates} (origin {OriginCoordinates}): {message}";

        public override string ToString() => PrefixLogMessage(
            $"ByGroup({managedByGroup}) AlwaysAlign({alwaysClaimToBeAligned}) Offsets({string.Join(", ", managedOffsetSides.Select(mo => $"{mo.Offset} {mo.AnchorDirection}"))})");

        [ContextMenu("Info")]
        public void Info() => Debug.Log(this);

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
            alwaysClaimToBeAligned = platform.Bool(TiledConfiguration.instance.ClaimAlwaysAlignedKey, true);
            managedByGroup = platform.Int(TiledConfiguration.instance.ObjManagedByGroupKey, -1);
            managedToggleEffect = platform.Loop(TiledConfiguration.instance.ObjToggleEffectKey, TDEnumLoop.None);
        }

        [Serializable]
        private struct ManagedOffset
        {
            public Vector3Int Offset;
            public Direction AnchorDirection;
            public Transform Transform;
        }

        [SerializeField, HideInInspector]
        List<ManagedOffset> managedOffsetSides = new List<ManagedOffset>();

        public bool IsSamePlatform(Vector3Int coordinates, Direction anchor)
        {
            var offset = coordinates - CurrentCoordinates;
            if (offset == Vector3Int.zero && anchor == Direction.Down) return true;

            return managedOffsetSides.Any(mo => mo.Offset == offset && mo.AnchorDirection == anchor);
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

            Debug.Log(PrefixLogMessage($"Is coordinating transform with offset {offset} cube face {cubeSide}"));
            managedOffsetSides.Add(new ManagedOffset()
            {
                Offset = offset,
                AnchorDirection = cubeSide,
                Transform = attached,
            });
        }

        public bool MayEnter(GridEntity entity) {
            if (entity.TransportationMode.HasFlag(TransportationMode.Flying)) return true;

            if (entity.AnchorDirection == Direction.Down) {
                var myCoordinates = CurrentCoordinates;
                foreach (var mo in managedOffsetSides)
                {
                    // We're part of the same platform!
                    if (myCoordinates + mo.Offset == entity.Coordinates && mo.AnchorDirection == Direction.Down)
                    {
                        return true;
                    }
                }
            }

            return AlignedWithGrid;
        }

        HashSet<GridEntity> constrainedEntities = new HashSet<GridEntity>();

        private PositionConstraint AddConstraint(
            GridEntity entity,
            PositionConstraint constraint,
            Transform newConstrainer)
        {
            if (constraint == null)
            {
                constraint = entity.gameObject.AddComponent<PositionConstraint>();
            }

            bool hasMyConstraint = false;
            for (int i = 0, l = constraint.sourceCount; i < l; i++)
            {
                if (constraint.GetSource(i).sourceTransform == newConstrainer)
                {
                    hasMyConstraint = true;
                    break;
                }
            }

            if (!hasMyConstraint)
            {
                constraint.constraintActive = false;

                constraint.translationOffset = Vector3.zero;
                constraint.AddSource(new ConstraintSource() { sourceTransform = newConstrainer, weight = 1f });
                constraint.translationAtRest = Vector3.zero;
            }

            constraint.weight = 0;

            Debug.Log(PrefixLogMessage($"Constraining {entity.name}"));
            return constraint;
        }

        /// <summary>
        /// Removes all constraints that are not needed
        /// </summary>
        public PositionConstraint RemoveConstraints(GridEntity entity, Transform newAnchor)
        {
            var constraint = entity.GetComponent<PositionConstraint>();
            if (constraint == null) {
                return constraint;
            }

            for (int i = 0, l = constraint.sourceCount; i < l; i++)
            {
                var source = constraint.GetSource(i);
                if (source.sourceTransform != newAnchor && (
                    source.sourceTransform == transform || managedOffsetSides.Any(mo => mo.Transform == source.sourceTransform)))
                {
                    constraint.RemoveSource(i);
                    i--;
                    l--;

                    if (l == 0)
                    {
                        constraint.weight = 0f;
                        break;
                    }
                }
            }

            return constraint;
        }

        float nextPhase;

        private void Start()
        {

            if (phase == Phase.Initial && Interaction == TDEnumInteraction.Automatic)
            {
                Debug.Log(PrefixLogMessage($"Starting platform Interaction({Interaction}) Loop({Loop}) MoveDirection({MoveDirection})"));
                InitWaitToStart();
            }

            // TODO: Figure out this managing moving cube face business
            var anchor = GetComponent<Anchor>();
            if (anchor != null) {
                anchor.ManagingMovingCubeFace = this;
            }
        }

        ToggleGroup toggleGroup => GetComponentInParent<ToggleGroup>();

        private void OnEnable()
        {
            OriginalDirection = MoveDirection;

            // We should free when we no longer occupy and or gain new anchor that isn't us rather
            // than just any move perhaps and in either case if on move it should be when the move is progressed
            // and final enough that we aren't on our anchors anymore
            GridEntity.OnPositionTransition += GridEntity_OnTransition;
            
            if (Interaction == TDEnumInteraction.Managed && managedByGroup >= 0)
            {
                Debug.Log(PrefixLogMessage($"Registering to toggle group {managedByGroup}"));
                toggleGroup?.RegisterReciever(managedByGroup, OnToggleGroupToggle);
            }
        }

        private void OnDisable()
        {
            GridEntity.OnPositionTransition -= GridEntity_OnTransition;
            if (Interaction == TDEnumInteraction.Managed && managedByGroup >= 0)
            {
                toggleGroup?.UnregisterReciever(managedByGroup, OnToggleGroupToggle);
            }
        }

        // TODO: This needs to be saved, should probably also save current position, phase next phase what is needed
        // but no real need to save where in the transition we are
        bool isToggled = false;

        private void OnToggleGroupToggle()
        {
            isToggled = !isToggled;
            Debug.Log(PrefixLogMessage($"Toggling the platform to {isToggled}"));
            if (isToggled && managedToggleEffect == TDEnumLoop.Bounce)
            {
                if (MoveDirection != OriginalDirection)
                {
                    MoveDirection = MoveDirection.Inverse();
                    ActivePhaseFunction = null;
                    InitMoveStep();
                    Debug.Log(PrefixLogMessage($"Inverting movement direction, platform going {MoveDirection}"));
                } else if (phase != Phase.Moving && phase != Phase.WaitingStart)
                {
                    ActivePhaseFunction = null;
                    InitMoveStep();
                    Debug.Log(PrefixLogMessage($"Invoking bounce by entry, platform going {MoveDirection}"));
                }
            }
            else if (managedToggleEffect == TDEnumLoop.Bounce)
            {
                MoveDirection = MoveDirection.Inverse();
                Debug.Log(PrefixLogMessage($"Invoking bounce by exit, platform going {MoveDirection}"));
                InitWaitToStart();
            }
            else if (managedToggleEffect == TDEnumLoop.Wrap) { 
            
                if (!BecomeTile(OriginCoordinates, true))
                {
                    Debug.LogError(PrefixLogMessage("Failed to wrap around to spawn"));
                }

                ActivePhaseFunction = null;
                phase = Phase.Ended;
            }
            else if (managedToggleEffect != TDEnumLoop.None && managedToggleEffect != TDEnumLoop.Unknown)
            {
                throw new NotImplementedException($"{managedToggleEffect} by exit platforms not implemented");
            } 
        }

        private Transform ConstrainingTransform(GridEntity entity)
        {
            var entityAnchorTransform = entity.NodeAnchor?.transform;

            var myCoords = CurrentCoordinates;
            if (transform == entityAnchorTransform || entity.Coordinates == CurrentCoordinates && entity.AnchorDirection == Direction.Down)
            {
                return transform;
            }

            foreach (var mo in managedOffsetSides)
            {
                if (mo.Transform == entityAnchorTransform || entity.AnchorDirection == mo.AnchorDirection && entity.Coordinates == mo.Offset + myCoords)
                {
                    return mo.Transform;
                } 
            }
            return null;
        }

        private void GridEntity_OnTransition(GridEntity entity)
        {
            if (entity.Moving.HasFlag(MovementType.Translating))
            {
                var newConstrainer = ConstrainingTransform(entity);

                // Free all that are not new constraint
                var constraint = RemoveConstraints(entity, newConstrainer);
                // Add needed constraints

                if (newConstrainer != null)
                {
                    constraint = AddConstraint(entity, constraint, newConstrainer);
                }

                // Update tracking list 
                if (newConstrainer == null)
                {
                    constrainedEntities.Remove(entity);
                } else
                {
                    constrainedEntities.Add(entity);
                }

                if (constraint != null)
                {
                    constraint.constraintActive = constraint.sourceCount > 0;
                }
            }
        }

        Action ActivePhaseFunction;

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
                        Debug.Log(PrefixLogMessage($"Movement phase {phase}"));
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

        bool NodeSideIsPlatformOrEmpty(TDNode node, Direction direction) =>
            NodeSideIsPlatformOrEmpty(node, direction, Vector3Int.zero);
        bool NodeSideIsPlatformOrEmpty(TDNode node, Direction direction, Vector3Int offset)
        {
            if (node == null) return true;

            // TODO: This might require a bit more logic
            if (node.Obstructed) return false;

            if (!node.sides.Has(direction)) return true;

            return managedOffsetSides.Any(mo => mo.Offset == offset && mo.AnchorDirection == direction);
        }

        bool CanTranslate(Direction moveDirection)
        {
            // TODO: Check if all managed full fill these checks
            var source = Dungeon[CurrentCoordinates];
            var anchor = GetComponent<Anchor>();
            if (anchor == null)
            {
                Debug.LogError(PrefixLogMessage("We don't know the cube face of the platform because it lacks an anchor"));
                return false;
            }

            var target = moveDirection.Translate(CurrentCoordinates);

            if (NodeSideIsPlatformOrEmpty(source, moveDirection))
            {
                if (Dungeon.HasNodeAt(target))
                {
                    var node = Dungeon[target];

                    // We must allow the entry from our origin direction 
                    if (NodeSideIsPlatformOrEmpty(node, moveDirection.Inverse(), moveDirection.AsLookVector3D()))
                    {
                        // We need to be able to occupy our cube face in the new node 
                        if (NodeSideIsPlatformOrEmpty(node, anchor.CubeFace, moveDirection.AsLookVector3D()))
                        {
                            return true;
                        }
                        Debug.LogWarning(PrefixLogMessage($"Can't enter {target} because it already has same side stuff at {anchor.CubeFace}"));
                        return false;
                    }

                    Debug.LogWarning(PrefixLogMessage($"Refused because {target} doesn't allow entry from {moveDirection.Inverse()} or has a {moveDirection} side alreador has a {moveDirection} side blocking"));
                    return false;
                }

                // We don't allow dungeon escape
                return false;
            }

            if (Dungeon.HasNodeAt(target))
            {
                var node = Dungeon[target];

                // We must allow the entry from our origin direction 
                if (NodeSideIsPlatformOrEmpty(node, moveDirection.Inverse(), moveDirection.AsLookVector3D()))
                {
                    // We need to be able to occupy our cube face in the new node 
                    if (NodeSideIsPlatformOrEmpty(node, anchor.CubeFace, moveDirection.AsLookVector3D()))
                    {
                        return true;
                    }
                    Debug.LogWarning(PrefixLogMessage($"Can't enter {target} because it already has same side stuff at {anchor.CubeFace}"));
                    return false;
                }

                Debug.LogWarning(PrefixLogMessage($"Refused because {target} doesn't allow entry from {moveDirection.Inverse()} or has a {moveDirection} side alreador has a {moveDirection} side blocking"));
                return false;
            }

            // We don't allow us to escape the dungeon
            return false;
        }

        void SetManagedNodeCubeSides(Vector3Int coordinates, bool value)
        {
            foreach (var mo in managedOffsetSides)
            {
                var otherCoordinates = mo.Offset + coordinates;
                if (Dungeon.HasNodeAt(otherCoordinates))
                {
                    Dungeon[otherCoordinates].UpdateSide(mo.AnchorDirection, value);
                }
                else
                {
                    Debug.LogWarning(PrefixLogMessage($"Can't set {mo.AnchorDirection} of node at {otherCoordinates} to {value} because outside dungeon"));
                }
            }
        }

        bool BecomeTile(Vector3Int coordinates, bool translate = false)
        {
            var currentNode = GetComponentInParent<TDNode>();
            var anchor = GetComponent<Anchor>();
            var myFace = anchor?.CubeFace ?? Direction.Down;

            if (currentNode.Coordinates == coordinates)
            {
                Debug.LogWarning(PrefixLogMessage($"I'm already at {coordinates}"));

                if (translate)
                {
                    transform.localPosition = Vector3.zero;
                }
                return true;
            }

            foreach (var entity in constrainedEntities)
            {
                entity.Node.RemoveOccupant(entity);
            }


            // Clear my position
            currentNode.UpdateSide(myFace, false);
            SetManagedNodeCubeSides(currentNode.Coordinates, false);

            // Gain new position
            if (Dungeon.HasNodeAt(coordinates))
            {
                Debug.Log(PrefixLogMessage($"I'm becomming {coordinates}"));
                var newNode = Dungeon[coordinates];
                transform.SetParent(newNode.transform);
                newNode.UpdateSide(myFace, true);

                // We really need to also ask those to update their own parenting so they are the correct nodes
                SetManagedNodeCubeSides(coordinates, true);

                // Constrained enemies must enter new node
                foreach (var entity in constrainedEntities)
                {
                    // Since entity should be attached to us / our anchor it doesn't need to update its own node
                    newNode.AddOccupant(entity);
                    Debug.Log(PrefixLogMessage($"Managed entity now is {entity}"));
                }

            } else
            {
                Debug.LogError(PrefixLogMessage($"Could not become {coordinates} because dungeon lacks node"));
            }

            if (translate)
            {
                transform.localPosition = Vector3.zero;
            }
            return false;
        }

        bool _alignedWithGrid;
        public bool AlignedWithGrid {
            get => alwaysClaimToBeAligned ? true : _alignedWithGrid;
            private set
            {
                _alignedWithGrid = value;
            }
        }

        void InitMoveStep()
        {
            Debug.Log(PrefixLogMessage($"Init move {MoveDirection}"));

            if (!CanTranslate(MoveDirection))
            {
                Debug.Log(PrefixLogMessage($"I've reached end of my movement , can't move {MoveDirection} to {MoveDirection.Translate(CurrentCoordinates)}"));
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
