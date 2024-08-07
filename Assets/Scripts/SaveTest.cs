using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveTest : MonoBehaviour
{
    [System.Serializable]
    class Save
    {
        public string Id;
        public Payload payload;
        public Vector3Int Test;


        public Save(string id, Payload payload)
        {
            Id = id;
            this.payload = payload;
        }
    }

    [System.Serializable]
    abstract class Payload {}

    [System.Serializable]
    class PayloadExample: Payload
    {
        public string Id;
        public int Value;
    }

    [ContextMenu("Test Save")]
    void TestSave()
    {
        var payload = new PayloadExample() { Id = "hello", Value = 42 };
        var save = new Save("goodbye", payload) { Test = Vector3Int.back };

        Debug.Log(JsonUtility.ToJson(save));    
    }
}
