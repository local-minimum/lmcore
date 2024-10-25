using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.SaveLoad;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    /// <summary>
    /// Dynamically added class to cause faces to move.
    /// 
    /// They can be Automatic or Managed by toggle groups.
    /// 
    /// See Configure function for what settings it respects
    /// and documentation on the settings
    /// </summary>
    public class TDMovingPlatform : TDFeature, IMovingCubeFace, IOnLoadSave
    {
        [Serializable]
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
        bool alwaysClaimToBeAligned;

        [SerializeField, HideInInspector]
        Direction MoveDirection = Direction.None;
        Direction OriginalDirection;

        /// <summary>
        /// What happens when the platform can no longer continue in the move direction.
        /// 
        /// None leaves it at the end station.
        /// Bounce inverts the direction.
        /// Wrap makes it respawn at start.
        /// </summary>
        [SerializeField, HideInInspector]
        TDEnumLoop Loop = TDEnumLoop.None;

        /// <summary>
        /// How the activity of the platform is controlled
        /// 
        /// Automatic makes it start when the game starts
        /// Managed requires toggle group action to start it
        /// </summary>
        [SerializeField, HideInInspector]
        TDEnumInteraction Interaction = TDEnumInteraction.Automatic;

        [SerializeField, HideInInspector]
        int managedByGroup = -1;

        /// <summary>
        /// What the effect is when the toggle is toggled.
        /// 
        /// None disables toggle group effects
        /// Bounce inverts the directions
        /// Wrap returns it to the original position
        /// </summary>
        [SerializeField, HideInInspector]
        TDEnumLoop managedToggleEffect = TDEnumLoop.None;

        [SerializeField, HideInInspector]
        string _identifier;
        public string Identifier => _identifier;

        /// <summary>
        /// World position of a virtual node center misaligned with the dungeon grid
        /// </summary>
        public Vector3 VirtualNodeCenter => 
            transform.position + (Dungeon?.GridSize ?? 3f) * 0.5f * Vector3.up;

        protected string PrefixLogMessage(string message) => $"{Interaction} Moving Platform {Coordinates} (origin {StartCoordinates}): {message}";

        public override string ToString() => PrefixLogMessage(
            $"Phase({phase}) ByGroup({managedByGroup}) AlwaysAlign({alwaysClaimToBeAligned}) Offsets({string.Join(", ", managedOffsetSides.Select(mo => $"{mo.Offset} {mo.AnchorDirection}"))})");

        [ContextMenu("Info")]
        public void Info() => Debug.Log(this);

        public void Configure(TDNodeConfig conf)
        {
            var tConf = TiledConfiguration.InstanceOrCreate();

            var platform = conf.FirstObjectProps(obj => obj.Type == tConf.MovingPlatformClass);
            if (platform == null)
            {
                Debug.LogWarning(PrefixLogMessage("Could not find any configuration"));
                return;
            }


            MoveDirection = platform.Direction(tConf.DirectionKey, TDEnumDirection.None).AsDirection();
            Loop = platform.Loop(tConf.LoopKey);
            Interaction = platform.Interaction(tConf.InteractionKey, TDEnumInteraction.Automatic);
            moveSpeed = platform.Float(tConf.VelocityKey, moveSpeed);
            loopDelay = platform.Float(tConf.PauseKey, loopDelay);
            alwaysClaimToBeAligned = platform.Bool(tConf.ClaimAlwaysAlignedKey, true);
            managedByGroup = platform.Int(tConf.ObjManagedByGroupKey, -1);
            managedToggleEffect = platform.Loop(tConf.ObjToggleEffectKey, TDEnumLoop.None);
            _identifier = platform.String(tConf.ObjIdKey, "");
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
            var offset = coordinates - Coordinates;
            if (offset == Vector3Int.zero && anchor == Direction.Down) return true;

            return managedOffsetSides.Any(mo => mo.Offset == offset && mo.AnchorDirection == anchor);
        }

        ConstraintSource constraintSource => new ConstraintSource() { sourceTransform = transform, weight = 1 };
        public void AddAttachedObject(Transform attached, Direction cubeSide)
        {
            var translationOffset = attached.position - transform.position;
            var constraint = attached.gameObject.AddComponent<PositionConstraint>();
            constraint.AddSource(constraintSource);
            constraint.translationAtRest = translationOffset;
            constraint.translationOffset = translationOffset;
            constraint.constraintActive = true;

            if (cubeSide == Direction.None) return;

            var otherNode = attached.GetComponentInParent<TDNode>();
            var coordinatesOffset = otherNode.Coordinates - Coordinates;

            Debug.Log(PrefixLogMessage($"Is coordinating transform with offset {coordinatesOffset} cube face {cubeSide}"));
            managedOffsetSides.Add(new ManagedOffset()
            {
                Offset = coordinatesOffset,
                AnchorDirection = cubeSide,
                Transform = attached,
            });
        }

        public bool MayEnter(GridEntity entity) {
            if (entity.TransportationMode.HasFlag(TransportationMode.Flying)) return true;

            if (entity.AnchorDirection == Direction.Down) {
                var myCoordinates = Coordinates;
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
            }

            constraint.translationAtRest = entity.transform.position - newConstrainer.position;
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

        float phaseStart;
        float nextPhase;

        private void Start()
        {

            if (phase == Phase.Initial && Interaction == TDEnumInteraction.Automatic)
            {
                Debug.Log(PrefixLogMessage($"Starting platform Interaction({Interaction}) Loop({Loop}) MoveDirection({MoveDirection})"));
                InitWaitToStart();
            } else if (Interaction == TDEnumInteraction.Managed && managedToggleEffect == TDEnumLoop.Bounce)
            {
                MoveDirection = MoveDirection.Inverse();
            }

            var anchor = Anchor;
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

        bool isToggled = false;

        private void OnToggleGroupToggle()
        {
            isToggled = !isToggled;
            Debug.Log(PrefixLogMessage($"Toggling the platform to {isToggled}"));
            if (managedToggleEffect == TDEnumLoop.Bounce)
            {
                MoveDirection = MoveDirection.Inverse();
                Debug.Log(PrefixLogMessage($"Invoking platform action, platform going {MoveDirection} / {isToggled}"));

                if (moveProgress > 0f && moveProgress < 1f)
                {
                    moveProgress = 1f - moveProgress;
                }
                if (isToggled)
                {
                    InitMoveStep();
                } else
                {
                    InitWaitToStart();
                }
            }
            else if (managedToggleEffect == TDEnumLoop.Wrap) { 
            
                if (!BecomeTile(StartCoordinates, true))
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
            return ConstrainingTransform(entityAnchorTransform, entity.Coordinates - Coordinates, entity.AnchorDirection);
        }

        private Transform ConstrainingTransform(Transform entityAnchorTransform, Vector3Int offset, Direction anchor)
        {
            if (transform == entityAnchorTransform || offset == Vector3Int.zero && anchor == Direction.Down)
            {
                return transform;
            }

            foreach (var mo in managedOffsetSides)
            {
                if (mo.Transform == entityAnchorTransform || anchor == mo.AnchorDirection && offset == mo.Offset)
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
            phaseStart = Time.timeSinceLevelLoad;
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
            Debug.Log(PrefixLogMessage($"Init move end wait at {Coordinates}"));
            phase = Phase.WaitingEnd;
            phaseStart = Time.timeSinceLevelLoad;
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
                        if (BecomeTile(StartCoordinates, true))
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
            var source = Dungeon[Coordinates];
            var anchor = Anchor;
            if (anchor == null)
            {
                Debug.LogError(PrefixLogMessage("We don't know the cube face of the platform because it lacks an anchor"));
                return false;
            }

            var target = moveDirection.Translate(Coordinates);

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
                        Debug.Log(PrefixLogMessage($"Can't enter {target} because it already has same side stuff at {anchor.CubeFace}"));
                        return false;
                    }

                    Debug.Log(PrefixLogMessage($"Refused because {target} doesn't allow entry from {moveDirection.Inverse()} or has a {moveDirection} side alreador has a {moveDirection} side blocking"));
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
                    Debug.Log(PrefixLogMessage($"Can't enter {target} because it already has same side stuff at {anchor.CubeFace}"));
                    return false;
                }

                Debug.Log(PrefixLogMessage($"Refused because {target} doesn't allow entry from {moveDirection.Inverse()} or has a {moveDirection} side alreador has a {moveDirection} side blocking"));
                return false;
            }

            // We don't allow us to escape the dungeon
            return false;
        }

        void SetManagedNodeCubeSides(Vector3Int coordinates, bool value)
        {
            foreach (var mo in managedOffsetSides)
            {
                var otherAnchor = mo.Transform.GetComponent<Anchor>();
                var otherCoordinates = mo.Offset + coordinates;
                if (Dungeon.HasNodeAt(otherCoordinates))
                {
                    var otherNode = Dungeon[otherCoordinates];
                    otherNode.UpdateSide(mo.AnchorDirection, value);
                    if (value && otherAnchor != null)
                    {
                        mo.Transform.SetParent(otherNode.transform);
                    }
                }
                else
                {
                    Debug.LogWarning(PrefixLogMessage($"Can't set {mo.AnchorDirection} of node at {otherCoordinates} to {value} because outside dungeon"));
                }
            }
        }

        bool BecomeTile(Vector3Int coordinates, bool translate = false)
        {
            var prevNode = Node;
            var anchor = Anchor;
            var myFace = (anchor != null) ? anchor.CubeFace : Direction.Down;

            if (prevNode.Coordinates == coordinates)
            {
                Debug.LogWarning(PrefixLogMessage($"I'm already at {coordinates}"));

                if (translate)
                {
                    transform.localPosition = Vector3.zero;
                }
                return true;
            }

            Dictionary<GridEntity, Vector3Int> entityOffsets = new Dictionary<GridEntity, Vector3Int>();

            foreach (var entity in constrainedEntities)
            {
                entityOffsets[entity] = entity.Coordinates - prevNode.Coordinates;
                //entity.Node.RemoveOccupant(entity);
            }

            // Clear my position
            prevNode.UpdateSide(myFace, false);
            SetManagedNodeCubeSides(prevNode.Coordinates, false);

            // Gain new position
            if (Dungeon.HasNodeAt(coordinates))
            {
                Debug.Log(PrefixLogMessage($"I'm becomming {coordinates}"));
                var newNode = Dungeon[coordinates];
                transform.SetParent(newNode.transform);
                newNode.UpdateSide(myFace, true);

                // We really need to also ask those to update their own parenting so they are the correct nodes
                SetManagedNodeCubeSides(coordinates, true);
            } else
            {
                Debug.LogError(PrefixLogMessage($"Could not become {coordinates} because dungeon lacks node"));
            }

            // Constrained enemies must enter new nodes
            foreach (var entity in constrainedEntities)
            {
                var newEntityCoordinates = coordinates += entityOffsets[entity];
                if (Dungeon.HasNodeAt(newEntityCoordinates))
                {
                    Debug.LogWarning(PrefixLogMessage($"Managed entity {entity.name} is becoming {newEntityCoordinates}"));
                    var newEntityNode = Dungeon[newEntityCoordinates];
                    entity.transform.SetParent(newEntityNode.transform);
                } else
                {
                    Debug.LogError(PrefixLogMessage($"Can't place {entity} at {newEntityCoordinates} because outside dungeon"));
                }
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

        public int OnLoadPriority => 10;

        float moveProgress;
        void InitMoveStep()
        {
            bool resume =  moveProgress != 0 && moveProgress != 1;

            var relay = Node.GetComponent<TDRelay>();
            if (!resume && relay != null && relay.Relays(MoveDirection.Inverse(), out var newDirection))
            {
                Debug.Log(PrefixLogMessage($"Relay caused us to change direction from {MoveDirection} to {newDirection} with rest {relay.Rest}s"));
                MoveDirection = newDirection;
                if (relay.Rest > 0f)
                {
                    phaseStart = relay.Rest + Time.timeSinceLevelLoad;
                    phase = Phase.Moving;
                    resume = true;
                }
            }

            if (!resume && !CanTranslate(MoveDirection))
            {
                Debug.Log(PrefixLogMessage($"I've reached end of my movement, can't move {MoveDirection} to {MoveDirection.Translate(Coordinates)}"));
                InitWaitEnd();
                return;
            }

            var becomeTileThreshold = 0.5f;
            if (MoveDirection == Anchor.CubeFace)
            {
                becomeTileThreshold = 0.1f;
            }
            else if (MoveDirection.Inverse() == Anchor.CubeFace)
            {
                becomeTileThreshold = 0.9f;
            }

            var startCoordinates = Coordinates;
            if (resume)
            {
                // The movement start / reference coordinates will have been the inverse direction of 
                // our current movement if we already passed the threshold of moving...
                phaseStart = Time.timeSinceLevelLoad - moveProgress * moveSpeed;
                if (moveProgress > becomeTileThreshold)
                {
                    startCoordinates = MoveDirection.Inverse().Translate(Coordinates);
                }
            } else
            {
                phaseStart = Time.timeSinceLevelLoad;
                moveProgress = 0f;
            }
            phase = Phase.Moving;
            var startPosition = startCoordinates.ToPosition(Dungeon.GridSize);
            var targetCoordinates = MoveDirection.Translate(startCoordinates);
            var targetPosition = targetCoordinates.ToPosition(Dungeon.GridSize);

            ActivePhaseFunction = () =>
            {
                moveProgress = Mathf.Clamp01((Time.timeSinceLevelLoad - phaseStart) / moveSpeed);

                AlignedWithGrid = moveProgress < 0.1f || moveProgress > 0.9f;

                if (moveProgress > becomeTileThreshold && Coordinates == startCoordinates)
                {
                    BecomeTile(targetCoordinates);
                }

                transform.position = Vector3.Lerp(startPosition, targetPosition, moveProgress);

                if (moveProgress == 1)
                {
                    ActivePhaseFunction = InitMoveStep;
                }
            };
        }

        private void Update()
        {
            ActivePhaseFunction?.Invoke();
        }

        public KeyValuePair<Vector3Int, MovingPlatformSave> Save() =>
            new KeyValuePair<Vector3Int, MovingPlatformSave>(
                StartCoordinates,
                new MovingPlatformSave()
                {
                    currentCoordinates = Coordinates,
                    moveDirection = MoveDirection,
                    phaseStartDelta = Time.timeSinceLevelLoad - phaseStart,
                    nextPhaseDelay = nextPhase - Time.timeSinceLevelLoad,
                    isToggled = isToggled,
                    phase = phase,
                    alignedWithGrid = _alignedWithGrid,
                    constrainedEntitites = constrainedEntities.Select(e => 
                        new ConstrainedEntitySave() {
                            Identifier = e.Identifier, 
                            Offset = e.Coordinates - Coordinates,
                            Anchor = e.AnchorDirection,
                        }).ToList(),
                });

        private void OnLoadGameSave(GameSave save)
        {
            if (save == null) return;

            var lvl = GetComponentInParent<IDungeon>().MapName;
            var platformSave = save.levels[lvl]?.movingPlatforms.GetValueOrDefault(StartCoordinates);

            if (platformSave == null)
            {
                Debug.LogError(PrefixLogMessage("I have no saved state"));
                return; 
            }

            BecomeTile(platformSave.currentCoordinates, true);
            MoveDirection = platformSave.moveDirection;
            phaseStart = Time.timeSinceLevelLoad - platformSave.phaseStartDelta;
            nextPhase = platformSave.nextPhaseDelay + Time.timeSinceLevelLoad;
            isToggled = platformSave.isToggled;
            phase = platformSave.phase;
            _alignedWithGrid = platformSave.alignedWithGrid;

            constrainedEntities.Clear();
            foreach (var constraintSave in platformSave.constrainedEntitites)
            {
                var entity = Dungeon.GetEntity(constraintSave.Identifier);
                if (entity == null)
                {
                    Debug.LogError(PrefixLogMessage($"Could not locate/constrain {constraintSave.Identifier}"));
                    continue;
                }

                Transform newConstrainer = ConstrainingTransform(null, constraintSave.Offset, constraintSave.Anchor);

                if (newConstrainer != null)
                {
                    var constraint = AddConstraint(entity, entity.GetComponent<PositionConstraint>(), newConstrainer);
                    constraint.weight = 1f;
                    constraint.constraintActive = constraint.sourceCount > 0;

                    constrainedEntities.Add(entity);
                }
            }

            switch (phase)
            {
                case Phase.Initial:
                    ActivePhaseFunction = null;
                    if (Interaction == TDEnumInteraction.Automatic)
                    {
                        InitWaitToStart();
                    }
                    break;
                case Phase.WaitingStart:
                    ActivePhaseFunction = HandleWaitToStart;
                    break;
                case Phase.WaitingEnd:
                    ActivePhaseFunction = HandleWaitToEnd;
                    break;
                case Phase.Moving:
                    InitMoveStep();
                    break;
                case Phase.Ended:
                    ActivePhaseFunction = null;
                    break;
            }

            ActivePhaseFunction?.Invoke();
            Debug.Log(PrefixLogMessage($"Platform loaded into phase {phase} / {platformSave.phase}"));
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
    }
}
