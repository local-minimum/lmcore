using LMCore.IO;
using UnityEngine;

[ExecuteInEditMode]
public class SaveTest : MonoBehaviour
{
    [SerializeField]
    SerializableDictionary<string, int> serializableDictionary = new SerializableDictionary<string, int>();

    [SerializeField]
    SerializableDictionary<string, GameObject> serializableDictionary2 = new SerializableDictionary<string, GameObject>();

    private void Start()
    {
        serializableDictionary.TryAdd("test", 1);
        serializableDictionary.TryAdd("me", 2);

        foreach (var kvp in serializableDictionary)
        {
            Debug.Log(kvp.Key);
        }
    }
}
