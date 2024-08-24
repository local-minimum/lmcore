using LMCore.Crawler;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class PositionSentinel : MonoBehaviour
    {
        [Tooltip("Use None for center of cube face")]
        public Direction Direction;
        public Vector3 Position => transform.position;
    }
}
