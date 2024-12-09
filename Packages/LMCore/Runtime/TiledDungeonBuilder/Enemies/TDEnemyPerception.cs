using LMCore.Crawler;
using LMCore.EntitySM.State.Critera;
using LMCore.EntitySM.Trait;
using LMCore.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyPerception : AbsCustomPassingCriteria 
    {
        [SerializeField]
        int maxDistance = 5;

        [SerializeField]
        bool requireLOS = false;

        [SerializeField]
        LayerMask LOSFilter;

        [SerializeField]
        float losMaxAngle = 60f;

        [SerializeField]
        TraitType effectTrait;

        [SerializeField]
        float effectMagnitude;

        [SerializeField]
        Transform _rayCaster;
        Transform rayCaster => _rayCaster ?? transform;

        TDEnemy _enemy;
        TDEnemy Enemy
        {
            get
            {
                if (_enemy == null)
                {
                    _enemy = GetComponentInParent<TDEnemy>();
                }
                return _enemy;
            }
        }

        private string PrefixLogMessage(string message) =>
            $"Perception {name} LOS({requireLOS}) ({effectTrait} {effectMagnitude}): {message}";

        private void OnEnable()
        {
            GridEntity.OnMove += CheckDetection;
        }

        private void OnDisable()
        {
            GridEntity.OnMove -= CheckDetection;
        }

        HashSet<GridEntity> Players = new HashSet<GridEntity>();

        private GridEntity _target;
        public GridEntity Target { 
            get
            {
                if (_target == null)
                {
                    _target = Enemy.Dungeon.Player;
                }
                return _target;
            }

            private set
            {
                _target = value;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Passing ? Color.red : Color.green;
            if (requireLOS)
            {
                foreach (var player in Players)
                {
                    var direction = player.LookTarget.position - rayCaster.position;
                    if (Physics.Raycast(rayCaster.position, direction, out var hitInfo, maxDistance * Enemy.Dungeon.GridSize, LOSFilter))
                    {
                        Gizmos.DrawLine(rayCaster.position, hitInfo.point);
                    }
                }
            } else
            {
                Gizmos.DrawWireSphere(rayCaster.position, maxDistance * Enemy.Dungeon.GridSize);
            }
        }

        private bool _passing;
        public override bool Passing => _passing;

        private void CheckDetection(GridEntity entity)
        {
            if (entity == Enemy.Entity)
            {
                foreach (GridEntity player in Players)
                {
                    CheckDetection(player);
                }
                return;
            }

            if (entity.EntityType != GridEntityType.PlayerCharacter) return;
            Players.Add(entity);

            var distance = entity.Coordinates.ManhattanDistance(Enemy.Entity.Coordinates);
            if (distance > maxDistance)
            {
                Debug.Log(PrefixLogMessage("Player too far"));
                _passing = false;
                return;
            }
            
            if (requireLOS)
            {
                Vector3 lookDirection = Enemy.Entity.LookDirection.AsLookVector3D();
                var direction = entity.LookTarget.position - rayCaster.position;
                var angle = Vector3.Angle(lookDirection, direction);

                if (angle < losMaxAngle && Physics.Raycast(rayCaster.position, direction, out var hitInfo, maxDistance * Enemy.Dungeon.GridSize, LOSFilter)) {
                    if (hitInfo.transform.GetComponentInParent<GridEntity>() == entity)
                    {
                        InvokeEffect(entity);
                        return;
                    }
                    Debug.Log(PrefixLogMessage($"LOS hit {hitInfo.transform.name}, not {entity.name}"));
                } else
                {
                    Debug.Log(PrefixLogMessage($"LOS hit nothing for {maxDistance}"));
                }
            } else
            {
                if (Enemy.Dungeon.ClosestPath(Enemy.Entity, Enemy.Entity.Coordinates, entity.Coordinates, maxDistance, out var path)) {
                    if (path.Count <= maxDistance)
                    {
                        InvokeEffect(entity);
                        return;
                    }
                    Debug.Log(PrefixLogMessage($"Closest Path is too far, {path.Count} > {maxDistance}"));
                } else
                {
                    Debug.Log(PrefixLogMessage("To path to player, can't hear them"));
                }
            }
            _passing = false;
        }

        void InvokeEffect(GridEntity entity)
        {
            Debug.Log(PrefixLogMessage($"{entity.name} detected"));
            Target = entity;
            Enemy.Personality.AdjustState(effectTrait, effectMagnitude);
            Enemy.UpdateActivity();
            _passing = true;
        }
    }
}
