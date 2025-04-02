using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.IO
{
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        public SerializableDictionary() { }
        public SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> elements)
        {
            if (elements != null)
            {
                foreach (var (key, value) in elements)
                {
                    if (key == null)
                    {
                        Debug.LogError($"Illegal null-key for '{value}', skipping...");
                        continue;
                    }
                    Add(key, value);
                }
            }
        }

        [SerializeField, HideInInspector] private List<TKey> keys = new List<TKey>();
        [SerializeField, HideInInspector] private List<TValue> values = new List<TValue>();

        public void OnAfterDeserialize()
        {
            // We clear the actual dictionary if there's something there before loading the serialized state
            Clear();

            int nKeys = keys.Count;
            if (nKeys != values.Count) throw new System.TypeLoadException($"Recieved {keys.Count} keys and {values.Count} values");
            if (nKeys != keys.ToHashSet().Count) throw new System.TypeLoadException("All keys not unique");

            for (int i = 0; i < nKeys; i++)
            {
                this[keys[i]] = values[i];
            }
        }

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            foreach (var (key, value) in this)
            {
                keys.Add(key);
                values.Add(value);
            }
        }

        public override string ToString() => $"{{{string.Join(", ", this.Select(kvp => $"{kvp.Key}={kvp.Value}"))}}}";
    }
}
