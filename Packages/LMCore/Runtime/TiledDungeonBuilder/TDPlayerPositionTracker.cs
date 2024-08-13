using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDPlayerPositionTracker : MonoBehaviour, IOnLoadSave
    {

        public int OnLoadPriority => 102;

        TiledDungeon _Dungeon;
        TiledDungeon Dungeon
        {
            get { 
                if (_Dungeon == null)
                {
                    _Dungeon = GetComponentInParent<TiledDungeon>();
                }

                return _Dungeon; 
            }
        }

        private void OnEnable()
        {
            TDNode.OnNewOccupant += TDNode_OnNewOccupant;
        }

        private void OnDisable()
        {
            TDNode.OnNewOccupant -= TDNode_OnNewOccupant;
        }

        private void TDNode_OnNewOccupant(TDNode node, GridEntity entity)
        {
            if (entity.EntityType == GridEntityType.PlayerCharacter && node.Dungeon == Dungeon)
            {
                if (!playerPositions.Contains(node.Coordinates))
                {
                    playerPositions.Add(node.Coordinates);
                }
            }
        }

        List<Vector3Int> playerPositions = new List<Vector3Int>();

        void OnLoadGameSave(GameSave save)
        {
            var lvl = save.levels.GetValueOrDefault(Dungeon.MapName);

            playerPositions.Clear();

            if (lvl == null)
            {
                return;
            }

            playerPositions.AddRange(lvl.playerPositions);
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }

        public IEnumerable<Vector3Int> Save() => playerPositions;
    }
}
