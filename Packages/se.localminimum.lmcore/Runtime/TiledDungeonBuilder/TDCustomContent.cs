using LMCore.AbstractClasses;
using LMCore.TiledImporter;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDCustomContent : Singleton<TDCustomContent, TDCustomContent>
    {
        [SerializeField]
        List<string> contentIds = new List<string>();

        [SerializeField]
        List<GameObject> content = new List<GameObject>();

        public void AddCustom(TDNode node, string contentId, TiledCustomProperties properties)
        {
            if (node == null) return;

            if (string.IsNullOrEmpty(contentId))
            {
                Debug.LogWarning($"Cannot add content without id on {node}");
                return;
            }

            var idx = contentIds.IndexOf(contentId);
            if (idx == -1 || idx >= content.Count)
            {
                Debug.LogWarning($"No content matches '{contentId}' for {node}");
                return;
            }

            var prefab = content[idx];
            var instance = Instantiate(prefab, node.transform);

            var cont = instance.GetComponent<ITDCustom>();
            if (cont != null)
            {
                cont.Configure(node, properties);
            }
            else
            {
                Debug.LogWarning($"There was no custom content on {instance} so nothing got configured for {node} / {contentId}");
            }
        }
    }
}
