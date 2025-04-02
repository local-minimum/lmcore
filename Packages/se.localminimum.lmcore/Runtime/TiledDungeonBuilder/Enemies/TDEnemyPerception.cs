using LMCore.Crawler;
using LMCore.EntitySM.State.Critera;
using LMCore.EntitySM.Trait;
using LMCore.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public delegate void DetectPlayerEvent(GridEntity player);

    public class TDEnemyPerception : AbsCustomPassingCriteria
    {
        public static event DetectPlayerEvent OnDetection;
        public event DetectPlayerEvent OnDetectPlayer;

        [SerializeField, Header("Detection")]
        int maxDistance = 5;

        [SerializeField]
        bool requireLOS = false;

        [SerializeField]
        LayerMask LOSFilter;

        [SerializeField]
        float losMaxAngle = 60f;

        [SerializeField]
        bool requirePath = false;

        [SerializeField]
        int maxPathLength = 7;

        [SerializeField, Header("Effects")]
        TraitType effectTrait;

        [SerializeField]
        float effectMagnitude;

        [SerializeField, Tooltip("If empty it uses current object transform as source")]
        Transform _rayCaster;
        Transform rayCaster => _rayCaster == null ? transform : _rayCaster;

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
            $"Perception {name} {(Passing ? "PASSING" : "NOT PASSING")} LOS({requireLOS}) ({effectTrait} {effectMagnitude}): {message}";

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
        public GridEntity Target
        {
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
            bool inDungeon = Enemy == null || Enemy.Dungeon == null;
            if (inDungeon) return;

            Gizmos.color = Passing ? Color.red : Color.green;
            if (requireLOS && maxDistance > 0)
            {
                foreach (var player in Players)
                {
                    var direction = player.LookTarget.position - rayCaster.position;
                    if (Physics.Raycast(rayCaster.position, direction, out var hitInfo, maxDistance * Enemy.Dungeon.GridSize, LOSFilter))
                    {
                        Gizmos.DrawLine(rayCaster.position, hitInfo.point);
                    }
                }
            }
            else if (maxDistance > 0)
            {
                {
                    Gizmos.DrawWireSphere(rayCaster.position, maxDistance * Enemy.Dungeon.GridSize);
                }
            }
        }

        private bool _passing;
        public override bool Passing => _passing;

        private bool CheckLOS(GridEntity target)
        {
            Vector3 lookDirection = Enemy.Entity.LookDirection.AsLookVector3D();
            var direction = target.LookTarget.position - rayCaster.position;
            var angle = Vector3.Angle(lookDirection, direction);

            if (angle < losMaxAngle && Physics.Raycast(rayCaster.position, direction, out var hitInfo, maxDistance * Enemy.Dungeon.GridSize, LOSFilter))
            {
                if (hitInfo.transform.GetComponentInParent<GridEntity>() == target)
                {
                    return !requirePath || Enemy.Dungeon.ClosestPath(Enemy.Entity, Enemy.Entity.Coordinates, target.Coordinates, maxPathLength, out var _, refuseSafeZones: true);
                }
                Debug.Log(PrefixLogMessage($"LOS hit {hitInfo.transform.name}, not {target.name}"));
            }
            return false;
        }

        private bool CheckArea(GridEntity target) =>
            Enemy.Dungeon.ClosestPath(Enemy.Entity, Enemy.Entity.Coordinates, target.Coordinates, maxDistance, out var _, refuseSafeZones: true);

        [ContextMenu("Check")]
        void Info()
        {
            var dungeon = Enemy.Dungeon;
            if (dungeon == null) return;

            var player = dungeon.Player;

            if (requireLOS)
            {
                if (CheckLOS(player))
                {
                    Debug.Log(PrefixLogMessage("I spot the player with LOS"));
                }
                else
                {
                    Debug.Log(PrefixLogMessage("No LOS to player"));
                }
            }
            else
            {
                if (CheckArea(player))
                {
                    Debug.Log(PrefixLogMessage("I hear the player in the area"));
                }
                else
                {
                    Debug.Log(PrefixLogMessage("No player in the area"));
                }
            }
        }

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
                // Debug.Log(PrefixLogMessage("Player too far"));
                _passing = false;
                return;
            }

            if (requireLOS)
            {
                if (CheckLOS(entity))
                {
                    InvokeEffect(entity);
                    return;
                }

                // Debug.Log(PrefixLogMessage($"LOS hit nothing for {maxDistance}"));
            }
            else if (CheckArea(entity))
            {
                InvokeEffect(entity);
                return;
            }
            else
            {
                // Debug.Log(PrefixLogMessage("No path to player, can't hear them"));
            }
            _passing = false;
        }

        void InvokeEffect(GridEntity player)
        {
            // Must set passing before updating activity or activity could assume we are not 
            // detecting!
            _passing = true;

            // Debug.Log(PrefixLogMessage($"{player.name} detected"));
            Target = player;
            Enemy.Personality.AdjustState(effectTrait, effectMagnitude);
            Enemy.UpdateActivity();

            OnDetectPlayer?.Invoke(player);
            OnDetection?.Invoke(player);
        }
    }
}
