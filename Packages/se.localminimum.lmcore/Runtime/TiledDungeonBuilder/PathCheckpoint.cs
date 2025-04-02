using LMCore.Crawler;
using UnityEngine;


#if UNITY_EDITOR
#endif

namespace LMCore.TiledDungeon
{
    [System.Serializable]
    public struct PathCheckpoint
    {
        public Vector3Int Coordinates;
        public Direction Anchor;

        public bool IsHere(GridEntity entity) =>
            entity.Coordinates == Coordinates && entity.AnchorDirection == Anchor;

        public override string ToString() =>
            $"<{Coordinates} {Anchor}>";
    }
}
