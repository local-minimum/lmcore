using LMCore.AbstractClasses;
using LMCore.IO;
using LMCore.Juice;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    public class InstantMovementNodes : MonoBehaviour, IEntityMover
    {
        public IGridSizeProvider GridSizeProvider { get; set; }
        public IDungeon Dungeon { get; set; }

        public bool Enabled => enabled && gameObject.activeSelf;

        public event EntityMovementStartEvent OnMoveStart;
        public event EntityMovementEndEvent OnMoveEnd;

        [SerializeField]
        NodeShaker RefusedMovementShaker;

        private LazyComponent<CrawlerInput> cInput;
        private LazyComponent<GridEntity> gEntity;
        private LazyComponent<GridEntityController> gController;

        private void Awake()
        {
            cInput = new LazyComponent<CrawlerInput>(gameObject);
            gEntity = new LazyComponent<GridEntity>(gameObject);
            gController = new LazyComponent<GridEntityController>(gameObject);

            GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
            enabled = GameSettings.InstantMovement.Value;
            if (enabled)
            {
                gEntity.Value.Sync();
            }
        }

        private void OnDestroy()
        {
            GameSettings.InstantMovement.OnChange -= InstantMovement_OnChange;
        }

        private void InstantMovement_OnChange(bool value)
        {
            enabled = value;
        }

        private void OnEnable()
        {
            cInput.Value.OnMovement += CInput_OnMovement;
            gEntity.Value.OnLand.AddListener(OnLand);

            Movers.Activate(this);
        }

        public void OnDisable()
        {
            cInput.Value.OnMovement -= CInput_OnMovement;
            gEntity.Value.OnLand.RemoveListener(OnLand);

            Movers.Deactivate(this);
        }

        private void HandleRotation(Movement movement, Vector3Int startPosition)
        {
            var endLookDirection = gEntity.Value.LookDirection.ApplyRotation(gEntity.Value.Anchor, movement, out var endAnchor);

            OnMoveStart?.Invoke(
                gEntity,
                movement,
                startPosition,
                endLookDirection,
                endAnchor,
                true
            );

            Debug.Log($"{gEntity.Value.name}: Rotating({movement}) to Looking({endLookDirection}) Anchor({endAnchor})");
            gEntity.Value.Rotate(movement);

            OnMoveEnd?.Invoke(
                gEntity,
                true
            );
        }

        private void HandleTranslation(Movement movement, Vector3Int target, Direction startLookDirection)
        {
            // Simple move 
            OnMoveStart?.Invoke(
                gEntity,
                movement,
                target,
                startLookDirection,
                gEntity.Value.Anchor,
                true
            );

            Debug.Log($"{gEntity.Value.name}: Translating({movement}) to {target} Anchor({gEntity.Value.Anchor}) Looking({startLookDirection})");
            gEntity.Value.Translate(movement);

            OnMoveEnd?.Invoke(
                gEntity,
                true
            );

        }

        bool HandleSpecialTranslations(Movement movement, Vector3Int intermediary)
        {
            // TODO: Allow letting go / walling down and catching wall further down?

            if (!movement.IsTranslation()) return false;

            var position = gEntity.Value.Position;
            var lookDirection = gEntity.Value.LookDirection;
            var anchor = gEntity.Value.Anchor;
            var target = intermediary;
            List<Movement> rotationMovements = new List<Movement>();

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

            if (Dungeon.HasNodeAt(intermediary))
            {
                if (Dungeon[intermediary].CanAnchorOn(gEntity, anchor)) return false;
            }

            // Getting off a wall
            if (anchor.IsPlanarCardinal())
            {                
                target = anchor.Translate(intermediary);
                if (Dungeon.HasNodeAt(target))
                {
                    var targetNode = Dungeon[target];
                    var allowEntry = targetNode.AllowsEntryFrom(gEntity, anchor.Inverse());
                    var canAnchorOn = targetNode.CanAnchorOn(gEntity, Direction.Down);
                    if (!allowEntry || !canAnchorOn)
                    {
                        Debug.LogWarning($"Trying to go {position} -> {intermediary} -> {target}: Allow entry from above ({allowEntry}) or Attaching to that wall ({canAnchorOn})");
                        return false;
                    }

                    lookDirection = anchor;
                    anchor = Direction.Down;
                    rotationMovements.Add(Movement.PitchDown);
                } else
                {
                    Debug.LogWarning($"Trying to go {position} -> {intermediary} -> {target} but target is not on the map.");
                    return false;
                }
            } else if (anchor == Direction.Down)
            {
                // Climbing down onto wall
                anchor = lookDirection.RelativeTranslation3D(anchor, movement).Inverse();
                if (anchor == lookDirection.Inverse())
                {
                    rotationMovements.Add(Movement.YawCW);
                    rotationMovements.Add(Movement.YawCW);
                } else if (!anchor.IsParallell(lookDirection))
                {
                    return false;
                }

                lookDirection = Direction.Up;
                target = Direction.Down.Translate(intermediary);

                if (Dungeon.HasNodeAt(target))
                {
                    var targetNode = Dungeon[target];
                    var allowEntry = targetNode.AllowsEntryFrom(gEntity, Direction.Up);
                    var canAnchorOn = targetNode.CanAnchorOn(gEntity, anchor);
                    if (!allowEntry || !canAnchorOn)
                    {
                        Debug.LogWarning(
                            $"Trying to find ladder {position} -> {intermediary} -> {target} facing {anchor}: Allow entry from above ({allowEntry}) or Attaching to that wall ({canAnchorOn})"
                            );
                        return false;
                    }

                } else {
                    Debug.LogWarning($"Trying to find ladder {position} -> {intermediary} -> {target} facing {anchor}, but there's no node there");
                    return false; 
                }
            } else
            {
                Debug.LogWarning("No known ceiling special moves");
                return false;
            }

            OnMoveStart?.Invoke(
                gEntity,
                movement,
                target,
                lookDirection,
                anchor,
                true
            );

            Debug.Log($"{gEntity.Value.name} invoking special move: Translate({movement}) with Rotations({string.Join("/", rotationMovements)}) to {target} Looking({lookDirection}) Anchor({anchor})");
            gEntity.Value.Position = target;
            gEntity.Value.Anchor = anchor;
            gEntity.Value.LookDirection = lookDirection;

            OnMoveEnd?.Invoke(
                gEntity,
                true
            );

            return true;
        }


        private void CInput_OnMovement(int tickId, Movement movement, float duration)
        {
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
                        RefusedMovementShaker?.Shake();
                        Debug.Log($"{node} refuses rotation {movement}");
                        return;
                    }
                }

                HandleRotation(movement, startPosition);

            } else if (movement.IsTranslation())
            {
                var direction = startLookDirection.RelativeTranslation3D(startAnchor, movement);
                var target = direction.Translate(startPosition);

                if (!HandleSpecialTranslations(movement, target))
                {
                    if (Dungeon.HasNodeAt(startPosition))
                    {
                        var startNode = Dungeon[startPosition];
                        var outcome = startNode.AllowsMovement(gEntity, startAnchor, direction);

                        if (outcome == MovementOutcome.Refused || outcome == MovementOutcome.Blocked)
                        {
                            RefusedMovementShaker?.Shake();
                            Debug.LogWarning($"Movement {movement} {outcome} by node");
                            return;
                        }

                        if (outcome == MovementOutcome.NodeInternal) {

                            // Re-interpret movement as a rotation on the inside of a cube
                            movement = startLookDirection.RotationMovementFromCubeInsideDirections(startAnchor, direction);

                            HandleRotation(movement, startPosition);
                        } else if (Dungeon.HasNodeAt(target))
                        {
                            var targetNode = Dungeon[target];
                            if (!targetNode.AllowsEntryFrom(gEntity, direction.Inverse()))
                            {
                                RefusedMovementShaker?.Shake();
                                Debug.LogWarning($"{name} attempted to enter map at {target} from {direction.Inverse()}, this should be blocked");
                                return;
                            }

                            if (targetNode.CanAnchorOn(gEntity, startAnchor))
                            {
                                HandleTranslation(movement, target, startLookDirection);
                            } else if (startAnchor == Direction.Down)
                            {
                                // TODO: Fix how falling should be handled better
                                HandleTranslation(movement, target, startLookDirection);
                            } else
                            {
                                RefusedMovementShaker?.Shake();
                                Debug.LogWarning($"Move {movement}, because could not anchor on {startAnchor}");
                                return;
                            }
                        } else
                        {
                            HandleTranslation(movement, target, startLookDirection);
                        }
                    } else {
                        HandleTranslation(movement, target, startLookDirection);
                    }

                }
            }

            Debug.Log("Syncing position");
            gEntity.Value.Sync();
        }

        public void OnLand()
        {
            RefusedMovementShaker?.Shake();
        }
    }
}
