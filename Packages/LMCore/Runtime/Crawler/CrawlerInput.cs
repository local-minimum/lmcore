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

                var waitingMove = CheckQueueRefill(false);

                if (waitingMove != movement)
                {
                    queue.Enqueue(waitingMove);
                }

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

        [SerializeField, Tooltip("Minimum delay after having held a key for a while"), Range(0, 2)]
        private float holdingAsRePress = 0.4f;

        [
            SerializeField, 
            Tooltip("How to ease between the two delays (y=1 -> first press delay, y=0 ->minimum delay). Each re-use counts as 1 x-unit so to have 3 step easing curve should start at 0 and end at 2. Last point's right tangent should probably be constant.")
        ]
        private AnimationCurve delayEasing;

        private Movement mostRecentRefill = Movement.None;

        private bool ReadyToReuse(Press press)
        {
            float e = delayEasing.Evaluate(press.resuses);
            var neededDelta = Mathf.Lerp(holdingAsRePress, holdingAsFirstRepress, e);
            return Time.timeSinceLevelLoad - press.time > neededDelta;
        }

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

        /// <summary>
        /// Get and consume the next movement from the queue
        /// </summary>
        public Movement GetMovement()
        {
            var movement = queue.Count == 0 ? Movement.None : queue.Dequeue();

            if (movement == Movement.None)
            {
                movement = CheckQueueRefill(false);
            }

            return movement;
        }

        /// <summary>
        /// Check what the upcomming movement is without consuming it
        /// </summary>
        /// <returns>Next movement in queue</returns>
        public Movement Peak() => queue.Count == 0 ? Movement.None : queue.Peek();
        

        private void Update()
        {
            if (QueueDepth <= 0)
            {
                CheckQueueRefill(true);
            }
        }
    }
}