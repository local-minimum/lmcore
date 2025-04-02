using LMCore.Crawler;
using LMCore.Extensions;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyAttacking : TDAbsEnemyBehaviour
    {
        IAttack[] _attacks;
        public IAttack[] attacks
        {
            get
            {
                if (_attacks == null)
                {
                    _attacks = GetComponentsInChildren<IAttack>(true);
                }
                return _attacks;
            }
        }

        IAttack activeAttact { get; set; }

        [SerializeField, Range(0f, 3f)]
        float minIntermission = 0.3f;
        [SerializeField, Range(0f, 10f)]
        float maxIntermission = 1.2f;
        [SerializeField, Range(0f, 3f)]
        float initialWaitIntermissionScale = 1;

        float intermission = -1f;
        float nextAttack;

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"{name} Attack: Active({activeAttact}) Ready({string.Join(", ", attacks.Where(a => a.Ready))})");
        }

        private void OnEnable()
        {
            Enemy.OnResumeEnemy += Enemy_OnResumeEnemy;
            GridEntity.OnMove += GridEntity_OnMove;
        }

        private void OnDisable()
        {
            Enemy.OnResumeEnemy -= Enemy_OnResumeEnemy;
            GridEntity.OnMove -= GridEntity_OnMove;
        }

        private void GridEntity_OnMove(GridEntity entity)
        {
            if (entity.EntityType != GridEntityType.PlayerCharacter) return;
            if (activeAttact == null || !activeAttact.Active || activeAttact.Ready) return;

            if (!activeAttact.Committed && !activeAttact.Ready)
            {
                activeAttact.Abort();
                activeAttact = null;
                intermission = minIntermission;
                if (GetRandomAttack() == null)
                {
                    Enemy.UpdateActivity(true);
                    intermission *= initialWaitIntermissionScale;
                    return;
                }

                nextAttack = Time.timeSinceLevelLoad + intermission;
            }
        }

        private void Enemy_OnResumeEnemy(float pauseDuration)
        {
            if (intermission >= 0f)
            {
                nextAttack += pauseDuration;
            }
        }

        public override void EnterBehaviour()
        {
            base.EnterBehaviour();
            if (intermission < 0f)
            {
                intermission = Random.Range(minIntermission, maxIntermission) * initialWaitIntermissionScale;
            }
            nextAttack = Time.timeSinceLevelLoad + intermission;
        }

        IAttack GetRandomAttack() =>
            attacks.Where(a => a.Ready).ToList().GetRandomElementOrDefault();

        private void Update()
        {
            if (Paused) return;

            if (activeAttact != null)
            {
                if (activeAttact.Active) return;

                if (GetRandomAttack() == null)
                {
                    Enemy.UpdateActivity(true);
                    return;
                }
                else
                {
                    intermission = Random.Range(minIntermission, maxIntermission);
                    nextAttack = Time.timeSinceLevelLoad + intermission;
                    activeAttact = null;
                    Debug.Log($"next attack will be in {intermission} seconds");
                }
            }

            if (Time.timeSinceLevelLoad < nextAttack) return;

            activeAttact = GetRandomAttack();
            if (activeAttact == null)
            {
                Enemy.UpdateActivity(true);
                return;
            }

            activeAttact.Attack();
            intermission = -1;
        }

        public override void ExitBehaviour()
        {
            if (activeAttact != null)
            {
                if (activeAttact.Active && !activeAttact.Committed)
                {
                    activeAttact.Abort();
                    intermission = Mathf.Max(minIntermission, nextAttack - Time.timeSinceLevelLoad) * initialWaitIntermissionScale;
                }
                else
                {
                    Debug.LogWarning($"{name} exited, but attack {activeAttact} is committed to");
                    intermission = -1;
                }

                activeAttact = null;
            }
            else
            {
                intermission = Mathf.Max(minIntermission, nextAttack - Time.timeSinceLevelLoad) * initialWaitIntermissionScale;
            }

            base.ExitBehaviour();
        }
    }
}
