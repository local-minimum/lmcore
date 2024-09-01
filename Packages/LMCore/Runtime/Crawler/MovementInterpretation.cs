using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace LMCore.Crawler
{
    public class MovementInterpretation
    {
        public Direction PrimaryDirection { get; set; }
        public MovementInterpretationOutcome Outcome { get; set; }
        public List<MovementCheckpointWithTransition> Steps { get; set; } = new List<MovementCheckpointWithTransition>();

        public MovementCheckpointWithTransition Start => Steps[0];
        public MovementCheckpointWithTransition End => Steps.Last();

        public IEnumerable<float> Lengths(IDungeon dungeon)
        {
            var previous = Start.Checkpoint.Position(dungeon);
            for (int i=1, n=Steps.Count; i<n; ++i)
            {
                var current = Steps[i].Checkpoint.Position(dungeon);
                yield return (current - previous).magnitude;
                previous = current;
            }
        }

        public float Length(IDungeon dungeon) => Lengths(dungeon).Sum();

        /*
        Vector3 Lerp(
            MovementCheckpointWithTransition start, 
            MovementCheckpointWithTransition end, 
            float progress, 
            IDungeon dungeon) =>
            Vector3.Lerp(
                start.Checkpoint.Position(dungeon),
                end.Checkpoint.Position(dungeon),
                progress
            );
        */

        float SteppingProgress(float progress, int stepsPerTransition, System.Func<float, float> easing)
        {
            float stepLength = 0.5f / stepsPerTransition;
            var remainder = progress % stepLength;

            return progress - remainder + easing(remainder) * stepLength;
        }

        float SteppingProgress(float progress, int stepsPerTransition) =>
            SteppingProgress(progress, stepsPerTransition, (p) => Mathf.SmoothStep(0, 1, p));

        /*
        Vector3 LerpJump(
            MovementCheckpointWithTransition start,
            MovementCheckpointWithTransition end,
            Direction down,
            float height,
            float progress,
            IDungeon dungeon
        ) => LerpJump(
            start.Checkpoint.Position(dungeon),
            end.Checkpoint.Position(dungeon),
            down.AsLookVector3D().ToDirection(),
            height,
            progress
        );
        */

        /// <summary>
        /// Lerps position through a jump segment.
        /// </summary>
        /// <param name="start">Jump start</param>
        /// <param name="end">Jump end</param>
        /// <param name="down">Direction of down</param>
        /// <param name="height">Jump height relative to start</param>
        /// <param name="progress">Linear progress through the jump segment (0 - 1)</param>
        /// <returns></returns>
        Vector3 LerpJump(Vector3 start, Vector3 end, Vector3 down, float height, float progress)
        {
            var up = down * -1f;
            return Vector3.Lerp(start, end, progress) + up * Mathf.Cos(progress * Mathf.PI) * height;
        }

        /// <summary>
        /// Lerps position through stairs segment, matching half a tile
        /// </summary>
        /// <param name="start">Start position of stairs</param>
        /// <param name="end">End position of stairs</param>
        /// <param name="down">Direction of down in stairs</param>
        /// <param name="progress">Linear progress through the stairs segment (0 - 1)</param>
        /// <param name="steps">Number of steps in a full tile</param>
        /// <param name="entering">If it's walking into a tile</param>
        /// <returns></returns>
        Vector3 LerpStairs(Vector3 start, Vector3 end, Direction downDirection, float progress, int steps, bool entering)
        {
            var down = downDirection.AsLookVector3D();
            var delta = end - start;
            var orthogonal = Vector3.Project(delta, down);
            var planar = delta - orthogonal;

            System.Func<float, float> orthoEasing = Vector3.Dot(orthogonal, down) > 0 ? EaseInExpo : EaseOutExpo;
            var adjustedProgress = entering ? progress / 2f : 0.5f + progress / 2f;
            var adjustedStart = entering ? start : start - delta;
            var adjustedEnd = entering ? end + delta : start;

            return adjustedStart + 2f * (
                planar * SteppingProgress(adjustedProgress, steps) + 
                orthogonal * SteppingProgress(adjustedProgress, steps, orthoEasing));
        } 

        /// <summary>
        /// Lerps positions through simple segment, matching half a tile
        /// </summary>
        /// <param name="start">Start position of segment</param>
        /// <param name="end">End position of segment</param>
        /// <param name="progress">Linear progress throug segment (0 - 1)</param>
        /// <param name="easing">Function that eases a tile progress to a position</param>
        /// <param name="entering">If it's walking into a tile</param>
        /// <returns></returns>
        Vector3 LerpHalfNode(Vector3 start, Vector3 end, float progress, System.Func<float, float> easing, bool entering)
        {
            var delta = end - start;

            var adjustedProgress = entering ? progress / 2f : 0.5f + progress / 2f;
            var adjustedStart = entering ? start : start - delta;

            return adjustedStart + 2f * delta * easing(adjustedProgress);
        }

        /// <summary>
        /// Lerps positions through simple segment, matching half a tile
        /// </summary>
        /// <param name="start">Start position of segment</param>
        /// <param name="end">End position of segment</param>
        /// <param name="progress">Linear progress throug segment (0 - 1)</param>
        /// <param name="steps">Number of steps in a full tile</param>
        /// <param name="entering">If it's walking into a tile</param>
        /// <returns></returns>
        Vector3 LerpHalfNode(Vector3 start, Vector3 end, float progress, int steps, bool entering)
        {
            var delta = end - start;

            var adjustedProgress = entering ? progress / 2f : 0.5f + progress / 2f;
            var adjustedStart = entering ? start : start - delta;

            return adjustedStart + 2f * delta * SteppingProgress(adjustedProgress, steps);
        }

        bool CalculateMidpoint(GridEntity entity, out Vector3 position, out MovementTransition transition)
        {
            var count = Steps.Count;
            if (count == 2)
            {
                position = Vector3.Lerp(Start.Checkpoint.Position(entity.Dungeon), End.Checkpoint.Position(entity.Dungeon), 0.5f);
                transition = Start.Transition;
                return true;
            }
            if (count == 3)
            {
                position = Steps[1].Checkpoint.Position(entity.Dungeon);
                transition = Steps[1].Transition;
                return true;
            }

            if (count != 4)
            {
                position = Vector3.zero;
                transition = MovementTransition.None;
                return false;                
            }

            var origin = Start;
            var first = Steps[1];
            var second = Steps[2];

            var originPos = origin.Checkpoint.Position(entity.Dungeon);
            var firstPos = first.Checkpoint.Position(entity.Dungeon);
            var secondPos = second.Checkpoint.Position(entity.Dungeon);

            var up = origin.Checkpoint.Down.AsLookVector3D();
            var dUp = Vector3.Project(secondPos - firstPos, up).magnitude;
            if (dUp > entity.Abilities.minScaleHeight)
            {
                position = Vector3.zero;
                transition = MovementTransition.None;
                return false;
            }

            var direction = (firstPos - originPos);
            var dFirst = direction.magnitude;
            var norm = direction.normalized;
            var dSecond = Vector3.Project(secondPos - originPos, norm).magnitude;

            if (dSecond < dFirst)
            {
                position = originPos + norm * dSecond;
                transition = Steps[2].Transition;
                return true;
            }

            if ((dSecond - dFirst) < entity.Abilities.minForwardJump) {
                position = Vector3.Lerp(firstPos, secondPos, 0.5f);
                transition = Steps[2].Transition;
                return true;
            }



            position = Vector3.zero;
            transition = MovementTransition.None;
            return false;
        }

        Vector3 EvaluateSegment(
            MovementTransition transition, 
            AnchorTraversal traversal,
            Vector3 start, 
            Vector3 end, 
            Direction down,
            GridEntity entity,
            float segmentProgress,
            bool entering
        )
        {
            if (transition == MovementTransition.Jump)
            {
                return LerpJump(
                    start, 
                    end, 
                    down.AsLookVector3D(), 
                    entity.Abilities.jumpHeight, 
                    segmentProgress);
            }

            switch (traversal)
            {
                case AnchorTraversal.None:
                    System.Func<float, float> easing  = entity.Falling ? Linear : SmothStep;

                    return LerpHalfNode(start, end, segmentProgress, easing, entering);

                case AnchorTraversal.Walk:
                    return LerpHalfNode(start, end, segmentProgress, entity.Abilities.walkingStepsPerTransition, entering);

                case AnchorTraversal.Converyor:
                    return LerpHalfNode(start, end, segmentProgress, Linear, entering);

                case AnchorTraversal.Climb:
                    return LerpHalfNode(start, end, segmentProgress, entity.Abilities.climbingStepsPerTransition, entering);

                case AnchorTraversal.Scale:
                    // This is for climbing partially elevated things like ramps during the intermediary segment
                    // from side so 2 steps for "full tile" makes it one step
                    return LerpHalfNode(start, end, segmentProgress, 2, entering);

                case AnchorTraversal.Stairs:
                    return LerpStairs(start, end, down, segmentProgress, entity.Abilities.walkingStepsPerTransition, entering);
            }

            Debug.LogError($"Segment not understood with transition {transition} and traversal {traversal}");
            return start;
        }

        #region Ease functions
        // TODO: Test or at least visualize this
        float CubicBezier(float a, float b, float c, float d, float progress) {
            float t = Mathf.Clamp01(progress);
            return a * Mathf.Pow(1 - t, 3) + 3 * b * Mathf.Pow(1 - t, 2) * t + 3 * c * (1 - t) * Mathf.Pow(t, 2) + d * Mathf.Pow(t, 3);
        }

        // Use for stepups in stairs?
        float EaseOutExpo(float progress)
        {
            float t = Mathf.Clamp01(progress);
            return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
        }

        float EaseInExpo(float progress)
        {
            var t = Mathf.Clamp01(progress);
            return t == 0 ? 0 : Mathf.Pow(2, 10 * t - 10);
        }

        float EaseOutCubic(float progress) => 1f - Mathf.Pow(1f - Mathf.Clamp01(progress), 3f);

        float Linear(float progress) => progress;

        float SmothStep(float progress) => Mathf.SmoothStep(0, 1, progress);
        #endregion


        private enum Segment { First, Intermediary, Last }
        private Segment CurrentSegment = Segment.First;
        float segmentStartProgress = 0;

        public IDungeonNode CurrentNode
        {
            get
            {
                if (CurrentSegment == Segment.First)
                {
                    return Start.Checkpoint.Node;
                } else
                {
                    return End.Checkpoint.Node;
                }
            }
        }

        public void RefuseMovement() {
            var start = Start;
            MovementCheckpointWithTransition intermediary = new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(start.Checkpoint, PrimaryDirection),
                Transition = Start.Transition,
            };

            Steps.Clear();
            Steps.Add(start);
            Steps.Add(intermediary);
            Steps.Add(start);

            Outcome = MovementInterpretationOutcome.Bouncing;
            CurrentSegment = Segment.Last;
        }
        
        public Vector3 Evaluate(GridEntity entity, float progress)
        {
            // 1. Figure out segment active from total length and progressNumber of steps in a full tile
            //    Take into account freeze frames and step-up/downs and jumps for misaligned tiles
            // 2. If embarking on the transition between first and second path check if valid and
            //    if not evolve self to refused movement.
            // 3. Given transition of segment scale progress to internal 0-1 progress of segment
            //    and lerp its positions using potentially ajusted intermediaries

            if (Outcome == MovementInterpretationOutcome.Bouncing)
            {
                progress = CubicBezier(0.1f, 0.62f, 0.9f, 0.38f, progress);
            } else if (Outcome == MovementInterpretationOutcome.Landing)
            {
                progress = EaseOutCubic(progress);
            }

            var start = Start;
            var end = End;

            if (Steps.Count == 2 && start.Transition == MovementTransition.Jump)
            {
                return EvaluateSegment(
                    MovementTransition.Jump,
                    AnchorTraversal.None,
                    start.Checkpoint.Position(entity.Dungeon), 
                    end.Checkpoint.Position(entity.Dungeon), 
                    start.Checkpoint.Down, 
                    entity, 
                    progress,
                    true); // It doesn't matter for true false here
            }
            else if (CalculateMidpoint(entity, out var mid, out var transition))
            {
                if (progress < 0.5f && CurrentSegment == Segment.First)
                {
                    var startPos = start.Checkpoint.Position(entity.Dungeon);
                    return EvaluateSegment(
                        start.Transition,
                        start.Checkpoint.Traversal,
                        startPos,
                        mid,
                        start.Checkpoint.Down,
                        entity,
                        progress / 0.5f,
                        false
                    );
                } else
                {
                    CurrentSegment = Segment.Last;
                    var endPos = end.Checkpoint.Position(entity.Dungeon);
                    return EvaluateSegment(
                        transition,
                        end.Checkpoint.Traversal,
                        mid,
                        endPos,
                        end.Checkpoint.Down,
                        entity,
                        (progress - 0.5f) / 0.5f,
                        true);
                }
            } else 
            {
                // Some multistep procedure
                var lengths = Lengths(entity.Dungeon).ToList();
                var segments = lengths.Count;

                if (segments != 3)
                {
                    Debug.LogError($"{entity.name} is attempting a {segments} part transition, we don't know how to handle that");
                    return Vector3.Lerp(
                        start.Checkpoint.Position(entity.Dungeon),
                        end.Checkpoint.Position(entity.Dungeon),
                        progress
                    );
                }

                var totalLength = lengths.Sum();
                var progressCheckpoints = lengths.Select(l => l / totalLength).ToList();
                var activeSegment = 0;
                for ( var i = 0; i < segments; i++ )
                {
                    if (progress > progressCheckpoints[i])
                    {
                        activeSegment++;
                    }
                }

                // Adjust activeSegment by CurrentSegment
                activeSegment = Mathf.Min(Mathf.Max(activeSegment, (int)CurrentSegment), 2);

                var midStart = Steps[1];
                var midEnd = Steps[2];

                // Check if we may go from 0 -> 1
                if (activeSegment > 0 && CurrentSegment == Segment.First)
                {
                    var delta = midEnd.Checkpoint.Position(entity.Dungeon) - midStart.Checkpoint.Position(entity.Dungeon);

                    if (midStart.Transition == MovementTransition.Jump)
                    {
                        var forward = PrimaryDirection.AsLookVector3D();
                        if (Vector3.Project(delta, forward).magnitude > entity.Abilities.maxForwardJump)
                        {
                            RefuseMovement();
                        }
                    } else if (midStart.Transition == MovementTransition.Grounded)
                    {
                        var up = midStart.Checkpoint.Down.Inverse().AsLookVector3D();
                        if (Vector3.Dot(delta, up) > entity.Abilities.maxScaleHeight)
                        {
                            RefuseMovement();
                        }
                    }
                }

                var startIdx = Mathf.Max(activeSegment, (int)CurrentSegment);
                var newSegment = (Segment)startIdx;

                if (CurrentSegment != newSegment)
                {
                    segmentStartProgress = progress;
                }
                CurrentSegment = newSegment;


                var remainingProgressAtSegmentStart = 1 - segmentStartProgress;
                var totalRemainingSegmentLengths = lengths.Skip(startIdx).Sum();
                var segmentPartOfTotalRemaining = lengths[startIdx] / totalRemainingSegmentLengths;
                var segmentDuration = segmentPartOfTotalRemaining * remainingProgressAtSegmentStart;
                var segmentProgress = (progress - segmentStartProgress) / segmentDuration;

                var pt1 = Steps[startIdx];
                var pt2 = Steps[startIdx + 1];

                return EvaluateSegment(
                    pt1.Transition,
                    pt1.Checkpoint.Traversal,
                    pt1.Checkpoint.Position(entity.Dungeon), 
                    pt2.Checkpoint.Position(entity.Dungeon), 
                    pt1.Checkpoint.Down, 
                    entity, 
                    segmentProgress,
                    CurrentSegment != Segment.First);
            }
        }
    }
}
