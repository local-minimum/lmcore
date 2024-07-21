using LMCore.AbstractClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiledDungeon
{
    public class ToggleGroup : Singleton<ToggleGroup> 
    {
        Dictionary<int, HashSet<System.Action>> callbacks = new ();

        public void RegisterReciever(int group, System.Action callback)
        {
            if (!callbacks.ContainsKey(group)) {
                callbacks[group] = new HashSet<System.Action>() { callback };
            } else
            {
                callbacks[group].Add(callback);
            }
        }

        public void UnregisterReciever(int group, System.Action callback) { 
            if (callbacks.ContainsKey(group))
            {
                callbacks[group].Remove(callback);
            }
        }

        public void Toggle(int group)
        {
            if (callbacks.ContainsKey(group))
            {
                foreach (System.Action action in callbacks[group])
                {
                    action.Invoke();
                }
            }
        }
    }
}
