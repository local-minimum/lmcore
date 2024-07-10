using LMCore.Extensions;
using LMCore.IO;
using LMCore.Juice;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
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
        List<EntityState> animationStates;

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

            if (animationStates == null || animationStates.Count > 0)
            {
                var last = allowedAnimation ? animationStates.Last() : animationStates.First();
                gEntity.Position = last.Coordinates;
                gEntity.Anchor = last.Anchor;
                gEntity.LookDirection = last.LookDirection;
                gEntity.RotationRespectsAnchorDirection = last.RotationRespectsAnchorDirection;
                gEntity.TransportationMode = last.TransportationMode;
                gEntity.Sync();
            } else if (animationOutcome == MovementOutcome.Refused)
            {
                gEntity.Sync();
                WallHitShakeTarget?.Shake();

            }


            Debug.Log("Smooth movement");

            OnMoveEnd?.Invoke(
                gEntity, 
                allowedAnimation
            );
        }

        MovementOutcome animationOutcome;


        private void MovementInterpreter_OnEntityMovement(GridEntity entity, MovementOutcome outcome, List<EntityState> states, float duration)
        {
            if (entity != gEntity) return;

            animationOutcome = outcome;

            var first = states.FirstOrDefault();

            animationStates = states;
            onlyTurning = states.All(s => s.Coordinates == first.Coordinates);
            animating = true;

            animationStartTime = Time.timeSinceLevelLoad;
            animationInterpolationStart = 0;
            animationDuration = onlyTurning ? duration * turnDurationFactor : duration;
        }

        float AnimationProgress =>
            animationInterpolationStart + (1 - animationInterpolationStart) * Mathf.Clamp01((Time.timeSinceLevelLoad - animationStartTime) / animationDuration);

        bool allowedAnimation => 
            animationOutcome == MovementOutcome.NodeInternal || animationOutcome == MovementOutcome.NodeExit;

        private EntityState GetAnimationTransition(float progress, out EntityState endState)
        {
            var n = animationStates.Count;
            var steps = n - 1;

            int startIdx = 0;

            if (steps > 1)
            {
                var partLength = 1f / steps;
                startIdx = Mathf.FloorToInt(progress / partLength);
                // Debug.Log($"n {n} steps {steps} partLength {partLength} start {startIdx} progress {progress}");
            }

            if (startIdx >= steps)
            {
                endState = animationStates[steps + 1];
                return animationStates[steps];
            }

            endState = animationStates[startIdx + 1];
            return animationStates[startIdx];
        }

        private void Update()
        {
            if (!animating) return;

            if (animationOutcome == MovementOutcome.Refused)
            {
                Debug.Log("Refused");
                EndAnimation();
                return;
            }

            var progress = AnimationProgress;

            if (progress == 1)
            {
                Debug.Log("Completed");
                EndAnimation();
                return;
            }
        
            var startState = GetAnimationTransition(progress, out var endState);
            var adjustedProgress = allowedAnimation ? progress : 2 * bounceAtProgress - progress;

            // Debug.Log($"{adjustedProgress} : {startState} -> {endState}");

            if (
                (startState.LookDirection != endState.LookDirection)
                || startState.Anchor != endState.Anchor && (startState.RotationRespectsAnchorDirection || endState.RotationRespectsAnchorDirection)
             )
            {
                var startRotation = startState.LookDirection.AsQuaternion(startState.Anchor, startState.RotationRespectsAnchorDirection);
                var endRotation = endState.LookDirection.AsQuaternion(endState.Anchor, endState.RotationRespectsAnchorDirection);
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, adjustedProgress);
            }

            if (startState.Coordinates != endState.Coordinates || startState.Anchor != endState.Anchor)
            {
                // TODO: Lerping should not be diagonally!
                var startPosition = startState.Coordinates.ToPosition(GridSizeProvider.GridSize) +
                    gEntity.CalculateAnchorOffset(
                        startState.Anchor, 
                        startState.RotationRespectsAnchorDirection
                    );
                var endPosition = endState.Coordinates.ToPosition(GridSizeProvider.GridSize) + 
                    gEntity.CalculateAnchorOffset(
                        endState.Anchor, 
                        endState.RotationRespectsAnchorDirection
                    );

                transform.position = Vector3.Lerp(startPosition, endPosition, adjustedProgress);
            }

        }
    }
}
