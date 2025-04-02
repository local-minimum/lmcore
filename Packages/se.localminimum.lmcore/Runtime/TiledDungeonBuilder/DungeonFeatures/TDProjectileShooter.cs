using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.UI;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDProjectileShooter : TDFeature
    {
        [SerializeField]
        Transform overrideShotOrigin;

        Transform shotOrigin => overrideShotOrigin == null ? transform : overrideShotOrigin;

        [SerializeField, HideInInspector]
        Direction ShootDirection;

        [SerializeField]
        Projectile Prefab;

        [SerializeField]
        float interval = 3f;

        [SerializeField, Tooltip("Voids interval, projectiles must be emitted with Shoot()")]
        bool manual;

        List<Projectile> projectiles = new List<Projectile>();

        float _LastShotTime;
        float LastShotTime
        {
            set
            {
                _LastShotTime = value;
            }
        }
        float TimeSinceLastShot =>
            Paused ? _PauseStart - _LastShotTime : Time.timeSinceLevelLoad - _LastShotTime;

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
            projectile.name = $"Projectile #{projectiles.Count} of {name})";
            projectile.transform.SetParent(Node.Dungeon.LevelParent, true);
            projectile.OnRecycle += Projectile_OnRecycle;
            Debug.Log($"Projectile setup: {projectile}");
        }

        private void Projectile_OnRecycle(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            AbsMenu.OnShowMenu += HandleMenusPausing;
            AbsMenu.OnExitMenu += HandleMenusPausing;

        }

        private void OnDisable()
        {
            AbsMenu.OnShowMenu -= HandleMenusPausing;
            AbsMenu.OnExitMenu -= HandleMenusPausing;
        }

        bool _Paused;
        float _PauseStart;
        bool Paused
        {
            get => _Paused;
            set
            {
                if (!Paused && value)
                {
                    _PauseStart = Time.timeSinceLevelLoad;
                    foreach (var projectile in projectiles)
                    {
                        projectile.Paused = true;
                    }
                }
                else if (Paused && !value)
                {
                    _LastShotTime += Time.timeSinceLevelLoad - _PauseStart;
                    foreach (var projectile in projectiles)
                    {
                        projectile.Paused = false;
                    }
                }
                _Paused = value;
            }
        }
        private void HandleMenusPausing(AbsMenu menu)
        {
            Paused = AbsMenu.PausingGameplay;
        }

        private void Update()
        {
            if (manual || Paused) return;

            if (TimeSinceLastShot < interval)
            {
                return;
            }

            Shoot();
        }

        public Projectile Shoot()
        {
            var projectile = projectiles.GetInactiveOrInstantiate(Prefab, SetupProjectile);

            if (projectile == null)
            {
                Debug.LogWarning(PrefixLogMessage("Can't spawn projectile even if it is time"));
                return null;
            }

            projectile.Shoot(shotOrigin, ShootDirection);

            LastShotTime = Time.timeSinceLevelLoad;

            return projectile;
        }

        public Projectile ShootAt(Vector3 target)
        {
            var projectile = projectiles.GetInactiveOrInstantiate(Prefab, SetupProjectile);

            if (projectile == null)
            {
                Debug.LogWarning(PrefixLogMessage("Can't spawn projectile even if it is time"));
                return null;
            }

            var direction = (target - shotOrigin.position).normalized;

            projectile.Shoot(shotOrigin, direction);

            LastShotTime = Time.timeSinceLevelLoad;

            return projectile;
        }
    }
}
