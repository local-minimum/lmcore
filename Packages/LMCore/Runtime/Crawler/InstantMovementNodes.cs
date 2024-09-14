using LMCore.IO;
using LMCore.Juice;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

namespace LMCore.Crawler
{
    public class InstantMovementNodes : MonoBehaviour, IEntityMover
    {
        public IGridSizeProvider GridSizeProvider { get; set; }
        public IDungeon Dungeon { get; set; }

        public bool Enabled => enabled && gameObject.activeSelf;

        public bool Animating => false;

        public event EntityMovementStartEvent OnMoveStart;
        public event EntityMovementEndEvent OnMoveEnd;

        [SerializeField]
        NodeShaker RefusedMovementShaker;

        private GridEntity gEntity;
        public GridEntity Entity => gEntity;

        private IEntityMovementInterpreter movementInterpreter;

        private void Awake()
        {
            gEntity = GetComponent<GridEntity>();
            movementInterpreter = GetComponent<IEntityMovementInterpreter>();

            GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
            enabled = GameSettings.InstantMovement.Value;
            if (enabled)
            {
                gEntity.Sync();
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
            gEntity.OnLand.AddListener(OnLand);
            movementInterpreter.OnEntityMovement += MovementInterpreter_OnEntityMovement;

            Movers.Activate(this);
        }

        public void OnDisable()
        {
            gEntity.OnLand.RemoveListener(OnLand);
            movementInterpreter.OnEntityMovement -= MovementInterpreter_OnEntityMovement;

            Movers.Deactivate(this);
        }

        private void MovementInterpreter_OnEntityMovement(int tickId, GridEntity entity, MovementOutcome outcome, List<EntityState> states, float duration)
        {
            if (entity != gEntity) return;

            if (states == null || states.Count == 0 || outcome == MovementOutcome.Refused || outcome == MovementOutcome.Blocked)
            {
                RefusedMovementShaker?.Shake();
                return;
            }

            var target = states.LastOrDefault();

            OnMoveStart?.Invoke(
                entity,
                states.Select(s => s.Coordinates).ToList(),
                states.Select(s => s.Anchor).ToList()
            );

            if (outcome == MovementOutcome.NodeExit)
            {
                Dungeon[entity.Coordinates]?.RemoveOccupant(entity);
            }

            entity.Coordinates = target.Coordinates;
            entity.AnchorDirection = target.Anchor;
            entity.LookDirection = target.LookDirection;
            entity.RotationRespectsAnchorDirection = target.RotationRespectsAnchorDirection;
            entity.TransportationMode = target.TransportationMode;

            var constraint = GetComponent<PositionConstraint>();
            if (constraint != null && constraint.constraintActive)
            {
                constraint.weight = 1;
            }

            entity.Sync();

            if (outcome == MovementOutcome.NodeExit) { 
                Dungeon[entity.Coordinates]?.AddOccupant(entity);
            }

            OnMoveEnd?.Invoke(
                entity,
                true
            );
        }

        public void OnLand()
        {
            RefusedMovementShaker?.Shake();
        }

        public void EndAnimation(bool emitEndEvent = true)
        {
            // Do nothing since where never animating
        }
    }
}
