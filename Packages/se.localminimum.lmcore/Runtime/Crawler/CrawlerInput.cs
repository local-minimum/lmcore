using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.Crawler
{
    public delegate void MovementEvent(int tickId, Movement movement, float duration, bool forced);

    public class CrawlerInput : MonoBehaviour
    {
        protected string PrefixLogMessage(string message) => $"CrawlerInput '{gEntity.name}': {message}";

        GridEntity _entity;
        GridEntity entity
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

        bool MovementBlocked => entity.MovementBlocked;
        bool inputEnabled = true;
        public event MovementEvent OnMovement;

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log(PrefixLogMessage($"Input Enabled ({inputEnabled})" +
                $" Movement Blocked ({MovementBlocked})" +
                $" Next move forced ({nextMovementForced})" +
                $" Queue: {QueueInfo}"));
        }

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

        bool nextMovementForced;
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

        public void InjectMovement(Movement movement, bool forced)
        {
            Debug.Log(PrefixLogMessage($"Injected movement {movement} forced({forced})"));
            nextMovement = movement;
            nextMovementForced = forced;
            nextNextMovement = Movement.None;
        }

        /// <summary>
        /// Only shifts queue, does not handle if move is forced or not
        /// </summary>
        void ShiftQueue()
        {
            currentMovement = nextMovement;
            nextMovement = nextNextMovement;
            nextNextMovement = Movement.None;
        }

        private string QueueInfo => $"Queue {currentMovement} <- {nextMovement} <- {nextNextMovement}";

        private HeldButtonInfo GetReplay(bool force = false)
        {
            if (!inputEnabled) return null;

            var candidate = replayStack.LastOrDefault();

            // Indication that the stack is empty or not yet passed enough time
            if (candidate == null || candidate.movement == Movement.None || (!force && !ReadyToReuse(candidate)))
            {
                return null;
            }

            return candidate;
        }

        private void HandleCall(InputAction.CallbackContext context, Movement movement)
        {
            if (DevConsole.DevConsole.focused)
            {
                return;
            }

            if (context.phase == InputActionPhase.Started)
            {
                if (!inputEnabled || MovementBlocked) return;

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
            if (DevConsole.DevConsole.focused)
            {
                return;
            }

            if (context.phase == InputActionPhase.Started)
            {
                gEntity.Interact();
            }
        }

        [SerializeField, Range(0, 1)]
        float translationDeadAxisMin = 0.2f;
        Movement lastTranslationMovement;

        public void OnTranslate(InputAction.CallbackContext context)
        {
            if (DevConsole.DevConsole.focused)
            {
                return;
            }

            if (context.phase == InputActionPhase.Performed)
            {
                var movement = Movement.None;
                var value = context.ReadValue<Vector2>();
                if (Mathf.Abs(value.x) > Mathf.Abs(value.y))
                {
                    if (value.x < -translationDeadAxisMin)
                    {
                        movement = Movement.StrafeLeft;
                    }
                    else if (value.x > translationDeadAxisMin)
                    {
                        movement = Movement.StrafeRight;
                    }
                }
                else
                {
                    if (value.y < -translationDeadAxisMin)
                    {
                        movement = Movement.Backward;
                    }
                    else if (value.y > translationDeadAxisMin)
                    {
                        movement = Movement.Forward;
                    }
                }

                if (movement == Movement.None)
                {
                    // Debug.Log(PrefixLogMessage($"Liminal translation input {lastTranslationMovement} -> {movement}"));
                    replayStack.Clear();
                    lastTranslationMovement = movement;
                }
                else if (movement != lastTranslationMovement)
                {
                    var allowEnqueue = inputEnabled && !MovementBlocked;
                    if (allowEnqueue)
                    {
                        var waitingButton = GetReplay();
                        if (waitingButton != null)
                        {
                            // Debug.Log(PrefixLogMessage($"Translation input injecting replay {waitingButton.movement}"));

                            waitingButton.Replay();
                            EnqueueMovement(waitingButton.movement);
                        }
                    }

                    // Debug.Log(PrefixLogMessage($"Changed translation input {lastTranslationMovement} -> {movement}"));
                    replayStack.Clear();
                    if (allowEnqueue)
                    {
                        EnqueueMovement(movement);
                        replayStack.Add(new HeldButtonInfo(movement));
                    }
                    lastTranslationMovement = movement;
                }
            }
            else
            {
                Debug.Log(PrefixLogMessage($"No translation input ({context.phase}), removing {lastTranslationMovement}"));
                replayStack.Clear();
                lastTranslationMovement = Movement.None;
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
            ShiftQueue();

            bool forced = nextMovementForced;
            nextMovementForced = false;

            if (currentMovement != Movement.None)
            {
                // Debug.Log(PrefixLogMessage($"Tick {tickId}: {currentMovement} ({expectedDuration})"));
                OnMovement?.Invoke(tickId, currentMovement, expectedDuration, forced);
            }
            else
            {
                Debug.Log(PrefixLogMessage("No movement waiting, skipping turn"));
            }
        }

        void AddReplayMove()
        {
            var replay = GetReplay(true);
            if (replay != null)
            {
                replay.Replay();
                EnqueueMovement(replay.movement);
            }
        }

        private void ElasticGameClock_OnTickEnd(int tickId)
        {
            currentMovement = Movement.None;
            if (!MovesWaiting && inputEnabled)
            {
                AddReplayMove();
            }
        }

        bool Moving => currentMovement != Movement.None;

        bool MovesWaiting => nextMovement != Movement.None
            || nextNextMovement != Movement.None;

        private void Update()
        {
            if (Moving) return;

            if (MovesWaiting)
            {
                if (!ElasticGameClock.instance.RequestTick())
                {
                    if (!ElasticGameClock.instance.AdjustEndOfTick())
                    {
                        Debug.LogWarning(PrefixLogMessage("We have moves waiting but clock refuses both new ticks and adjusting end time of tick!"));
                    }
                }
            }
            else if (inputEnabled)
            {
                AddReplayMove();
            }
        }

        public void CauseFall()
        {
            InjectMovement(Movement.AbsDown, true);
            Debug.Log(PrefixLogMessage($"Fall with queue {QueueInfo}"));
        }

        public void DisableInput(bool clearQueue)
        {
            inputEnabled = false;
            if (clearQueue)
            {
                nextMovementForced = false;
                nextMovement = Movement.None;
                nextNextMovement = Movement.None;
            }
            Debug.Log(PrefixLogMessage($"Disable input"));
        }

        public void EnableInput()
        {
            inputEnabled = true;
            Debug.Log(PrefixLogMessage("Enable input"));
        }

        public void EndFall()
        {
            inputEnabled = true;
            Debug.Log(PrefixLogMessage("End fall"));
        }
    }
}
