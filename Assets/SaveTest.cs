using LMCore.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveTest : MonoBehaviour
{
    [SerializeField]
    SerializableDictionary<string, int> test = new SerializableDictionary<string, int>();
}
