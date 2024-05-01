using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LMCore.AbstractClasses;
using LMCore.IO;

namespace LMCore.Crawler
{
    public class CrawlerInput :  BlockableActions
    {
        Queue<Movement> queue = new Queue<Movement>();

        private class Press
        {
            public Movement movement;
            public float time;

            public Press(Movement movement, float time)
            {
                this.movement = movement;
                this.time = time;
            }
            
            public Press()
            {
                movement = Movement.None;
                time = 0;
            }
        }

        List<Press> pressStack = new List<Press>();

        private void HandleCall(InputAction.CallbackContext context, Movement movement)
        {
            if (context.phase == InputActionPhase.Started)
            {
                if (ActionsBlocked) return;

                pressStack.Add(new Press(movement, Time.timeSinceLevelLoad));

                queue.Enqueue(movement);
            } else if (context.phase == InputActionPhase.Canceled)
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
        float holdingAsFirstRepress = 0.8f;
        [SerializeField, Tooltip("Continued re-press time"), Range(0, 2)]
        float holdingAsRePress = 0.4f;

        Movement mostRecentRefill = Movement.None;

        Movement CheckQueueRefill(bool enqueue)
        {
            var candidate = pressStack.LastOrDefault(press => Time.timeSinceLevelLoad - press.time < holdingAsRePress);

            // Indication that the stack is empty or not yet passed enough time
            if (candidate.movement == Movement.None)
            {
                // If the movement that we're awaiting is no the same as the pressent it's new and we
                // use longer time before redoing it
                if (pressStack.LastOrDefault().movement != mostRecentRefill)
                {
                    mostRecentRefill = Movement.None;
                }
                return Movement.None;
            }

            // If it's the first time for this movement lets reset recent, because it's new
            if (candidate.movement != mostRecentRefill && Time.timeSinceLevelLoad - candidate.time < holdingAsFirstRepress)
            {
                mostRecentRefill = Movement.None;
                return Movement.None;
            }

            // Add movement to queue and update time
            if (enqueue)
            {
                queue.Enqueue(candidate.movement);
            }

            candidate.time = Time.timeSinceLevelLoad;
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
