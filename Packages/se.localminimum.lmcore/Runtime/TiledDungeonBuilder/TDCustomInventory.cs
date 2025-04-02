using LMCore.Crawler;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Integration;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    [RequireComponent(typeof(TDContainer))]
    public class TDCustomInventory : MonoBehaviour
    {
        TDContainer _container;
        TDContainer container
        {
            get
            {
                if (_container == null)
                {
                    _container = GetComponent<TDContainer>();
                }
                return _container;
            }
        }

        [SerializeField]
        string UniqueId = System.Guid.NewGuid().ToString();

        [SerializeField]
        int Capacity = 7;

        [SerializeField]
        List<TDContainer.SlotContent> Content = new List<TDContainer.SlotContent>();

        [SerializeField]
        Direction Anchor;

        [SerializeField]
        Direction FacingDirection;

        [SerializeField]
        bool blockingPassage;

        [SerializeField]
        TDEnumInteraction interaction;

        [SerializeField]
        string key;

        [SerializeField]
        bool consumesKey;

        [ContextMenu("Sync")]
        private void Sync()
        {
            container.Configure(
                UniqueId,
                Capacity,
                Content,
                Anchor,
                FacingDirection,
                blockingPassage,
                interaction,
                key,
                consumesKey
                );
        }
    }
}
