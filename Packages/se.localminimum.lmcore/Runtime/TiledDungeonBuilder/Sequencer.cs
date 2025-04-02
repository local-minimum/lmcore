using LMCore.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class Sequencer : MonoBehaviour
    {
        protected string PrefixLogMessage(string message) =>
            $"Secuencer: {message}";

        struct Reciever
        {
            public int PhaseId;
            public System.Action Callback;

            public bool Matches(int phaseId, System.Action callback) => phaseId == PhaseId && callback == Callback;
        }


        Dictionary<int, List<Reciever>> callbacks = new Dictionary<int, List<Reciever>>();

        public void RegisterReciever(int sequenceId, int phaseId, System.Action callback)
        {
            if (!callbacks.ContainsKey(sequenceId))
            {
                callbacks[sequenceId] = new List<Reciever>() { new Reciever { PhaseId = phaseId, Callback = callback } };
            }
            else
            {
                callbacks[sequenceId].Add(new Reciever { PhaseId = phaseId, Callback = callback });
            }
            // Debug.Log(PrefixLogMessage($"registering callback for {sequenceId} phase {phaseId} ({callbacks[sequenceId].Count} callbacks in sequence)"));
        }

        public void UnregisterReciever(int sequenceId, int phaseId, System.Action callback)
        {
            if (callbacks.ContainsKey(sequenceId))
            {
                var removed = callbacks[sequenceId].RemoveAll(r => r.Matches(phaseId, callback));
                // Debug.Log(PrefixLogMessage($"removed {removed} callbacks for sequence {sequenceId} phase {phaseId}"));
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage($"has no sequence id {sequenceId}"));
            }
        }

        struct WaitingForInvoke
        {
            public int SequenceId;
            public int PhaseId;
        }

        List<WaitingForInvoke> waitingForInvokes = new List<WaitingForInvoke>();

        public void Invoke(int sequenceId, int phaseId)
        {
            if (AbsMenu.PausingGameplay)
            {
                waitingForInvokes.Add(new WaitingForInvoke() { SequenceId = sequenceId, PhaseId = phaseId });
                return;
            }

            if (!callbacks.ContainsKey(sequenceId))
            {
                Debug.LogWarning(PrefixLogMessage($"doesn't have a sequence {sequenceId}"));
                return;
            }

            var n = 0;
            foreach (var reciever in callbacks[sequenceId].Where(r => r.PhaseId == phaseId))
            {
                reciever.Callback();
                n++;
            }

            if (n == 0)
            {
                Debug.LogWarning(PrefixLogMessage($"doesn't have any recievier for sequence {sequenceId} phase {phaseId}"));
            }
        }


        private void Update()
        {
            if (AbsMenu.PausingGameplay || waitingForInvokes.Count == 0) return;

            foreach (var waitingForInvoke in waitingForInvokes)
            {
                Invoke(waitingForInvoke.SequenceId, waitingForInvoke.PhaseId);
            }
            waitingForInvokes.Clear();
        }
    }
}
