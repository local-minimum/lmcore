using LMCore.EntitySM;
using LMCore.EntitySM.State;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{
    public class TDEnemyDebugMaterials : MonoBehaviour
    {
        [SerializeField]
        Renderer swapTarget;

        [SerializeField]
        Material defaultMat;
        [SerializeField]
        Material patrolMat;
        [SerializeField]
        Material guardMat;
        [SerializeField]
        Material huntMat;
        [SerializeField]
        Material fightMat;

        ActivityManager _ActivityManager;
        ActivityManager ActivityManager
        {
            get
            {
                if (_ActivityManager == null)
                {
                    _ActivityManager = GetComponentInChildren<ActivityManager>();
                }
                return _ActivityManager;
            }
        }

        private void Start()
        {
            SyncState(ActivityManager, StateType.Patrolling);
        }

        private void OnEnable()
        {
            ActivityState.OnEnterState += SyncState; ;
        }

        private void OnDisable()
        {
            ActivityState.OnEnterState -= SyncState; ;
        }

        private void SyncState(EntitySM.ActivityManager manager, ActivityState state) =>
            SyncState(manager, state.State);

        private void SyncState(EntitySM.ActivityManager manager, StateType state)
        {
            if (manager != ActivityManager || swapTarget == null) return;

            switch (state)
            {
                case StateType.Patrolling:
                    swapTarget.material = patrolMat;
                    break;
                case StateType.Guarding:
                    swapTarget.material = guardMat;
                    break;
                case StateType.Hunting:
                    swapTarget.material = huntMat;
                    break;
                case StateType.Fighting:
                    swapTarget.material = fightMat;
                    break;
                default:
                    swapTarget.material = defaultMat;
                    break;
            }
        }
    }
}
