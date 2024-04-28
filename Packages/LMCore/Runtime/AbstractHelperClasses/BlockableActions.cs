using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.AbstractClasses
{
    abstract public class BlockableActions : MonoBehaviour
    {
        private static HashSet<MonoBehaviour> actionsBlockers = new HashSet<MonoBehaviour>();
        public static void BlockAction(MonoBehaviour behaviour) => actionsBlockers.Add(behaviour);
        public static void RemoveActionBlock(MonoBehaviour behaviour) => actionsBlockers.Remove(behaviour);
        public static bool ActionsBlocked => actionsBlockers.Count > 0;
    }
}