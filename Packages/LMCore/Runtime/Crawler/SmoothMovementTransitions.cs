using LMCore.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace LMCore.Crawler
{
    public class SmoothMovementTransitions : MonoBehaviour
    {
        protected string PrefixLogMessage(string message) =>
            $"Smooth Movement: {message}";

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
            enabled = !GameSettings.InstantMovement.Value;
        }

        private void OnDestroy()
        {
            GameSettings.InstantMovement.OnChange -= InstantMovement_OnChange;
        }

        private void InstantMovement_OnChange(bool value)
        {
            enabled = !value;
        }
        private void OnEnable()
        {
            var interpreter = GetComponent<MovementInterpreter>();
            interpreter.OnMovement += Interpreter_OnMovement;

            ElasticGameClock.OnTickEnd += ElasticGameClock_OnTickEnd;
            ElasticGameClock.OnTickEndAdjustment += ElasticGameClock_OnTickEndAdjustment;
        }

        private void OnDisable()
        {
            var interpreter = GetComponent<MovementInterpreter>();
            interpreter.OnMovement -= Interpreter_OnMovement;

            ElasticGameClock.OnTickEnd -= ElasticGameClock_OnTickEnd;
            ElasticGameClock.OnTickEndAdjustment -= ElasticGameClock_OnTickEndAdjustment;
        }

        private void ElasticGameClock_OnTickEndAdjustment(int tickId, float unadjustedProgress, float adjustedProgress, float endTime)
        {
            if (animationTickId != tickId || activeInterpretation == null) return;

            var turnCompensatedUnadjustedProgress = Mathf.Clamp01(unadjustedProgress / activeInterpretation.DurationScale);

            animationStartTime = Time.timeSinceLevelLoad;

            var remainder = endTime - animationStartTime;
            if (turnCompensatedUnadjustedProgress < 1)
            {

                animationDuration = remainder * activeInterpretation.DurationScale;
            }
            else
            {
                animationDuration = ANIMATION_EPSILON_DURATION;
            }
            animationInterpolationStart = turnCompensatedUnadjustedProgress;
        }

        private void ElasticGameClock_OnTickEnd(int tickId)
        {
            if (animationTickId == tickId)
            {
                FinalizeInterpretation();
            }
        }

        MovementInterpretation activeInterpretation;

        public bool Animating => activeInterpretation != null;

        private int animationTickId;
        /// <summary>
        /// The reference time for the start of the animation
        /// </summary>
        private float animationStartTime;
        /// <summary>
        /// Progress of the animation at the start time
        /// </summary>
        private float animationInterpolationStart;
        private float animationDuration;

        private const float ANIMATION_EPSILON_DURATION = 0.0000001f;
        float AnimationProgress =>
            animationInterpolationStart + (1 - animationInterpolationStart) * Mathf.Clamp01((Time.timeSinceLevelLoad - animationStartTime) / animationDuration);

        private List<IDungeonNode> ReservationNodes = new List<IDungeonNode>();

        private void FinalizeInterpretation()
        {
            if (activeInterpretation != null)
            {
                Entity.Sync(activeInterpretation.Last.Checkpoint);
                animationTickId = -1;
                activeInterpretation = null;
                currentCheckpoint = null;
                Entity.Moving = MovementType.Stationary;

                var positionConstraint = GetComponent<PositionConstraint>();
                if (positionConstraint != null && positionConstraint.constraintActive)
                {
                    positionConstraint.weight = 1;
                    positionConstraint.enabled = true;
                }

                ReservationNodes.Clear();
            }
        }

        MovementCheckpoint currentCheckpoint;

        bool startConstrained;

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

            FinalizeInterpretation();

            activeInterpretation = interpretation;
            animationTickId = tickId;
            animationStartTime = Time.timeSinceLevelLoad;
            animationDuration = Mathf.Max(duration, ANIMATION_EPSILON_DURATION);
            animationInterpolationStart = 0;
            Entity.Moving = interpretation.Movement;

            currentCheckpoint = activeInterpretation.First.Checkpoint;

            for (int i = 1, l = interpretation.Steps.Count; i < l; i++)
            {
                var node = interpretation.Steps[i].Checkpoint.Node;
                if (node != null && currentCheckpoint.Node != node)
                {
                    ReservationNodes.Add(node);
                    node.Reserve(entity);
                }
            }

            var positionConstraint = GetComponent<PositionConstraint>();
            if (positionConstraint != null) { 
                positionConstraint.enabled = !activeInterpretation.Movement.HasFlag(MovementType.Translating);
            }
            Debug.Log(PrefixLogMessage($"Perform {activeInterpretation}"));
            Debug.Log(PrefixLogMessage($"Segment relative lengths: {string.Join(", ", activeInterpretation.RelativeSegmentLengths(Entity.Dungeon))}"));
        }

        private void Update()
        {
            if (!Animating) { return; }

            var progress = AnimationProgress;

            if (progress == 1)
            {
                FinalizeInterpretation();
                return;
            }

            transform.position = activeInterpretation.Evaluate(
                Entity, 
                Mathf.Clamp01(progress / activeInterpretation.DurationScale), 
                out var rotation, 
                out var checkpoint,
                out var stepProgress);

            if (ReservationNodes.Count > 0)
            {
                if (activeInterpretation.Outcome == MovementInterpretationOutcome.DynamicBounce || activeInterpretation.Outcome == MovementInterpretationOutcome.Bouncing)
                {
                    foreach (var node in ReservationNodes)
                    {
                        node.RemoveReservation(Entity);
                    }

                    ReservationNodes.Clear();
                }
            }
            transform.rotation = rotation;

            if (currentCheckpoint != checkpoint)
            {
                if (checkpoint.Anchor != null)
                {
                    Entity.NodeAnchor = checkpoint.Anchor;
                }
                else if (checkpoint.Node != null)
                {
                    Entity.Node = checkpoint.Node;
                } else
                {
                    Entity.Coordinates = checkpoint.Coordinates;
                    Entity.AnchorDirection = checkpoint.AnchorDirection;
                } 
                Entity.LookDirection = checkpoint.LookDirection;

                currentCheckpoint = checkpoint;
                Debug.Log(PrefixLogMessage($"Shift checkpoint: {checkpoint}"));
            }

            var positionConstraint = GetComponent<PositionConstraint>();
            if (positionConstraint != null)
            {
                positionConstraint.enabled = !activeInterpretation.Movement.HasFlag(MovementType.Translating);
            }
        }
    }
}
