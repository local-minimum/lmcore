using LMCore.Crawler;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class PositionSentinel : MonoBehaviour
    {
        public Direction Direction;
        public Vector3 Position => transform.position;
    }
}
