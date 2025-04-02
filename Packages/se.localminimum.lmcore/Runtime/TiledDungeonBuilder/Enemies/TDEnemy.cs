using LMCore.Crawler;
using LMCore.EntitySM;
using LMCore.EntitySM.State;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.SaveLoad;
using LMCore.TiledImporter;
using LMCore.UI;
using LMCore.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Enemies
{

    // Enemy deaths are handled by the battle system
    public delegate void ChangeStateEvent(StateType type);
    public delegate void ResumeEnemyEvent(float pauseDuration);
    public delegate void PauseEnemyEvent();

    /// <summary>
    /// Coordinating script for each enemy.
    /// 
    /// Various Tiled configurations that may interact with an enemy:
    /// 
    /// --- The Enemy ---
    /// Class/Type: TiledConfiguration.EnemyClass
    /// Custom Properties:
    /// * TiledConfiguration.ObjEnemyClassIdKey (string): The type of enemy to spawn in. 
    /// * TiledConfiguration.ObjEnemyIdKey (string): The unique id for the enemy on the level 
    /// * TiledConfiguration.ObjEnemyForcedStateKey (string/StateType) [Optional]: If enemy
    ///   must start in a particular state else it uses the default start state of the enemy class
    /// * TiledConfiguration.ObjLookDirectionKey (Direction) [Optional]: Spawn look direction.
    ///   Also, if first state (either forced or default) allows for look direction restrictions 
    ///   (e.g. Guarding) then this direction is also applied there.
    ///
    /// --- Its stats ---
    /// * "EMaxHealth" (int): Adjust the max health of the prefab
    /// * "EStartHealth" (int): Adjust the start health (otherwise will become max health if 
    ///   max health is adjusted)
    /// * "EBaseDefence" (int): Adjust the base defence of the prefab
    /// 
    /// --- Its Danger Zone ---
    /// * "EDangerZoneSize" (int): Size of the danger zone
    /// * "EDangerZoneStay" (int): The duration tiles remain in danger
    /// 
    /// --- Its Home Area ---
    /// Place that enemy returns to when not interacting with the player.
    /// 
    /// Class/Type: TiledConfiguration.ObjHomeAreaKey
    /// Custom Properties:
    /// * TiledConfiguration.ObjEnemyIdKey (string): Must match the id of the enemy, not the
    /// class id.
    /// 
    /// --- Its Paths ---
    /// Enemy paths are points or areas (checkpoints) that the enemy pass through in sequence.
    /// 
    /// Class/Type: TiledConfiguration.ObjPathKey
    /// Curstom Properties:
    /// * TiledConfiguration.ObjEnemyIdKey (string): Must match the id of the enemy, not the
    /// class id.
    /// * TiledConfiguration.LoopKey (int) [Optional]: If more than one patrol path is 
    /// available to the enemy, it needs to be included to identify the different paths
    /// * TiledConfiguration.RankKey (int): Required id for the order of the checkpoints in
    /// the path. If multiple has same rank, there's a checkpoint area, the closest one is
    /// used.
    /// * TiledConfiguration.BounceKey (bool) [Optional]: If upon reaching this checkpoint, the path
    /// should be inverted. Typically for non looping paths, both ends should have this set.
    /// * TiledConfiguration.Terminal (bool) [Optional]: If upon reaching this checkpoint the pathing is
    ///   considered completed and can't be continued. The enemy will attempt to transition into a new state
    ///   using default configured transitions. Note that if forced state is used, terminal is ignored.
    /// * TiledConfiguration.ObjEnemyForcedStateKey (string/StateType) [Optional]: If enemy
    ///   must enter in a particular state upon reaching the checkpoint
    /// * TiledConfiguration.ObjLookDirectionKey (Direction) [Optional]: If forced state supports
    ///   limitation of look direction (e.g. Guarding) then this is the only direction it will look
    /// </summary>
    public class TDEnemy : MonoBehaviour, IOnLoadSave
    {
        public event ChangeStateEvent OnChangeState;
        public event ResumeEnemyEvent OnResumeEnemy;
        public event PauseEnemyEvent OnPauseEnemy;

        #region ID:s
        [SerializeField, Tooltip("Identifier when instancing, not to confuse with identifier for defining individual's areas and checkpoints")]
        string _classId = System.Guid.NewGuid().ToString();
        public string ClassId => _classId;

        [ContextMenu("Generate Class ID")]
        void GenerateClassID()
        {
            _classId = System.Guid.NewGuid().ToString();
        }

        [SerializeField, HideInInspector]
        string id;
        public string Id => id;
        #endregion

        [SerializeField]
        int _Level = 1;
        public int Level => _Level;

        [SerializeField]
        int _XP;
        public int XP => _XP;

        [SerializeField, Tooltip("If enemy uses animations that should be triggered by behaviours add it here")]
        Animator _animator;
        public Animator animator => _animator;

        [SerializeField]
        string deathAnimationTrigger;

        [SerializeField]
        string instaDeathAnimationTrigger;

        #region Lazy Refs
        ActivityManager _ActivityManager;
        ActivityManager ActivityManager
        {
            get
            {
                if (_ActivityManager == null)
                {
                    _ActivityManager = GetComponentInChildren<ActivityManager>(true);
                }
                return _ActivityManager;
            }
        }

        Personality _personality;
        public Personality Personality
        {
            get
            {
                if (_personality == null)
                {
                    _personality = GetComponentInChildren<Personality>(true);
                }
                return _personality;
            }
        }

        GridEntity _Entity;
        public GridEntity Entity
        {
            get
            {
                if (_Entity == null)
                {
                    _Entity = GetComponent<GridEntity>();
                }

                return _Entity;
            }

        }

        TiledDungeon _TiledDungeon;
        public TiledDungeon Dungeon
        {
            get
            {
                if (_TiledDungeon == null)
                {
                    _TiledDungeon = GetComponentInParent<TiledDungeon>();
                }

                return _TiledDungeon;
            }
        }

        TDAbsEnemyBehaviour[] _behaviours = null;
        TDAbsEnemyBehaviour[] behaviours
        {
            get
            {
                if (_behaviours == null)
                {
                    _behaviours = GetComponentsInChildren<TDAbsEnemyBehaviour>(true);
                }
                return _behaviours;
            }
        }
        TDAbsEnemyBehaviour activeBehaviour;

        SmoothMovementTransitions _movementHandler;
        public SmoothMovementTransitions movementHandler
        {
            get
            {
                if (_movementHandler == null)
                {
                    _movementHandler = GetComponent<SmoothMovementTransitions>();
                }
                return _movementHandler;
            }
        }

        IEnemyStats _Stats;
        public IEnemyStats Stats
        {
            get
            {
                if (_Stats == null)
                {
                    _Stats = GetComponent<IEnemyStats>();
                }
                return _Stats;
            }
        }
        #endregion

        public bool Alive => Stats.IsAlive && Entity.Alive;

        protected string PrefixLogMessage(string message) =>
            $"Enemy {ClassId} - {Id} ({activeState?.State ?? StateType.None} / {activeBehaviour}): {message}";

        bool MyHomeAreasFilter(Vector3Int position, TDNodeConfig config)
        {
            return config.HasObject(
                TiledConfiguration.InstanceOrCreate().ObjHomeAreaKey,
                props => props.String(TiledConfiguration.instance.ObjEnemyIdKey) == Id);
        }

        bool MyPathFilter(Vector3Int position, TDNodeConfig config)
        {
            return config.HasObject(
                TiledConfiguration.InstanceOrCreate().ObjPathKey,
                props => props.String(TiledConfiguration.instance.ObjEnemyIdKey) == Id);
        }

        Vector3 CoordinatesExtractor(Vector3Int position, TDNodeConfig config) => position;


        [ContextMenu("Info")]
        void Info()
        {
            var paths = string.Join(", ", TDPathCheckpoint.GetAll(this).OrderBy(c => c.Loop).ThenBy(c => c.Rank));
            var areas = string.Join(", ", TDAreaMarker.GetAll(this).OrderBy(c => c.Coordinates));

            Debug.Log(PrefixLogMessage($"Home area: {areas} / Paths {paths}"));
        }

        public int ArbitraryMaxPathSearchDepth => 40;

        public void Configure(
            string id,
            Direction lookDirection = Direction.None,
            StateType forcedState = StateType.None,
            List<TiledCustomProperties> configs = null)
        {
            this.id = id;
            Entity.Identifier = id;

            if (lookDirection != Direction.None)
            {
                Entity.LookDirection = lookDirection;
                forceDirection = lookDirection;
                Entity.Sync();
            }

            if (forcedState != StateType.None)
            {
                ActivityManager.UpdateAndForceEntryState(forcedState);
            }

            // Stats Config
            var maxHealth = configs
                .FirstOrDefault(prop => prop.Ints.ContainsKey("EMaxHealth"))
                ?.Int("EMaxHealth", -1) ?? -1;
            var startHealth = configs
                .FirstOrDefault(prop => prop.Ints.ContainsKey("EStartHealth"))
                ?.Int("EStartHealth", -1) ?? -1;
            var baseDefence = configs
                .FirstOrDefault(prop => prop.Ints.ContainsKey("EBaseDefence"))
                ?.Int("EBaseDefence", -1) ?? -1;
            Stats.Configure(maxHealth, baseDefence, startHealth);

            // DangerZone Config
            var dzSize = configs
                .FirstOrDefault(prop => prop.Ints.ContainsKey("EDangerZoneSize"))
                ?.Int("EDangerZoneSize", -1) ?? -1;

            var dzStay = configs
                .FirstOrDefault(prop => prop.Ints.ContainsKey("EDangerZoneStay"))
                ?.Int("EDangerZoneStay", -1) ?? -1;

            var dangerZone = GetComponentInChildren<TDDangerZone>();
            if (dangerZone != null)
            {
                dangerZone.Configure(dzSize, dzStay);
            }

            Info();
        }

        private void OnEnable()
        {
            ActivityState.OnStayState += ActivityState_OnStayState;
            ActivityState.OnEnterState += ActivityState_OnEnterState;

            AbsMenu.OnShowMenu += HandleMenusPausing;
            AbsMenu.OnExitMenu += HandleMenusPausing;

            for (int i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour != null && behaviour != activeBehaviour)
                {
                    Debug.Log(PrefixLogMessage($"Disabling {behaviour} because not {activeBehaviour}"));
                    behaviour.enabled = false;
                }
            }
        }

        private void OnDisable()
        {
            ActivityState.OnStayState -= ActivityState_OnStayState;
            ActivityState.OnEnterState -= ActivityState_OnEnterState;

            AbsMenu.OnShowMenu -= HandleMenusPausing;
            AbsMenu.OnExitMenu -= HandleMenusPausing;
        }

        StateTimer<bool> pauseTimer;

        public bool Paused
        {
            get => pauseTimer?.state ?? false;
            private set
            {
                if (pauseTimer == null)
                {
                    pauseTimer = new StateTimer<bool>(false);
                }
                if (pauseTimer.UpdateState(value, out var duration))
                {
                    if (value)
                    {
                        movementHandler.Paused = value;

                        animator.enabled = false;

                        OnPauseEnemy?.Invoke();
                    }
                    else
                    {
                        movementHandler.Paused = value;

                        animator.enabled = true;

                        OnResumeEnemy?.Invoke(duration);
                    }
                }
            }
        }

        private void HandleMenusPausing(AbsMenu menu)
        {
            Paused = AbsMenu.PausingGameplay;
        }

        #region SaveState
        /// <summary>
        /// Next frame will invoke active states taxation
        /// </summary>
        public bool MayTaxStay { get; set; }

        ActivityState activeState;
        #endregion

        private void ActivityState_OnStayState(ActivityManager manager, ActivityState state)
        {
            if (manager != ActivityManager || !MayTaxStay) return;

            if (state != activeState)
            {
                activeState = state;
                OnChangeState?.Invoke(state.State);
            }

            state.TaxStayPersonality(Personality);
            MayTaxStay = false;
        }

        private void Update()
        {
            if (Paused) return;

            if (activeState == null && Stats.IsAlive)
            {
                ActivityManager.ForceEntryState();
            }
            else if (activeBehaviour != null && !activeBehaviour.Valid)
            {
                SynchActiveStateBehavior();
            }
        }

        void Kill(bool animate = true)
        {
            Stats.Kill();

            if (activeState != null)
            {
                activeState.enabled = false;
                activeState.Exit();
                activeState = null;
                OnChangeState?.Invoke(StateType.None);
            }

            foreach (var behaviour in behaviours)
            {
                behaviour.enabled = false;
            }

            if (animator != null)
            {

                if (!animate && !string.IsNullOrEmpty(instaDeathAnimationTrigger))
                {
                    animator.SetTrigger(instaDeathAnimationTrigger);
                }
                if (!string.IsNullOrEmpty(deathAnimationTrigger))
                {
                    animator.SetTrigger(deathAnimationTrigger);
                }
            }


            Entity.Kill();
            Debug.Log(PrefixLogMessage($"Death occured at {Entity.Coordinates}"));

            enabled = false;
            movementHandler.enabled = false;
            ActivityManager.enabled = false;
            var dangerZone = GetComponentInChildren<TDDangerZone>();
            if (dangerZone != null)
            {
                dangerZone.enabled = false;
            }

            foreach (var perception in GetComponentsInChildren<TDEnemyPerception>())
            {
                perception.enabled = false;
            }
        }

        public StateType CurrentActivityState => activeState == null ? StateType.None : activeState.State;

        /// <summary>
        /// Call this whenever the enemy has done something or the player
        /// </summary>
        public void UpdateActivity(bool avoidActive = false)
        {
            if (!Stats.IsAlive)
            {
                Kill();
                return;
            }

            // Debug.Log(PrefixLogMessage("Checking for new state"));
            ActivityManager.CheckTransition(avoidActive);
        }

        /// <summary>
        /// Force the activity manager into a particular state independed
        /// of if it normally is an allowed transition
        /// </summary>
        /// <param name="state">Sought state</param>
        public void ForceActivity(StateType state, Direction forceDirection = Direction.None)
        {
            var previousState = activeState == null ? StateType.None : activeState.State;
            this.forceDirection = forceDirection;
            ActivityManager.ForceState(state);
            Debug.Log(PrefixLogMessage($"Forced activity {state}, was {previousState} (Enforced direction: {forceDirection})"));
        }

        [SerializeField, HideInInspector]
        Direction forceDirection = Direction.None;

        private void SynchActiveStateBehavior(bool initBehaviour = true)
        {
            if (activeState == null)
            {
                return;
            }

            var newActiveBehaviour = activeState.GetComponent<TDAbsEnemyBehaviour>();
            newActiveBehaviour.enabled = true;

            if (newActiveBehaviour is TDEnemyPatrolling)
            {
                TDEnemyPatrolling patrolling = (TDEnemyPatrolling)newActiveBehaviour;
                if (initBehaviour && !patrolling.HasTarget) SetPatrolGoal(patrolling);
            }
            else if (newActiveBehaviour is TDEnemyGuarding)
            {
                TDEnemyGuarding guarding = (TDEnemyGuarding)newActiveBehaviour;
                if (initBehaviour) guarding.InitGuarding(forceDirection);
            }
            else if (newActiveBehaviour is TDEnemyHunting)
            {
                if (initBehaviour)
                {
                    TDEnemyHunting hunting = (TDEnemyHunting)newActiveBehaviour;
                    var perception = GetComponentsInChildren<TDEnemyPerception>()
                        .Where(p => p.Target != null)
                        .FirstOrDefault();

                    if (perception == null)
                    {
                        Debug.LogWarning(PrefixLogMessage("We can't be hunting without a target"));
                        newActiveBehaviour.enabled = false;
                        return;
                    }
                    hunting.InitHunt(perception.Target);
                }
            }

            if (activeBehaviour != newActiveBehaviour)
            {
                if (activeBehaviour != null)
                {
                    activeBehaviour.ExitBehaviour();
                    activeBehaviour.enabled = false;
                    Debug.Log(PrefixLogMessage($"Disabling {activeBehaviour} because switching to {newActiveBehaviour}"));
                }

                if (newActiveBehaviour != null)
                {
                    newActiveBehaviour.EnterBehaviour();
                    newActiveBehaviour.enabled = true;
                }

                activeBehaviour = newActiveBehaviour;
                Debug.Log(PrefixLogMessage($"{activeBehaviour} now ready"));

            }
            else if (newActiveBehaviour != null && !newActiveBehaviour.enabled)
            {
                Debug.LogWarning(PrefixLogMessage(
                    $"We seem to be in a disabled behaviour {newActiveBehaviour}, attempting recovery"));

                newActiveBehaviour.EnterBehaviour();
                newActiveBehaviour.enabled = true;
                SynchActiveStateBehavior();
            }

            forceDirection = Direction.None;
        }

        private void ActivityState_OnEnterState(ActivityManager manager, ActivityState state)
        {
            if (manager != ActivityManager && Stats.IsAlive) return;

            Debug.Log(PrefixLogMessage($"Getting state {state.State}"));
            activeState = state;
            SynchActiveStateBehavior();
            OnChangeState?.Invoke(state.State);
        }

        TDPathCheckpoint ClosestCheckpoint(int loop = -1) =>
            ClosestCheckpoint(pt => loop == -1 || pt.Loop == loop);

        public TDPathCheckpoint ClosestCheckpointOnOtherLoop(TDPathCheckpoint current) =>
            ClosestCheckpoint(pt => pt.Loop != current.Loop);

        TDPathCheckpoint ClosestCheckpoint(System.Func<TDPathCheckpoint, bool> predicate)
        {
            var entity = Entity;
            var dungeon = Dungeon;
            TDPathCheckpoint closest = null;
            int maxDistance = int.MaxValue;
            var points = TDPathCheckpoint
                .GetAll(this, predicate)
                .Select(ch => new { CheckPoint = ch, Dist = entity.Coordinates.ManhattanDistance(ch.Coordinates) })
                .OrderBy(pt => pt.Dist)
                .ToList();

            foreach (var pt in points)
            {
                if (pt.Dist >= maxDistance)
                {
                    continue;
                }

                if (dungeon.ClosestPath(
                    entity,
                    entity.Coordinates,
                    pt.CheckPoint.Coordinates,
                    maxDistance,
                    out var closestPath,
                    refuseSafeZones: true))
                {
                    // Path includes start position
                    var distance = closestPath.Count() - 1;
                    if (distance < maxDistance)
                    {
                        closest = pt.CheckPoint;
                        maxDistance = distance;
                    }
                }
                else
                {
                    Debug.LogWarning(PrefixLogMessage($"Found no path from {Entity.Coordinates} to {pt.CheckPoint} from with max distance {maxDistance}"));
                }
            }

            return closest;
        }

        public IEnumerable<TDPathCheckpoint> GetCheckpoints(int loop, int rank) =>
            TDPathCheckpoint.GetAll(this, loop, rank);

        public int LoopMaxRank(int loop) =>
            TDPathCheckpoint.GetLoop(this, loop).Max(c => c.Rank);

        public IEnumerable<TDPathCheckpoint> GetNextCheckpoints(
            TDPathCheckpoint current,
            int direction,
            out int newDirection)
        {
            if (direction == 0)
            {
                direction = 1;
            }

            if (current.Terminal)
            {
                newDirection = 0;
                return new List<TDPathCheckpoint>();
            }
            else if (current.Bounce)
            {
                direction *= -1;
                Debug.Log(PrefixLogMessage($"Current {current.name} chpt is bounce"));
            }
            var nextRank = current.Rank + direction;

            var loopMax = LoopMaxRank(current.Loop);
            if (nextRank < 0)
            {
                nextRank = loopMax;
            }
            else if (nextRank > loopMax)
            {
                // Debug.Log(PrefixLogMessage($"We are looping {nextRank} > {loopMax}"));
                nextRank = 0;
            }

            newDirection = direction;
            // Debug.Log(PrefixLogMessage($"Setting goal rank {nextRank} from {current} using direction {direction} with resulting direction {direction}"));
            return GetCheckpoints(current.Loop, nextRank);
        }

        void SetPatrolGoal(TDEnemyPatrolling patrolling)
        {
            if (patrolling == null)
            {
                Debug.LogError(PrefixLogMessage("I don't have a patrolling pattern"));
                return;
            }

            var pathCheckpoint = ClosestCheckpoint();
            if (pathCheckpoint == null)
            {
                Debug.LogError(PrefixLogMessage("There's no closest checkpoint for me"));
                Info();
            }
            else
            {
                // Debug.Log(PrefixLogMessage($"Setting patroll checkpoint {pathCheckpoint}"));
                patrolling.SetCheckpointFromPatrolPath(pathCheckpoint, pathCheckpoint.Rank == 0 && pathCheckpoint.Bounce ? -1 : 1);
            }
        }

        #region Save / Load
        public EnemySave Save()
        {
            var patrolling = (TDEnemyPatrolling)behaviours.FirstOrDefault(b => b is TDEnemyPatrolling);
            var guarding = (TDEnemyGuarding)behaviours.FirstOrDefault(b => b is TDEnemyGuarding);
            var hunting = (TDEnemyHunting)behaviours.FirstOrDefault(b => b is TDEnemyHunting);
            var dangerZone = GetComponentInChildren<TDDangerZone>();

            return new EnemySave()
            {
                Id = Id,
                Alive = Stats.IsAlive,
                entity = new GridEntitySave(Entity),
                dangerZone = Stats.IsAlive && dangerZone != null ? dangerZone.Save() : null,

                activeState = activeState?.name,
                activeStateType = activeState?.State ?? StateType.None,
                mayTaxStay = MayTaxStay,
                activeStateActiveDuration = activeState?.ActiveDuration ?? 0,

                patrolling = patrolling == null ? null : patrolling.Save(),
                guarding = guarding == null ? null : guarding.Save(),
                hunting = hunting == null ? null : hunting.Save(),
            };
        }

        public int OnLoadPriority => 750;

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

            var enemySave = lvlSave.enemies.FirstOrDefault(s => s.Id == Id);
            if (enemySave != null)
            {
                // Restore entity
                enemySave.entity.LoadOntoEntity(Entity);
                Entity.Sync();
                // TODO: Check if falling must be set some other way and if it needs to be before sync or after
                Entity.Falling = enemySave.entity.falling;

                if (!enemySave.Alive)
                {
                    Kill(false);
                    Debug.Log(PrefixLogMessage("Loaded as dead"));
                    return;
                }

                MayTaxStay = enemySave.mayTaxStay;

                // Reinstate the active state from save info
                if (enemySave.activeState == null)
                {
                    activeState = null;
                    Debug.LogWarning(PrefixLogMessage("Entity loads into no active state"));
                }
                else
                {
                    var states = GetComponentsInChildren<ActivityState>()
                        .Where(s => s.State == enemySave.activeStateType)
                        .ToList();

                    if (states.Count == 0)
                    {
                        Debug.LogError(PrefixLogMessage($"Could not load enemy into {enemySave.activeStateType} because I don't have one"));
                    }
                    else if (states.Count == 1)
                    {
                        activeState = states[0];
                    }
                    else
                    {
                        activeState = states
                             .OrderBy(s => s.name.LevenshteinDistance(enemySave.activeState))
                             .First();
                    }

                    activeState?.Load(true, enemySave.activeStateActiveDuration);
                    SynchActiveStateBehavior(false);
                    OnChangeState?.Invoke(activeState.State);
                }

                Debug.Log(PrefixLogMessage("Loaded"));
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage("Could not find my save state"));
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
