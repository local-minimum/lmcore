using LMCore.Extensions;
using LMCore.IO;
using UnityEngine;

namespace LMCore.Crawler
{
    public class PhysicsController : MonoBehaviour, GridEntityController
    {
        [SerializeField]
        LayerMask WallMask;

        [SerializeField, Range(0, 3)]
        float sphereCastRadius = 0.5f;

        public bool CanMoveTo(Movement movement, float length)
        {
            var source = Entity.Position.ToPosition(scale: length) + Vector3.up * length * 0.5f;
            var direction = Entity.LookDirection.RelativeTranslation3D(Entity.Anchor, movement).AsLookVector3D();

            if (Physics.SphereCast(source, sphereCastRadius, direction, out var hitInfo, length, WallMask, QueryTriggerInteraction.Ignore))
            {
                Debug.Log($"Can't move because hit wall {hitInfo.collider.name} at {hitInfo.point}");

                hitInfo.transform.GetComponentInParent<ICollisionHandler>()?.Collision(Entity);

                return false;
            }

            return true;
        }

        GridEntity _entity;
        private GridEntity Entity
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
