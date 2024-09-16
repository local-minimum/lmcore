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
        GridEntity Entity {  
            get {
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
            float duration
            )
        {
            if (entity != Entity)
            {
                return;
            }

            interpretation.Evaluate(Entity);

            Debug.Log(PrefixLogMessage($"Perform {interpretation}"));

            if (interpretation.Outcome == MovementInterpretationOutcome.Bouncing || 
                interpretation.Outcome == MovementInterpretationOutcome.DynamicBounce)
            {
                RefusedMovementShaker?.Shake();
            }

            Entity.Sync(interpretation.Last.Checkpoint);
            Entity.Moving = MovementType.Stationary;

            var positionConstraint = GetComponent<PositionConstraint>();
            if (positionConstraint != null && positionConstraint.constraintActive)
            {
                positionConstraint.weight = 1;
            }

        }
    }
}
