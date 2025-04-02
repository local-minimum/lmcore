using UnityEngine;

namespace LMCore.AbstractClasses
{
    /// <summary>
    /// Deriving from this ensures only one instance of the class will be
    /// present at a time.
    /// 
    /// See static methods `InstanceOrCreate` and `InstanceOrResource` as ways to access it.
    /// Only use the public static field `instance` if it can be absolutely certain there will
    /// be an instance present, or null handling is included.
    /// </summary>
    /// <typeparam name="T">Type of the singleton class</typeparam>
    /// <typeparam name="TSelf">To allow for generic classes this would be the subclass, else it should be the same as `T`</typeparam>
    public abstract class Singleton<T, TSelf> : MonoBehaviour where T : MonoBehaviour where TSelf : Singleton<T, TSelf>
    {
        private static TSelf _instance;

        public static TSelf instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TSelf>(true);
                }
                return _instance;
            }
        }

        public static TSelf InstanceOrCreate(string name = null, Transform parent = null)
        {
            var i = instance;
            if (i) return i;

            var go = new GameObject(name ?? typeof(TSelf).Name, new[] { typeof(TSelf) });
            if (parent != null)
            {
                go.transform.SetParent(parent);
            }

            return go.GetComponent<TSelf>();
        }

        public static TSelf InstanceOrResource(string resourcePath, Transform parent = null)
        {
            var i = instance;
            if (i != null) return i;

            var prefab = Resources.Load<TSelf>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"{typeof(TSelf).Name}: Could not locate resource @ '{resourcePath}'");
                return null;
            }

            i = Instantiate(prefab, parent);
            i.name = typeof(TSelf).Name;

            return i;
        }

        protected void Awake()
        {
            if (_instance == null)
            {
                _instance = this as TSelf;
            }
            else if (_instance != (this as TSelf))
            {
                Debug.LogWarning($"Duplicate Singleton: {_instance} exists yet {this} also exists");
                Destroy(this);
            }
        }

        protected void OnDestroy()
        {
            if (_instance == (this as TSelf))
            {
                _instance = null;
            }
        }
    }
}