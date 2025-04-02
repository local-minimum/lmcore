using LMCore.IO;
using LMCore.Juice;
using UnityEngine;
using UnityEngine.Animations;

namespace LMCore.Crawler
{
    public class InstantMovementTransitions : MonoBehaviour
    {
        [SerializeField]
        NodeShaker RefusedMovementShaker;

        protected string PrefixLogMessage(string message) =>
            $"Instant Movement: {message}";

        GridEntity _entity;
        GridEntity Entity
        {
            get
            {
                if (_entity == null)
                {
                    _entity = GetComponent<GridEntity>();
                }
                return _entity;
            }
        }

        private void Awake()
        {
            GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
            enabled = GameSettings.InstantMovement.Value;
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
            var interpreter = GetComponent<MovementInterpreter>();
            interpreter.OnMovement += Interpreter_OnMovement;
        }

        private void OnDisable()
        {
            var interpreter = GetComponent<MovementInterpreter>();
            interpreter.OnMovement -= Interpreter_OnMovement;
        }
        private void Interpreter_OnMovement(
            GridEntity entity,
            MovementInterpretation interpretation,
            int tickId,
            float duration,
            bool forced,
            AnimationCurve easing
            )
        {
            if (entity != Entity)
            {
                return;
            }

            interpretation.Evaluate(Entity);
            entity.Moving = interpretation.Movement;

            Debug.Log(PrefixLogMessage($"Perform {interpretation}"));

            if (interpretation.Outcome == MovementInterpretationOutcome.Bouncing ||
                interpretation.Outcome == MovementInterpretationOutcome.DynamicBounce)
            {
                RefusedMovementShaker?.Shake();
            }

            var checkpoint = interpretation.Last.Checkpoint;
            bool changedAnchor = Entity.NodeAnchor != checkpoint.Anchor;

            if (Entity.NodeAnchor != null && changedAnchor)
            {
                foreach (var effect in Entity.NodeAnchor.GetComponentsInChildren<IAnchorEffect>())
                {
                    effect.ExitTile(Entity);
                }
            }

            Entity.Sync(checkpoint, interpretation.PrimaryDirection, forced);

            if (Entity.NodeAnchor != null && changedAnchor)
            {
                foreach (var effect in Entity.NodeAnchor.GetComponentsInChildren<IAnchorEffect>())
                {
                    effect.EnterTile(Entity);
                }
            }

            var positionConstraint = GetComponent<PositionConstraint>();
            if (positionConstraint != null && positionConstraint.constraintActive)
            {
                positionConstraint.weight = 1;
            }

            Entity.Moving = MovementType.Stationary;
        }
    }
}
