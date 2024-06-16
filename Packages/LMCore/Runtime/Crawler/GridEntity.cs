using UnityEngine;
using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace LMCore.Crawler
{
    public class GridEntity : MonoBehaviour
    {
        public UnityEvent<bool> OnFall;
        public UnityEvent OnLand;

        public TransportationMode transportationMode;

        private bool falling;
        public bool Falling
        {
            get => falling;
            set {
                if (value != falling)
                {
                    if (value) {
                        OnFall?.Invoke(true);
                    } else
                    {
                        OnLand?.Invoke();
                    }
                }
                falling = value;
            }
        }

        #region Movers
        List<IEntityMover> movers;
        public IEnumerable<IEntityMover> Movers { 
            get { 
                if (movers == null)
                {
                    movers = GetComponents<IEntityMover>().ToList();
                }
                return movers; 
            } 
        }

        public IEntityMover ActiveMover => movers.Where(m => m.Enabled).FirstOrDefault();
        #endregion Movers

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

        [SerializeField]
        Direction StartLookDirection;

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
