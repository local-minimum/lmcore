using LMCore.Crawler;
using LMCore.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDProjectileShooter : TDFeature 
    {
        [SerializeField, HideInInspector]
        Direction ShootDirection;

        [SerializeField]
        Projectile Prefab;

        [SerializeField]
        float interval = 3f;

        List<Projectile> projectiles = new List<Projectile>();

        float lastShot;

        public void Configure(Direction wallSide)
        {
            ShootDirection = wallSide.Inverse();
        }

        public override string ToString() =>
            $"Shooter {name} @ {Node.Coordinates}";

        private string PrefixLogMessage(string message) =>
            $"{this}: {message}";

        private void Start()
        {
            InitStartCoordinates();
        }

        private void SetupProjectile(Projectile projectile)
        {
            projectile.transform.SetParent(Node.Dungeon.LevelParent); 
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
