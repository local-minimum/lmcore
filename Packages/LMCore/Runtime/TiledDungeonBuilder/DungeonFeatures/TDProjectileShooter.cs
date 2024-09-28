using LMCore.Crawler;
using LMCore.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDProjectileShooter : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        Direction ShootDirection;

        [SerializeField]
        Projectile Prefab;

        [SerializeField]
        float interval = 3f;

        List<Projectile> projectiles = new List<Projectile>();

        float lastShot;

        TDNode _node;
        TDNode Node {
            get {
                if (_node == null)
                {
                    _node = GetComponentInParent<TDNode>();
                }
                return _node;
            } 
        }

        public void Configure(Direction wallSide)
        {
            ShootDirection = wallSide.Inverse();
        }

        public override string ToString() =>
            $"Shooter {name} @ {Node.Coordinates}";

        private string PrefixLogMessage(string message) =>
            $"{this}: {message}";

        private void SetupProjectile(Projectile projectile)
        {
            projectile.OnRecycle += Projectile_OnRecycle;
        }

        private void Projectile_OnRecycle(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Time.timeSinceLevelLoad - lastShot < interval)
            {
                return;
            }

            var projectile = projectiles.GetInactiveOrInstantiate(Prefab, SetupProjectile);

            if (projectile == null) {
                Debug.LogWarning(PrefixLogMessage("Can't spawn projectile even if it is time"));
                return;
            }

            projectile.FlightDirection = ShootDirection;
            projectile.transform.position = transform.position;

            projectile.Shoot();

            lastShot = Time.timeSinceLevelLoad;
        }
    }
}
