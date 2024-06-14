using LMCore.Extensions;
using LMCore.IO;
using UnityEngine;

namespace LMCore.Crawler
{
    public class PhysicsController : MonoBehaviour, GridEntityController
    {
        [SerializeField]
        LayerMask WallMask;

        public bool CanMoveTo(Movement movement, int length)
        {
            var source = entity.Position.ToPosition(scale: length) + Vector3.up * length * 0.5f;
            var direction = entity.LookDirection.RelativeTranslation(movement).AsLookVector3D();

            if (Physics.Raycast(source, direction, out var hitInfo, length, WallMask, QueryTriggerInteraction.Ignore))
            {
                Debug.Log($"Can't move because hit wall {hitInfo.collider.name} at {hitInfo.point}");
                return false;
            }

            return true;
        }


        GridEntity _entity;
        private GridEntity entity
        {
            get
            {
                if (_entity == null)
                {
                    _entity = GetComponent<GridEntity>();
                }
                return _entity;
            }
        }
    }
}
