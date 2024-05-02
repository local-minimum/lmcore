using UnityEngine;

namespace LMCore.AbstractClasses
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>(true);
                }
                return _instance;
            }
        }

        protected void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else if (_instance != this as T)
            {
                Debug.LogError($"Duplicate Singleton: {_instance} exists yet {this} also exists");
                Destroy(this);
            }
        }

        protected void OnDestroy()
        {
            if (_instance == this as T)
            {
                _instance = null;
            }
        }
    }
}