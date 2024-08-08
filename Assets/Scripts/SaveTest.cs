using LMCore.IO;
using LMCore.TiledDungeon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveTest : MonoBehaviour
{
    [ContextMenu("Test Save")]
    void TestSave()
    {
        var dict = new SerializableDictionary<string, StackedItemInfo>
        {
            { "test", new StackedItemInfo("id", "origin", 42) }
        };

        Debug.Log(dict.ToString());
        Debug.Log(JsonUtility.ToJson(dict));    
    }
}
