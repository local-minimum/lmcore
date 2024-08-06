using LMCore.AbstractClasses;
using UnityEngine;

namespace LMCore.IO
{
    public class SaveSystem<T> : Singleton<SaveSystem<T>> where T : new()
    {
        [Min(1), Tooltip("Number of saves the game allows, minimum 1")]
        public int maxSaves = 3;

        [SerializeField]
        protected AbsStorageProvider<T> Provider;

        protected string PrefixLogMessage(string message) => $"SaveSystem: {message}";
    }
}
