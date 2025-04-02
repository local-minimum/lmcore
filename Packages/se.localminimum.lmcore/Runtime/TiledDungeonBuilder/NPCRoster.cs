using LMCore.AbstractClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class NPCRoster : Singleton<NPCRoster, NPCRoster>
    {
        [Serializable]
        class NPCInstruction
        {
            public string Identifier;
            public GameObject Prefab;
        }

        [SerializeField]
        List<NPCInstruction> npcPrefabs = new List<NPCInstruction>();

        string PrefixLogMessage(string message) => $"NPCRoster: {message}";

        public GameObject GetInstance(string identifier, Transform parent)
        {
            var instruction = npcPrefabs.FirstOrDefault(x => x.Identifier == identifier);
            if (instruction == null)
            {
                Debug.LogError(PrefixLogMessage($"Entity named '{identifier}' not known"));
                return null;
            }

            return Instantiate(instruction.Prefab, parent);
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
