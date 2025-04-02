using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LMCore.TiledImporter
{
    [ExecuteAlways]
    public class TiledLiveMapEdit : MonoBehaviour
    {
        [SerializeField]
        TiledMap map;

        public UnityEvent<TiledMap> OnMapUpdate;

        private void Awake()
        {
            Debug.Log($"Live editing of map {map.name}");
            TiledMapImporter.OnMapImported += TiledMapImporter_OnMapImported;
        }

        private void Destroy()
        {
            TiledMapImporter.OnMapImported += TiledMapImporter_OnMapImported;
        }

        private void TiledMapImporter_OnMapImported(string mapName)
        {
            if (map == null) return;

            if (map.Metadata.Name == mapName)
            {
                StartCoroutine(EmitUpdateMap());
            }
        }

        IEnumerator<WaitForSecondsRealtime> EmitUpdateMap()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            OnMapUpdate.Invoke(this.map);
        }
    }
}
