using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LMCore.IO
{
    public class DiskStorageProvider<TSave> : AbsStorageProvider<TSave> where TSave : new()
    {
        [SerializeField]
        bool Pretty;

        private string SaveSlotIdPattern = "{id}";
        private string PrefixLogMessage(string message) => $"DiskStorageProvider: {message}";

        int MaxSaves => SaveSystem<TSave>.instance?.maxSaves ?? 0;

        [Tooltip("Use '{id}' for injection of identifier for game with multiple saves")]
        public string SavePattern = "GameSave_{id}.json";

        private string FileName(int id)
        {
            if (SavePattern.Contains(SaveSlotIdPattern)) return SavePattern.Replace(SaveSlotIdPattern, id.ToString());

            if (id > 0)
            {
                Debug.LogWarning(PrefixLogMessage("Save pattern only allows for a single save, id disregarded"));
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

        public override bool HasSave(int id) => File.Exists(FilePath(id));

        public override bool Load(int id, out TSave value)
        {
            try
            {
                value = JsonUtility.FromJson<TSave>(File.ReadAllText(FilePath(id)));
            } catch
            {
                Debug.LogError(PrefixLogMessage($"Failed to load save {id}"));
                value = default(TSave);
                return false;
            }

            return true;
        }

        private IEnumerator<WaitForSeconds> AsyncLoad(int id, Action<TSave> OnLoad, Action OnLoadFail)
        {
            if (Load(id, out TSave value))
            {
                OnLoad(value);
            } else
            {
                OnLoadFail();
            }
            yield break;
        }

        public override void Load(int id, Action<TSave> OnLoad, Action OnLoadFail) =>
            StartCoroutine(AsyncLoad(id, OnLoad, OnLoadFail));

        public override bool Save(int id, TSave value)
        {
            var path = FilePath(id);

            try
            {
#if UNITY_WEBGL
                Directory.CreateDirectory(Path.GetDirectoryName(path));
#endif
                File.WriteAllText(path, JsonUtility.ToJson(value, Pretty));

                Debug.Log(PrefixLogMessage($"Saved slot {id} at {path}"));
                return true;
            } catch {
                Debug.LogError(PrefixLogMessage($"Failed to save slot {id} to {path}"));
                return false; 
            }
        }

        IEnumerator<WaitForSeconds> AsyncSave(int id, TSave value, Action OnSave, Action OnSaveFail)
        {
            if (Save(id, value))
            {
                OnSave();
            } else
            {
                OnSaveFail();
            }
            yield break;
        }

        public override void Save(int id, TSave value, Action OnSave, Action OnSaveFail) =>
            StartCoroutine(AsyncSave(id, value, OnSave, OnSaveFail));

        public override bool Info(int id, out SaveInfo info)
        {
            if (!HasSave(id))
            {
                Debug.LogWarning(PrefixLogMessage($"Save {id} does not exist"));
                info = SaveInfo.Nothing;
                return false;
            }

            info = new SaveInfo(id, File.GetLastWriteTime(FilePath(id)));
            return true;
        }

        public override IEnumerable<SaveInfo> List()
        {
            for (int i = 0; i < MaxSaves; i++)
            {
                if (!HasSave(i)) continue;

                if (Info(i, out SaveInfo info))
                {
                    yield return info;
                }
            }
        }

        public override int Count()
        {
            int n = 0;
            for (int i = 0; i<MaxSaves; i++)
            {
                if (HasSave(i)) n++;
            }

            return n;
        }

        public override bool FirstFreeSave(out int id)
        {
            for (int i = 0; i < MaxSaves; i++)
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

        public override int FirstFreeOrOldestSave()
        {
            if (MaxSaves <= 0) throw new ArgumentException("Max saves must be a positive number");

            if (FirstFreeSave(out int id))
            {
                return id;
            }
            
            return List().OrderBy(info => info.SaveTime).First().Id;
        }

        public override bool MostRecentSave(out int id)
        {
            var saves = List().ToList();
            if (saves.Count > 0)
            {
                id = saves.OrderByDescending(i => i.SaveTime).First().Id;
                return true;
            }

            id = -1;
            return false;
        }

        public override void LogStatus()
        {
            Debug.Log(PrefixLogMessage($"{Count()}/{MaxSaves} used"));

            if (MostRecentSave(out int id))
            {
                if (Info(id, out SaveInfo info))
                {
                    Debug.Log(PrefixLogMessage($"Most recent save {info.SaveTime} ({Mathf.RoundToInt((float)(DateTime.Now - info.SaveTime).TotalMinutes)} minutes ago)"));
                }
            }
        }

        public override bool Delete(int id)
        {
            try
            {
                File.Delete(FilePath(id));
            } catch (Exception e)
            {
                Debug.LogError(PrefixLogMessage(e.Message));
                return false;
            }

            return true;
        }
    }
}
