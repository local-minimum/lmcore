using UnityEngine;
using LMCore.Extensions;
using LMCore.IO;

namespace LMCore.Crawler
{
    public class GridEntity : MonoBehaviour
    {
        [SerializeField]
        Direction StartLookDirection;

        /// <summary>
        /// Using XZ Plane, returns position in 2D
        /// </summary>
        public Vector2Int Position2D {
            get => Position.To2DInXZPlane();
            set => Position = value.To3DFromXZPlane();
        }

        Vector3Int _Position;
        public Vector3Int Position
        {
            get => _Position;
            set => _Position = value;
        }
        public Direction LookDirection { get; set; }

        private void Start()
        {
            LookDirection = StartLookDirection;
            Sync();
        }

        public void Sync()
        {
            transform.position = Position.ToPosition();
            transform.rotation = LookDirection.AsQuaternion();
        }

        public void Translate(Movement movement)
        {
            Position = LookDirection.RelativeTranslation(movement).Translate(Position);
        }

        public void Rotate(Movement movement)
        {
            LookDirection = LookDirection.ApplyRotation(movement);
        }
    }
}
