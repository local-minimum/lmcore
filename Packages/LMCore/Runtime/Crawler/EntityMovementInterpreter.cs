using UnityEngine;
using LMCore.AbstractClasses;
using LMCore.IO;
using System.Collections.Generic;

namespace LMCore.Crawler
{
    public class EntityMovementInterpreter : MonoBehaviour, IEntityMovementInterpreter
    {
        public IDungeon Dungeon { get;  set; }

        public event EntityMovementEvent OnEntityMovement;

        private LazyComponent<CrawlerInput> cInput;
        private LazyComponent<GridEntity> gEntity;

        private void Awake()
        {
            cInput = new LazyComponent<CrawlerInput>(gameObject);
            gEntity = new LazyComponent<GridEntity>(gameObject);
        }
        private void OnEnable()
        {
            cInput.Value.OnMovement += CInput_OnMovement; ;

        }

        public void OnDisable()
        {
            cInput.Value.OnMovement -= CInput_OnMovement;
        }

        private void HandleRotation(int tickId, Movement movement, Vector3Int startPosition, float duration)
        {
            var endLookDirection = gEntity.Value.LookDirection.ApplyRotation(gEntity.Value.Anchor, movement, out var endAnchor);

            OnEntityMovement?.Invoke(
                tickId,
                gEntity.Value,
                MovementOutcome.NodeInternal,
                new List<EntityState>() {
                    new EntityState(gEntity),
                    new EntityState(startPosition, endAnchor, endLookDirection, gEntity.Value)
                },
                duration);
        }

        private void HandleTranslation(int tickId, Vector3Int target, Direction startLookDirection, float duration)
        {
            OnEntityMovement?.Invoke(
                tickId,
                gEntity.Value,
                MovementOutcome.NodeExit,
                new List<EntityState>()
                {
                    new EntityState(gEntity),
                    new EntityState(target, gEntity.Value.Anchor, startLookDirection, gEntity.Value)
                },
                duration
            );
        }

        bool HandleSpecialTranslations(int tickId, Movement movement, Vector3Int candidateTarget, float duration)
        {
            // TODO: Allow letting go / walling down and catching wall further down?

            var position = gEntity.Value.Position;
            var lookDirection = gEntity.Value.LookDirection;
            var anchor = gEntity.Value.Anchor;

            if (Dungeon.HasNodeAt(position))
            {
                var direction = lookDirection.RelativeTranslation3D(anchor, movement);
                var outcome = Dungeon[position].AllowsMovement(gEntity, anchor, direction);
                if (outcome != MovementOutcome.NodeExit)
                {
                    return false;
                }
            } else
            {
                Debug.LogWarning($"We are not on the map {position}");
            }

            if (Dungeon.HasNodeAt(candidateTarget))
            {
                if (Dungeon[candidateTarget].CanAnchorOn(gEntity, anchor)) return false;
            }

            // Getting up off a wall
            if (anchor.IsPlanarCardinal())
            {                
                lookDirection = anchor;
                var target = Dungeon[candidateTarget].Neighbour(lookDirection);
                if (Dungeon.HasNodeAt(target))
                {
                    var targetNode = Dungeon[target];
                    var allowEntry = targetNode.AllowsEntryFrom(gEntity, anchor.Inverse());
                    var canAnchorOn = targetNode.CanAnchorOn(gEntity, Direction.Down);
                    if (!allowEntry || !canAnchorOn)
                    {
                        Debug.LogWarning($"Trying to go {position} -> {candidateTarget} -> {target}: Entry({allowEntry}) | Anchor floor({canAnchorOn})");
                        return false;
                    }


                    Debug.Log($"{gEntity.Value.name}: Exits wall climbing");

                    OnEntityMovement?.Invoke(
                        tickId,
                        gEntity.Value,
                        MovementOutcome.NodeInternal,
                        new List<EntityState>() { 
                            new EntityState(gEntity),
                            new EntityState(
                                candidateTarget,
                                Direction.Down,
                                lookDirection,
                                gEntity
                            ),
                            new EntityState(
                                target,
                                Direction.Down,
                                lookDirection,
                                gEntity.Value.RotationRespectsAnchorDirection,
                                gEntity.Value.TransportationMode.RemoveFlag(TransportationMode.Climbing)
                            )
                        },
                        duration
                    );
                    return true;
                }
                Debug.LogWarning($"Trying to go {position} -> {candidateTarget} -> {target} but target is not on the map.");
                return false;

            } else if (anchor == Direction.Down)
            {
                // Climbing down onto wall
                anchor = lookDirection.RelativeTranslation3D(anchor, movement).Inverse();

                if (!anchor.IsParallell(lookDirection))
                {
                    return false;
                }
                var intermediaryState = new EntityState(
                    candidateTarget,
                    Direction.Down,
                    anchor,
                    gEntity.Value.RotationRespectsAnchorDirection,
                    gEntity.Value.TransportationMode.AddFlag(TransportationMode.Climbing)
                );

                lookDirection = Direction.Up;
                var target = Direction.Down.Translate(candidateTarget);

                if (Dungeon.HasNodeAt(target))
                {
                    var targetNode = Dungeon[target];
                    var allowEntry = targetNode.AllowsEntryFrom(gEntity, Direction.Up);
                    var canAnchorOn = targetNode.CanAnchorOn(gEntity, anchor);
                    if (!allowEntry || !canAnchorOn)
                    {
                        Debug.Log(
                            $"Trying to find ladder {position} -> {candidateTarget} -> {target} facing {anchor}: Allow entry from above ({allowEntry}) or Attaching to that wall ({canAnchorOn})"
                            );
                        return false;
                    }

                } else {
                    Debug.Log($"Trying to find ladder {position} -> {candidateTarget} -> {target} facing {anchor}, but there's no node there");
                    return false; 
                }

                Debug.Log($"{gEntity.Value.name}: Enters wall climbing");

                OnEntityMovement?.Invoke(
                    tickId,
                    gEntity,
                    MovementOutcome.NodeExit,
                    new List<EntityState>()
                    {
                        new EntityState(gEntity),                        
                        intermediaryState,
                        new EntityState(
                            target,
                            anchor,
                            lookDirection,
                            gEntity.Value.RotationRespectsAnchorDirection,
                            intermediaryState.TransportationMode
                        )
                    },
                    duration
                );
                return true;
            }

            Debug.LogWarning("No known ceiling special moves");
            return false;
        }

        private void CInput_OnMovement(int tickId, Movement movement, float duration)
        {
            var mover = gEntity.Value.ActiveMover;
            if (mover?.Animating == true)
            {
                Debug.LogWarning("Movement was ongoing need to end it");
                mover.EndAnimation(true);
                if (gEntity.Value.Falling && movement != Movement.Down)
                {
                    Debug.LogWarning("Overriding movement as fall");
                    movement = Movement.Down;
                }
            }

            var startPosition = gEntity.Value.Position;
            var startLookDirection = gEntity.Value.LookDirection;
            var startAnchor = gEntity.Value.Anchor;

            Debug.Log($"{name} Position{startPosition} Anchor({startAnchor}) Looking({startLookDirection}): {movement}");

            if (movement.IsRotation())
            {
                if (!Dungeon.HasNodeAt(startPosition))
                {
                    Debug.LogWarning($"{name} is outside the map at {startPosition}");
                } else
                {
                    var node = Dungeon[startPosition];
                    if (!node.AllowsRotating(gEntity))
                    {
                        OnEntityMovement?.Invoke(
                            tickId,
                            gEntity,
                            MovementOutcome.Refused,
                            null,
                            duration
                        );
                        Debug.Log($"{node} refuses rotation {movement}");
                        return;
                    }
                }

                HandleRotation(tickId, movement, startPosition, duration);
                return;

            } else if (movement.IsTranslation())
            {
                var direction = startLookDirection.RelativeTranslation3D(startAnchor, movement);
                var target = Dungeon.HasNodeAt(startPosition) ? Dungeon[startPosition].Neighbour(direction) : direction.Translate(startPosition);

                if (!HandleSpecialTranslations(tickId, movement, target, duration))
                {
                    if (Dungeon.HasNodeAt(startPosition))
                    {
                        var startNode = Dungeon[startPosition];
                        var outcome = startNode.AllowsMovement(gEntity, startAnchor, direction);

                        if (outcome == MovementOutcome.Refused || outcome == MovementOutcome.Blocked)
                        {
                            OnEntityMovement?.Invoke(
                                tickId,
                                gEntity, 
                                outcome, 
                                new List<EntityState>() { 
                                    new EntityState(gEntity),
                                    new EntityState(target, gEntity),
                                },
                                duration
                            );
                            Debug.LogWarning($"Movement {movement} {outcome} by node");
                            return;
                        }


                        if (outcome == MovementOutcome.NodeInternal) {

                            // Re-interpret movement as a rotation on the inside of a cube
                            movement = startLookDirection.RotationMovementFromCubeInsideDirections(startAnchor, direction);

                            HandleRotation(tickId, movement, startPosition, duration);
                            return;

                        } else if (Dungeon.HasNodeAt(target))
                        {
                            var targetNode = Dungeon[target];
                            if (!targetNode.AllowsEntryFrom(gEntity, direction.Inverse()))
                            {
                                OnEntityMovement?.Invoke(
                                    tickId,
                                    gEntity,
                                    MovementOutcome.Blocked,
                                    new List<EntityState>()
                                    {
                                        new EntityState(gEntity),
                                        new EntityState(target, gEntity),
                                    },
                                    duration
                                );
                                Debug.LogWarning($"{name} attempted to enter map at {target} from {direction.Inverse()}, this should be blocked");
                                return;
                            }

                            if (targetNode.CanAnchorOn(gEntity, startAnchor))
                            {
                                HandleTranslation(tickId, target, startLookDirection, duration);
                                return;

                            }
                            
                            if (startAnchor == Direction.Down)
                            {
                                // TODO: Fix how falling should be handled better
                                var rampTarget = Direction.Down.Translate(target);
                                if (Dungeon.HasNodeAt(rampTarget)) { 
                                    var t = Dungeon[rampTarget];
                                    if (t.AllowsEntryFrom(gEntity, Direction.Up) && t.IsHighRamp)
                                    {
                                        HandleTranslation(tickId, rampTarget, startLookDirection, duration);
                                        return;
                                    }
                                }
                                HandleTranslation(tickId, target, startLookDirection, duration);
                                return;
                            }

                            Debug.LogWarning($"Blocked move {movement}, because could not anchor on {startAnchor}");
                            OnEntityMovement?.Invoke(
                                tickId,
                                gEntity,
                                MovementOutcome.Blocked,
                                new List<EntityState>()
                                {
                                    new EntityState(gEntity),
                                    new EntityState(target, gEntity),
                                },
                                duration
                            );
                            return;
                        }

                        HandleTranslation(tickId, target, startLookDirection, duration);
                        return;
                    } 

                    HandleTranslation(tickId, target, startLookDirection, duration);
                    return;

                }
            }
        }
    }
}
