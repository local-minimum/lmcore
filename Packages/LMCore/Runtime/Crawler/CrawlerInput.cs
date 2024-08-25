using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LMCore.IO;
using System.Linq;

namespace LMCore.Crawler
{
    public delegate void MovementEvent(int tickId, Movement movement, float duration);

    public class CrawlerInput : MonoBehaviour
    {
        protected string PrefixLogMessage(string message) => $"Crawler input '{gEntity.name}': {message}";

        bool inputEnabled = true;
        public event MovementEvent OnMovement;

        private class HeldButtonInfo
        {
            public Movement movement;
            public float time;
            public int replays;

            public HeldButtonInfo(Movement movement)
            {
                this.movement = movement;
                time = Time.timeSinceLevelLoad;
                replays = 0;
            }

            public HeldButtonInfo()
            {
                movement = Movement.None;
                time = 0;
                replays = 0;
            }

            public void Replay()
            {
                time = Time.timeSinceLevelLoad;
                replays++;
            }

            public override string ToString() => $"[{movement} ({time})]";
        }

        private List<HeldButtonInfo> replayStack = new List<HeldButtonInfo>();

        Movement currentMovement;
        Movement nextMovement;
        Movement nextNextMovement;

        [SerializeField]
        bool replayTurns;

        [SerializeField, Range(0, 1)]
        float reuseAfterTickProgress = 0.95f;

        [SerializeField, Range(0, 1)]
        float minResuseAfterTime = 0.35f;

        public GridEntity gEntity => GetComponent<GridEntity>();

        private bool ReadyToReuse(HeldButtonInfo press)
        {
            float referenceDuration = ElasticGameClock.instance.ClampedReferenceDuration;
            return Time.timeSinceLevelLoad - press.time > Mathf.Max(referenceDuration * reuseAfterTickProgress, minResuseAfterTime);
        }

        void EnqueueMovement(Movement movement)
        {
            // TODO: Investigate if we really should both request later if failed and put 
            // movment on nextMovement with an end adjustment
            if (currentMovement == Movement.None)
            {
                currentMovement = movement;
                requestTick = !ElasticGameClock.instance.RequestTick();
                // TODO why should we enqueue twice if we don't get tick?!
                if (!requestTick)
                {
                    return;
                }

                Debug.LogWarning(PrefixLogMessage($"Movement {movement} enqueued more than once for some reason"));
            }

            if (nextMovement == Movement.None)
            {
                nextMovement = movement;
                ElasticGameClock.instance.AdjustEndOfTick();
            }
            else
            {
                nextNextMovement = movement;
            }
        }

        public void InjectMovement(Movement movement)
        {
            ClearQueue(true);
            EnqueueMovement(movement);
        }

        void ShiftQueue()
        {
            currentMovement = nextMovement;
            nextMovement = nextNextMovement;
            nextNextMovement = Movement.None;
        }

        void ClearQueue(bool includingCurrent = false)
        {
            if (includingCurrent)
            {
                currentMovement = Movement.None;
            }
            nextMovement = Movement.None;
            nextNextMovement = Movement.None;

            Debug.Log(PrefixLogMessage($"Queue cleared (fully {includingCurrent}) {QueueInfo}"));
        }

        private string QueueInfo => $"Queue {currentMovement} <- {nextMovement} <- {nextNextMovement}";

        private HeldButtonInfo GetReplay(bool force = false)
        {
            if (!inputEnabled) return null;

            var candidate = replayStack.LastOrDefault();

            // Indication that the stack is empty or not yet passed enough time
            if (candidate == null || candidate.movement == Movement.None || !force && !ReadyToReuse(candidate))
            {
                return null;
            }

            return candidate;
        }

        private void HandleCall(InputAction.CallbackContext context, Movement movement)
        {
            if (context.phase == InputActionPhase.Started)
            {
                if (!inputEnabled) return;

                var waitingButton = GetReplay();
                if (waitingButton != null)
                {
                    waitingButton.Replay();
                    EnqueueMovement(waitingButton.movement);
                }

                EnqueueMovement(movement);

                if (replayTurns || movement.IsTranslation())
                {
                    replayStack.Add(new HeldButtonInfo(movement));
                }
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                replayStack.RemoveAll(press => press.movement == movement);
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
            HandleCall(context, Movement.YawCCW);

        public void OnTurnCW(InputAction.CallbackContext context) =>
            HandleCall(context, Movement.YawCW);

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                gEntity.Interact();
            }
        }

        private void OnEnable()
        {
            ElasticGameClock.OnTickEnd += ElasticGameClock_OnTickEnd;
            ElasticGameClock.OnTickStart += ElasticGameClock_OnTickStart;
        }


        private void OnDisable()
        {
            ElasticGameClock.OnTickEnd -= ElasticGameClock_OnTickEnd;
            ElasticGameClock.OnTickStart -= ElasticGameClock_OnTickStart;
        }

        private void ElasticGameClock_OnTickStart(int tickId, float expectedDuration)
        {
            if (currentMovement != Movement.None)
            {
                Debug.Log(PrefixLogMessage($"Tick {tickId}: {currentMovement} ({expectedDuration})"));
                OnMovement?.Invoke(tickId, currentMovement, expectedDuration);
            }
        }

        bool requestTick = false;

        private void ElasticGameClock_OnTickEnd(int tickId)
        {
            ShiftQueue();
            if (currentMovement == Movement.None && inputEnabled)
            {
                var replay = GetReplay(true);
                if (replay != null)
                {
                    replay.Replay();
                    EnqueueMovement(replay.movement);
                }
            }
            else
            {
                requestTick = true;
            }
        }

        bool HasEmptyQueue => currentMovement == Movement.None 
            || nextMovement == Movement.None 
            || nextNextMovement == Movement.None;

        private void Update()
        {
            if (requestTick)
            {
                requestTick = !ElasticGameClock.instance.RequestTick();
            }

            if (HasEmptyQueue && inputEnabled)
            {
                var replay = GetReplay();
                if (replay != null)
                {
                    replay.Replay();
                    EnqueueMovement(replay.movement);

                }
            }
        }

        public void CauseFall(bool clearQueue)
        {
            DisableInput(clearQueue);
            EnqueueMovement(Movement.AbsDown);
            Debug.Log(PrefixLogMessage($"Fall with queue {QueueInfo}"));
        }

        public void DisableInput(bool clearQueue)
        {
            inputEnabled = false;
            if (clearQueue) ClearQueue(true);
            Debug.Log(PrefixLogMessage($"Disable input"));
        }

        public void EnableInput()
        {
            inputEnabled = true;
            Debug.Log(PrefixLogMessage("Enable input"));
        }

        public void EndFall(bool clearQueue)
        {
            if (clearQueue) ClearQueue(true);
            inputEnabled = true;
            Debug.Log(PrefixLogMessage("End fall"));
        }
    }
}
