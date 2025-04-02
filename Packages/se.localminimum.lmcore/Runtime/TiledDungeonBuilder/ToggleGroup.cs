using LMCore.UI;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class ToggleGroup : MonoBehaviour
    {
        Dictionary<int, HashSet<System.Action>> callbacks = new();

        public void RegisterReciever(int group, System.Action callback)
        {
            if (!callbacks.ContainsKey(group))
            {
                callbacks[group] = new HashSet<System.Action>() { callback };
            }
            else
            {
                callbacks[group].Add(callback);
            }
        }

        public void UnregisterReciever(int group, System.Action callback)
        {
            if (callbacks.ContainsKey(group))
            {
                callbacks[group].Remove(callback);
            }
        }

        List<int> waitingToggles = new List<int>();

        public void Toggle(int group)
        {
            if (callbacks.ContainsKey(group))
            {
                if (AbsMenu.PausingGameplay)
                {
                    waitingToggles.Add(group);
                }
                else
                {
                    foreach (System.Action action in callbacks[group])
                    {
                        action.Invoke();
                    }
                }
            }
        }

        private void Update()
        {
            if (AbsMenu.PausingGameplay || waitingToggles.Count == 0) return;

            foreach (int group in waitingToggles)
            {
                foreach (System.Action action in callbacks[group])
                {
                    action.Invoke();
                }
            }

            waitingToggles.Clear();
        }
    }
}
