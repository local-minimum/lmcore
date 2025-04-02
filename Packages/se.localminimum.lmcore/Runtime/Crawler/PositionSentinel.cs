using UnityEngine;

namespace LMCore.Crawler
{
    public class PositionSentinel : MonoBehaviour
    {
        [Tooltip("Use None for center of cube face")]
        public Direction Direction;

        /// <summary>
        /// Modes of transportation that are bocked from passing the sentinel
        /// </summary>
        public TransportationMode Blocked;
        public Vector3 Position => transform.position;

        public override string ToString() =>
            $"Sentinal {name}({Direction}, {Blocked})";
    }
}
