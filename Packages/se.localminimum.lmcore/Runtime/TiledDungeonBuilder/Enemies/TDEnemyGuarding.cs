using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyGuarding : TDAbsEnemyBehaviour, IOnLoadSave
    {
        [SerializeField]
        float minGuardTickTime = 2f;

        [SerializeField]
        float maxGuardTickTime = 4f;

        [SerializeField]
        float movementDuration = 1f;

        [SerializeField]
        int minSightDirection = 1;
        [SerializeField]
        float maxSightThreshold = 0.4f;

        string PrefixLogMessage(string message) =>
            $"Guarding {Enemy.name} ({lookDirection}): {message}";

        #region SaveState
        List<Direction> directions = new List<Direction>();
        Direction lookDirection = Direction.None;
        float nextTick;
        #endregion

        public void InitGuarding(Direction forcedDirection = Direction.None)
        {
            if (forcedDirection != Direction.None)
            {
                directions = new List<Direction> { forcedDirection };
                lookDirection = forcedDirection;
                nextTick = Time.timeSinceLevelLoad + Random.Range(minGuardTickTime, maxGuardTickTime);
                return;
            }

            int maxSight = 0;
            var candidates = new List<KeyValuePair<Direction, int>>();
            foreach (var direction in DirectionExtensions.AllDirections.Where(d => !d.IsParallell(Enemy.Entity.Down)))
            {
                var sight = 0;
                var coords = Enemy.Entity.Coordinates;
                var node = Dungeon[coords];

                while (true)
                {
                    if (node == null) break;

                    if (node._sides.Has(direction) || node.Obstructed) break;

                    coords = direction.Translate(coords);
                    sight++;
                    if (Dungeon.HasNodeAt(coords))
                    {
                        node = Dungeon[coords];
                    }
                    else
                    {
                        break;
                    }
                }

                maxSight = Mathf.Max(maxSight, sight);
                candidates.Add(new KeyValuePair<Direction, int>(direction, sight));
            }

            directions = candidates
                .Where(kvp => kvp.Value > minSightDirection && kvp.Value > maxSight * maxSightThreshold)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            lookDirection = directions.FirstOrDefault();
            nextTick = Time.timeSinceLevelLoad + Random.Range(minGuardTickTime, maxGuardTickTime);
        }

        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            foreach (var perception in GetComponentsInParent<TDEnemyPerception>(true))
            {
                perception.OnDetectPlayer += Perception_OnDetectPlayer;
                if (perception.Passing)
                {
                    Enemy.UpdateActivity();
                }
            }

            Enemy.OnResumeEnemy += Enemy_OnResumeEnemy;
        }

        private void OnDisable()
        {
            foreach (var perception in GetComponentsInParent<TDEnemyPerception>(true))
            {
                perception.OnDetectPlayer -= Perception_OnDetectPlayer;
            }
            lookDirection = Direction.None;

            Enemy.OnResumeEnemy -= Enemy_OnResumeEnemy;
        }

        private void Enemy_OnResumeEnemy(float pauseDuration)
        {
            nextTick += pauseDuration;
        }

        private void Perception_OnDetectPlayer(GridEntity player)
        {
            Enemy.UpdateActivity();
        }

        bool wasRotating;

        private void Update()
        {
            if (Paused || lookDirection == Direction.None) return;

            var entity = Enemy.Entity;
            if (entity.LookDirection == lookDirection)
            {
                if (wasRotating)
                {
                    if (Enemy.animator != null && !string.IsNullOrEmpty(noMovementAnimTrigger))
                    {
                        Enemy.animator.SetTrigger(noMovementAnimTrigger);
                    }
                }
                if (Time.timeSinceLevelLoad < nextTick) return;

                Enemy.MayTaxStay = true;

                var checkUpdateActivity = lookDirection != Direction.None;
                lookDirection = directions.GetRandomElement();
                if (checkUpdateActivity || lookDirection == Direction.None)
                {
                    Enemy.UpdateActivity();
                    if (!enabled) return;
                }
            }

            if (entity.LookDirection != lookDirection && lookDirection != Direction.None)
            {
                var movement = lookDirection.AsPlanarRotation(entity.LookDirection, entity.Down);
                if (movement.IsRotation())
                {
                    // We are turning
                    // Debug.Log(PrefixLogMessage(movement.ToString()));
                    entity.MovementInterpreter.InvokeMovement(movement, movementDuration, false);
                    if (Enemy.animator != null && !string.IsNullOrEmpty(rotateAnimTrigger))
                    {
                        Enemy.animator.SetTrigger(rotateAnimTrigger);
                    }
                    wasRotating = true;
                }
                else
                {
                    Debug.LogError(PrefixLogMessage($"We ave no movement based on needed direction {lookDirection} while looking {entity.LookDirection} wanting {lookDirection}"));
                }
            }

            nextTick = Time.timeSinceLevelLoad + Random.Range(minGuardTickTime, maxGuardTickTime);
        }

        #region Save/Load
        public EnemyGuardingSave Save() =>
            lookDirection != Direction.None ?
                new EnemyGuardingSave()
                {
                    directions = new List<Direction>(directions),
                    lookDirection = lookDirection,
                    timeToNextTick = nextTick - Time.timeSinceLevelLoad,
                } : null;

        public int OnLoadPriority => 500;

        private void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }

            var lvl = Dungeon.MapName;

            var lvlSave = save.levels[lvl];
            if (lvlSave == null)
            {
                return;
            }

            var guardingSave = lvlSave.enemies.FirstOrDefault(s => s.Id == Enemy.Id && s.Alive)?.guarding;
            if (guardingSave != null)
            {
                directions = new List<Direction>(guardingSave.directions);
                lookDirection = guardingSave.lookDirection;
                nextTick = Time.timeSinceLevelLoad + guardingSave.timeToNextTick;
            }
            else
            {
                lookDirection = Direction.None;
                enabled = false;
            }
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
        #endregion
    }
}
