using UnityEngine;

namespace LMCore.Crawler
{
    public class PositionSentinel : MonoBehaviour
    {
        [Tooltip("Use None for center of cube face")]
        public Direction Direction;

        public TransportationMode Blocked;
        public Vector3 Position => transform.position;
    }
}
