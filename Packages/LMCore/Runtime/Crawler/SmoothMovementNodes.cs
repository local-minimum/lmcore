using LMCore.Extensions;
using LMCore.IO;
using LMCore.Juice;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    public struct SmoothMovementCheckpoints
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public SmoothMovementCheckpoints(GridEntity entity, EntityState state, float gridSize)
        {
            Position = state.Coordinates.ToPosition(gridSize) + 
                entity.CalculateAnchorOffset(
                    state.Anchor, 
                    state.RotationRespectsAnchorDirection);

            Rotation = state.LookDirection.AsQuaternion(
                state.Anchor,
                state.RotationRespectsAnchorDirection
            );
        }

        public static SmoothMovementCheckpoints Lerp(SmoothMovementCheckpoints from, SmoothMovementCheckpoints to, float progress)
        {
            return new SmoothMovementCheckpoints { 
                Position = Vector3.Lerp(from.Position, to.Position, progress), 
                Rotation = Quaternion.Lerp(from.Rotation, to.Rotation, progress) 
            };
        }
    }

    public class SmoothMovementNodes : MonoBehaviour, IEntityMover
    {
        public event EntityMovementStartEvent OnMoveStart;
        public event EntityMovementEndEvent OnMoveEnd;
        public bool Enabled => enabled && gameObject.activeSelf;

        public IGridSizeProvider GridSizeProvider { get; set; }
        public IDungeon Dungeon { get; set; }

        [SerializeField, Range(0, 1), Tooltip("Part of tick used for turns, should not be 0")]
        float turnDurationFactor = 1f;

        [SerializeField]
        NodeShaker WallHitShakeTarget;

        [SerializeField, Range(0, 0.5f)]
        float bounceAtProgress = 0.4f;

        GridEntity gEntity;
        private IEntityMovementInterpreter movementInterpreter;

        void Awake()
        {
            gEntity = GetComponent<GridEntity>();
            movementInterpreter = GetComponent<IEntityMovementInterpreter>();

            GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
            enabled = !GameSettings.InstantMovement.Value;

            if (enabled)
            {
                gEntity.Sync();
            }
        }

        private void OnDestroy()
        {
            GameSettings.InstantMovement.OnChange -= InstantMovement_OnChange;

            Movers.Deactivate(this);
        }

        private void InstantMovement_OnChange(bool value)
        {
            enabled = !value;
        }

        private void OnEnable()
        {
            movementInterpreter.OnEntityMovement += MovementInterpreter_OnEntityMovement;

            ElasticGameClock.OnTickEnd += ElasticGameClock_OnTickEnd;
            ElasticGameClock.OnTickEndAdjustment += ElasticGameClock_OnTickEndAdjustment;

            gEntity.OnLand.AddListener(OnLand);

            Movers.Activate(this);
        }


        private void OnDisable()
        {
            movementInterpreter.OnEntityMovement -= MovementInterpreter_OnEntityMovement;

            ElasticGameClock.OnTickEnd -= ElasticGameClock_OnTickEnd;
            ElasticGameClock.OnTickEndAdjustment -= ElasticGameClock_OnTickEndAdjustment;

            gEntity.OnLand.RemoveListener(OnLand);

            Movers.Deactivate(this);
        }

        public void OnLand()
        {
            WallHitShakeTarget?.Shake();
        }


        int animationTickId;
        float animationStartTime;
        float animationDuration;
        float animationInterpolationStart;
        bool onlyTurning;
        bool animating = false;
        List<SmoothMovementCheckpoints> animationCheckpoints;
        EntityState animationStart;
        EntityState animationEnd;

        private void ElasticGameClock_OnTickEndAdjustment(int tickId, float unadjustedProgress, float adjustedProgress, float endTime)
        {
            if (animationTickId != tickId) return;

            var turnCompensatedUnadjustedProgress = onlyTurning ? Mathf.Clamp01(unadjustedProgress / turnDurationFactor) : unadjustedProgress;

            animationStartTime = Time.timeSinceLevelLoad;

            var remainder = endTime - animationStartTime;
            if (turnCompensatedUnadjustedProgress < 1)
            {

                animationDuration = onlyTurning ? remainder * turnDurationFactor : remainder;
            }
            else
            {
                animationDuration = 0.00000001f;
            }
            animationInterpolationStart = turnCompensatedUnadjustedProgress;
        }

        private void ElasticGameClock_OnTickEnd(int tickId)
        {
            if (animationTickId == tickId)
            {
                EndAnimation();
            }
        }

        void EndAnimation()
        {
            if (!animating) return;

            animating = false;

            var synchState = allowedAnimation ? animationEnd : animationStart;

            gEntity.Position = synchState.Coordinates;
            gEntity.Anchor = synchState.Anchor;
            gEntity.LookDirection = synchState.LookDirection;
            gEntity.RotationRespectsAnchorDirection = synchState.RotationRespectsAnchorDirection;
            gEntity.TransportationMode = synchState.TransportationMode;
            gEntity.Sync();

            if (animationOutcome == MovementOutcome.Refused)
            {
                WallHitShakeTarget?.Shake();

            }


            OnMoveEnd?.Invoke(
                gEntity, 
                allowedAnimation
            );
        }

        MovementOutcome animationOutcome;


        private void MovementInterpreter_OnEntityMovement(int tickId, GridEntity entity, MovementOutcome outcome, List<EntityState> states, float duration)
        {
            if (entity != gEntity) return;

            animationTickId = tickId;

            var completeStates = new List<SmoothMovementCheckpoints>();
            if (animating)
            {
                Debug.LogWarning($"{name} should not have been animating at this point but were");
                EndAnimation();
            }

            completeStates.AddRange(
                states.Select(s => new SmoothMovementCheckpoints(entity, s, GridSizeProvider.GridSize))
            );

            animationOutcome = outcome;
            animationCheckpoints = completeStates;

            var first = completeStates.FirstOrDefault();
            onlyTurning = completeStates.All(s => s.Position == first.Position);

            animating = true;

            animationStart = states.FirstOrDefault();
            animationEnd = states.LastOrDefault();

            animationStartTime = Time.timeSinceLevelLoad;
            animationInterpolationStart = 0;
            animationDuration = onlyTurning ? duration * turnDurationFactor : duration;
        }

        float AnimationProgress =>
            animationInterpolationStart + (1 - animationInterpolationStart) * Mathf.Clamp01((Time.timeSinceLevelLoad - animationStartTime) / animationDuration);

        bool allowedAnimation => 
            animationOutcome == MovementOutcome.NodeInternal || animationOutcome == MovementOutcome.NodeExit;

        private SmoothMovementCheckpoints GetAnimationTransition(
            float progress, 
            out SmoothMovementCheckpoints endState, 
            out float stateProgress)
        {
            var n = animationCheckpoints.Count;
            var steps = n - 1;

            int startIdx = 0;

            if (steps > 1)
            {
                var partLength = 1f / steps;
                startIdx = Mathf.FloorToInt(progress / partLength);
                stateProgress = Mathf.Clamp01((progress - startIdx * partLength) / partLength);
                // Debug.Log($"n {n} steps {steps} partLength {partLength} start {startIdx} progress {progress}");
            } else
            {
                stateProgress = progress;
            }

            if (startIdx >= steps)
            {
                endState = animationCheckpoints[steps + 1];
                return animationCheckpoints[steps];
            }

            endState = animationCheckpoints[startIdx + 1];
            return animationCheckpoints[startIdx];
        }

        private void Update()
        {
            if (!animating) return;

            if (animationOutcome == MovementOutcome.Refused)
            {
                EndAnimation();
                return;
            }

            var progress = AnimationProgress;

            if (progress == 1)
            {
                EndAnimation();
                return;
            }
        
            var adjustedProgress = allowedAnimation || progress < bounceAtProgress ? progress : 2 * bounceAtProgress - progress;
            var startState = GetAnimationTransition(adjustedProgress, out var endState, out var stateProgress);

            // Debug.Log($"{adjustedProgress} : {startState} -> {endState}");

            if (startState.Rotation != endState.Rotation)
            {
                transform.rotation = Quaternion.Lerp(startState.Rotation, endState.Rotation, stateProgress);
            }

            if (startState.Position != endState.Position)
            {
                transform.position = Vector3.Lerp(startState.Position, endState.Position, stateProgress);
            }

        }
    }
}
