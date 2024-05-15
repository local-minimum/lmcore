using LMCore.AbstractClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LMCore.IO
{
    public class SaveStorage : Singleton<SaveStorage> 
    {
        private string IdPattern = "{id}";

        [Min(1), Tooltip("Number of saves the game allows, minimum 1")]
        public int maxSaves = 3;

        [Tooltip("Use '{id}' for injection of identifier for game with multiple saves")]
        public string SavePattern = "GameSave_{id}.json";

        private string FileName(int id)
        {
            if (SavePattern.Contains(IdPattern)) return SavePattern.Replace(IdPattern, id.ToString());

            if (id > 0)
            {
                Debug.LogWarning("Save pattern only allows for a single save, id disregarded");
            }
            return SavePattern;
        } 

#if UNITY_WEBGL
        private string FilePath(int id) =>
            Path.Combine(Path.GetDirectoryName(Application.persistentDataPath), Application.productName, FileName(id));
#else
        private string FilePath(int id) =>
            Path.Combine(Application.persistentDataPath, FileName(id));
#endif

        public T Load<T>(int id)
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(FilePath(id)));
        }

        public bool Save<T>(int id, T obj)
        {
            var path = FilePath(id);

            try
            {
#if UNITY_WEBGL
                Directory.CreateDirectory(Path.GetDirectoryName(path));
#endif
                File.WriteAllText(path, JsonUtility.ToJson(obj));

                return true;
            } catch {
                Debug.LogError($"Failed to save to {path}");
                return false; 
            }
        }

        public bool HasSave(int id) => File.Exists(FilePath(id));

        public struct SaveInfo
        {
            public int id;
            public DateTime saveTime;

        }

        public SaveInfo Info(int id)
        {
            if (!HasSave(id)) throw new ArgumentException($"Save {id} does not exist");

            return new SaveInfo { id = id, saveTime = File.GetLastWriteTime(FilePath(id)) };
        }

        public IEnumerable<SaveInfo> List()
        {
            for (int i = 0; i < maxSaves; i++)
            {
                if (!HasSave(i)) continue;

                yield return Info(i);
            }
        }

        public int Count()
        {
            int n = 0;
            for (int i = 0; i<maxSaves; i++)
            {
                if (HasSave(i)) n++;
            }

            return n;
        }

        public bool FirstFreeSave(out int id)
        {
            for (int i = 0; i < maxSaves; i++)
            {
                if (!HasSave(i))
                {
                    id = i;
                    return true;
                }
            }

            id = -1;
            return false;
        }

        public int FirstFreeOrOldestSave()
        {
            if (maxSaves <= 0) throw new ArgumentException("Max saves must be a positive number");

            if (FirstFreeSave(out int id))
            {
                return id;
            }
            
            return List().OrderBy(i => i.saveTime).First().id;
        }

        public bool MostRecentSave(out int id)
        {
            var saves = List().ToList();
            if (saves.Count > 0)
            {
                id = saves.OrderByDescending(i => i.saveTime).First().id;
                return true;
            }

            id = -1;
            return false;
        }

        [ContextMenu("Log Status")]
        public void Status()
        {
            Debug.Log($"SaveStorage: {Count()}/{maxSaves} used");

            if (MostRecentSave(out int id))
            {
                var info = Info(id);
                Debug.Log($"SaveStorage: Most recent save {info.saveTime} ({Mathf.RoundToInt((float) (DateTime.Now - info.saveTime).TotalMinutes)} minutes ago)");
            }
        }
    }
}
