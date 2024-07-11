using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    public interface IDungeon 
    {
        public IDungeonNode this[Vector3Int coordinates] { get; }
        public bool HasNodeAt(Vector3Int coordinates);

        public List<IDungeonNode> FindTeleportersById(int id);
    }
}
