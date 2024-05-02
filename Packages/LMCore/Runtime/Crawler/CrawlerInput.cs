using LMCore.AbstractClasses;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.Crawler
{
    public class CrawlerInput : BlockableActions
    {
        private Queue<Movement> queue = new Queue<Movement>();

        private class Press
        {
            public Movement movement;
            public float time;
            public int resuses;

            public Press(Movement movement)
            {
                this.movement = movement;
                time = Time.timeSinceLevelLoad;
                resuses = 0;
            }

            public Press()
            {
                movement = Movement.None;
                time = 0;
                resuses = 0;
            }

            public void Reuse()
            {
                time = Time.timeSinceLevelLoad;
                resuses++;
            }

            public override string ToString() => $"[{movement} ({time})]";
        }

        private List<Press> pressStack = new List<Press>();

        public string PressStackInfo => string.Join(" < ", pressStack);
        private void HandleCall(InputAction.CallbackContext context, Movement movement)
        {
            if (context.phase == InputActionPhase.Started)
            {
                if (ActionsBlocked) return;

                pressStack.Add(new Press(movement));

                queue.Enqueue(movement);
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                pressStack.RemoveAll(press => press.movement == movement);
            }
        }

        public void OnMoveForward(InputAction.CallbackContext context) =>
            HandleCall(context, Movement.Forward);

        public void OnMoveBackward(InputAction.CallbackContext context) =>
            HandleCall(context, Movement.Backward);

        public void OnStrafeLeft(InputAction.CallbackContext context) =>
            HandleCall(context, Movement.StrafeLeft);

        public void OnStrafeRight(InputAction.CallbackContext context) =>
            HandleCall(context, Movement.StrafeRight);

        public void OnTurnCCW(InputAction.CallbackContext context) =>
            HandleCall(context, Movement.TurnCCW);

        public void OnTurnCW(InputAction.CallbackContext context) =>
            HandleCall(context, Movement.TurnCW);

        public void Clear()
        {
            queue.Clear();
            pressStack.Clear();
        }

        public int QueueDepth => queue.Count;

        [SerializeField, Tooltip("Initial delay before held key is counted again"), Range(0, 2)]
        private float holdingAsFirstRepress = 0.8f;

        [SerializeField, Tooltip("Continued re-press time"), Range(0, 2)]
        private float holdingAsRePress = 0.4f;

        [SerializeField]
        private AnimationCurve delayEasing;

        private Movement mostRecentRefill = Movement.None;

        private bool ReadyToReuse(Press press)
        {
            var neededDelta = Mathf.Lerp(holdingAsFirstRepress, holdingAsRePress, delayEasing.Evaluate(press.resuses));
            return Time.timeSinceLevelLoad - press.time > neededDelta;
        }

        // TODO: Perhaps refill before queuing?
        private Movement CheckQueueRefill(bool enqueue)
        {
            var candidate = pressStack.LastOrDefault();

            // Indication that the stack is empty or not yet passed enough time
            if (candidate == null || candidate.movement == Movement.None || !ReadyToReuse(candidate))
            {
                // If the movement that we're awaiting is no the same as the pressent it's new and we
                // use longer time before redoing it
                if (candidate?.movement != mostRecentRefill)
                {
                    mostRecentRefill = Movement.None;
                }
                return Movement.None;
            }

            // Add movement to queue and update time
            if (enqueue)
            {
                queue.Enqueue(candidate.movement);
            }

            candidate.Reuse();
            mostRecentRefill = candidate.movement;

            return candidate.movement;
        }

        public Movement GetMovement()
        {
            var movement = queue.Count == 0 ? Movement.None : queue.Dequeue();

            if (movement == Movement.None)
            {
                movement = CheckQueueRefill(false);
            }

            return movement;
        }

        private void Update()
        {
            if (QueueDepth <= 0)
            {
                CheckQueueRefill(true);
            }
        }
    }
}