using LMCore.Crawler;
using LMCore.EntitySM.State;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    [RequireComponent(typeof(ActivityState))]
    public abstract class TDAbsEnemyBehaviour : MonoBehaviour
    {
        [SerializeField]
        protected string enterStateTrigger;
        [SerializeField]
        protected string exitStateTrigger;

        [SerializeField]
        protected string translateAnimTrigger;

        [SerializeField]
        protected string rotateAnimTrigger;

        [SerializeField]
        protected string noMovementAnimTrigger;


        TDEnemy _enemy;
        protected TDEnemy Enemy
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

        public bool Valid =>
            Enemy.CurrentActivityState == GetComponent<ActivityState>().State;

        public bool Paused => Enemy.Paused;

        TiledDungeon _dungeon;
        protected TiledDungeon Dungeon
        {
            get
            {
                if (_dungeon == null)
                {
                    _dungeon = GetComponentInParent<TiledDungeon>();
                }
                return _dungeon;
            }
        }

        public virtual void EnterBehaviour()
        {
            if (!string.IsNullOrEmpty(enterStateTrigger) && Enemy.animator != null)
            {

                Debug.Log($"Doing the enter state animation: {enterStateTrigger}");
                Enemy.animator.SetTrigger(enterStateTrigger);
            }
        }

        public virtual void ExitBehaviour()
        {
            if (!string.IsNullOrEmpty(exitStateTrigger) && Enemy.animator != null)
            {
                Enemy.animator.SetTrigger(exitStateTrigger);
            }
        }

        // Note that for some derived behaviours previous path is in save states!
        private List<PathTranslation> _previousPath;
        protected List<PathTranslation> previousPath
        {
            get
            {
                return _previousPath;
            }

            set
            {
                if (_previousPath == value)
                {
                    return;
                }

                _previousPath = value;

                if (_previousPath != null)
                {
                    pathHistory.Enqueue(_previousPath);
                    while (pathHistory.Count > maxPathHistory)
                    {
                        pathHistory.Dequeue();
                    }
                }
            }
        }
        int maxPathHistory = 6;
        Queue<List<PathTranslation>> pathHistory = new Queue<List<PathTranslation>>();
        IEnumerable<List<PathTranslation>> PathHistory => pathHistory;

        private void OnDrawGizmosSelected()
        {
            var dungeon = Dungeon;
            var entityNode = Enemy.Entity.Node;
            if (entityNode == null) return;

            var oldColorPathColor = Color.black;
            var currentPathColor = Color.yellow;
            float n = pathHistory.Count();
            float i = 0;

            foreach (var oldPath in PathHistory)
            {
                Gizmos.color = n == 1f ? currentPathColor : Color.Lerp(oldColorPathColor, currentPathColor, i / n);

                var first = oldPath.First();
                var node = dungeon[first.Checkpoint.Coordinates];

                if (node == null) continue;

                var start = node.GetEdge(first.Checkpoint.Anchor);
                foreach (var translation in oldPath.Skip(1))
                {
                    node = dungeon[translation.Checkpoint.Coordinates];
                    if (node == null)
                    {
                        continue;
                    }

                    var intermediary = node.GetEdge(translation.Checkpoint.Anchor, translation.TranslationHere.Inverse());
                    var finish = node.GetEdge(translation.Checkpoint.Anchor);

                    Gizmos.DrawLine(start, intermediary);
                    Gizmos.DrawLine(intermediary, finish);
                    Gizmos.DrawSphere(finish, 0.2f - (0.025f * (n - i)));

                    start = finish;
                }

                i++;
            }
        }

        public bool NextActionCollidesWithPlayer(List<PathTranslation> path, Direction lookDirection, bool strafe)
        {
            if (path == null || path.Count < 2) return false;

            // We don't care if we share anchor we care if we are on the same tile
            var playerIndex = path.FindIndex(t => t.Checkpoint.Coordinates == Dungeon.Player.Coordinates);
            if (playerIndex < 1) return false;
            // If we don't need to rotate, the next action would be colliding
            if (playerIndex == 1) return lookDirection == path[1].TranslationHere;

            // If we are not strafing and translation to next point is not our own look direction
            // then next action is to turn, and that is always safe
            // (Note that since we know playerIndex is larger than 0 we safely know there's at least 1 items in list)
            if (!strafe && path[1].TranslationHere != lookDirection)
            {
                // Our next action(s) are to rotate
                return false;
            }

            var translationsUntilPlayer = path
                .Skip(1)
                .Take(playerIndex)
                .Reverse()
                .SkipWhile(kvp => kvp.TranslationHere == Direction.Down)
                .Count();

            return translationsUntilPlayer == 1;
        }


        [SerializeField]
        AnimationCurve translationEasing;

        [SerializeField]
        AnimationCurve rotationEasing;

        AnimationCurve GetMovementEasing(Movement movement)
        {
            if (movement.IsTranslation())
            {
                if (translationEasing != null && translationEasing.length > 0)
                {
                    return translationEasing;
                }
                return null;
            }

            if (movement.IsRotation())
            {
                if (rotationEasing != null && rotationEasing.length > 0)
                {
                    return rotationEasing;
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// Cause relevant translation to approach a target
        /// </summary>
        /// <param name="translationDirection">Direction of next translation</param>
        /// <param name="target">Reference point, i.e. player, to assist in turning direction</param>
        /// <param name="movementDuration">Duration of translations</param>
        /// <param name="prefixLogMessage">Formatter or log messages</param>
        protected Movement InvokePathBasedMovement(
            Direction translationDirection,
            Vector3Int target,
            float movementDuration,
            bool strafe = false,
            System.Func<string, string> prefixLogMessage = null)
        {
            var entity = Enemy.Entity;
            var outcome = entity.Node.AllowsTransition(
                entity,
                entity.Coordinates,
                entity.AnchorDirection,
                translationDirection,
                out var translationTarget,
                out var targetAnchor);

            var movement = translationDirection.AsMovement(entity.LookDirection, entity.Down);
            var easing = GetMovementEasing(movement);

            if ((strafe && movement.IsTranslation()) || movement == Movement.Forward)
            {
                if (outcome == MovementOutcome.Refused || TDSafeZone.In(translationTarget)) return Movement.None;
                // if (prefixLogMessage != null) Debug.Log(prefixLogMessage("Moving forward"));
                entity.MovementInterpreter.InvokeMovement(movement, movementDuration, easing);
                return movement;
            }

            if (movement == Movement.Up || movement == Movement.Down)
            {
                if (outcome == MovementOutcome.Refused || TDSafeZone.In(translationTarget)) return Movement.None;

                // if (prefixLogMessage != null) Debug.Log(prefixLogMessage($"Moving {movement}"));
                entity.MovementInterpreter.InvokeMovement(movement, movementDuration, easing);
                return movement;
            }

            var yawBias = entity.LookDirection.AsPlanarRotation(entity.Down, entity.Coordinates, target);
            movement = translationDirection.AsPlanarRotation(entity.LookDirection, entity.Down, yawBias);
            if (movement != Movement.None)
            {
                // We are turning
                entity.MovementInterpreter.InvokeMovement(movement, movementDuration, easing);
                return movement;
            }

            if (Dungeon[translationTarget].AllowsEntryFrom(entity, translationDirection.Inverse()))
            {
                // TODO: Could we have a sane fallback here?
                // Note that we can't just chance going forward at least since there might be nothing there,
                // or there might be a safe zone there
                if (prefixLogMessage != null)
                {
                    Debug.LogError(prefixLogMessage($"We have no movement based on needed direction {translationDirection} while looking {entity.LookDirection}"));
                }
                else
                {
                    Debug.LogError($"We have no movement based on needed direction {translationDirection} while looking {entity.LookDirection}");
                }
            }
            return Movement.None;
        }
    }
}
