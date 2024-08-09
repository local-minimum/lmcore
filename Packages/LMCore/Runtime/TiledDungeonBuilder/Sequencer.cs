using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class Sequencer : MonoBehaviour
    {
        struct Reciever
        {
            public int PhaseId;
            public System.Action Callback;

            public bool Matches(int phaseId, System.Action callback) => phaseId == PhaseId && callback == Callback;
        }

        Dictionary<int, List<Reciever>> callbacks = new Dictionary<int, List<Reciever>> ();

        public void RegisterReciever(int sequenceId, int phaseId, System.Action callback)
        {
            if (!callbacks.ContainsKey(sequenceId))
            {
                callbacks[sequenceId] = new List<Reciever>() { new Reciever { PhaseId = phaseId, Callback = callback } };
            } else
            {
                callbacks[sequenceId].Add(new Reciever { PhaseId = phaseId, Callback = callback });
            }
            Debug.Log($"Sequencer registering callback for {sequenceId} phase {phaseId} ({callbacks[sequenceId].Count} callbacks in sequence)");
        }

        public void UnregisterReciever(int sequenceId, int phaseId, System.Action callback)
        {
            if (callbacks.ContainsKey(sequenceId))
            {
                var removed = callbacks[sequenceId].RemoveAll(r => r.Matches(phaseId, callback));
                Debug.Log($"Sequencer removed {removed} callbacks for sequence {sequenceId} phase {phaseId}");
            } else
            {
                Debug.LogWarning($"Sequencer has no sequence id {sequenceId}");
            }
        }

        public void Invoke(int sequenceId, int phaseId)
        {
            if (!callbacks.ContainsKey(sequenceId))
            {
                Debug.LogWarning($"Sequencer doesn't have a sequence {sequenceId}");
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
                Debug.LogWarning($"Sequencer doesn't have any recievier for sequence {sequenceId} phase {phaseId}");
            } else
            {
                Debug.Log($"Sequencer invokes {n}/{callbacks[sequenceId].Count} callbacks for sequence {sequenceId} phase {phaseId}");
            }
        }
    }
            
}
