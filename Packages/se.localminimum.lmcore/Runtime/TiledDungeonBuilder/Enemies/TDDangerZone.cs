using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.Enemies;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDDangerZone : MonoBehaviour, IOnLoadSave
    {
        private static HashSet<TDDangerZone> _Zones;
        private static HashSet<TDDangerZone> Zones
        {
            get
            {
                if (_Zones == null)
                {
                    _Zones = GameObject.FindObjectsByType<TDDangerZone>(FindObjectsSortMode.None).ToHashSet();
                }

                return _Zones;
            }
        }

        private bool Active =>
            enabled && gameObject.activeSelf;

        public static bool In(GridEntity entity) => entity == null ? false : In(entity.Coordinates);
        public static bool In(Vector3Int coordinates) => Zones.Any(z => z.Active && z.Coordinates.Contains(coordinates));

        GridEntity _Entity;
        GridEntity Entity
        {
            get
            {
                if (_Entity == null)
                {
                    _Entity = GetComponentInParent<GridEntity>(true);
                }

                return _Entity;
            }
        }

        TiledDungeon Dungeon
        {
            get
            {
                return Entity.Dungeon as TiledDungeon;
            }
        }

        #region SaveState
        Vector3Int Epicenter;
        HashSet<Vector3Int> _Coordinates;
        Dictionary<Vector3Int, int> DecayingCoordinates = new Dictionary<Vector3Int, int>();
        #endregion

        HashSet<Vector3Int> Coordinates
        {
            get
            {
                if (_Coordinates == null)
                {
                    _Coordinates = CalculateCurrentZone().ToHashSet();
                }
                return _Coordinates;
            }
        }

        string PrefixLogMessage(string message) =>
            $"DangerZone of {Entity.name} size ({Coordinates.Count}, {DecayingCoordinates.Count}): {message}";


        public static bool FFillTransitionFilter(IDungeonNode from, Direction direction, IDungeonNode to) =>
            to != null &&
            !TDSafeZone.In(to.Coordinates) &&
            FloodFill.TransitionFilter(from, direction, to);

        IEnumerable<Vector3Int> CalculateCurrentZone()
        {
            // Can't log with prefix here because it might trigger calling this function again
            return FloodFill
                .Fill(Dungeon, Entity.Coordinates, Size - 1, FFillTransitionFilter);
        }

        [SerializeField, Range(1, 20)]
        int Size = 6;

        [SerializeField, Range(0, 10)]
        int StayDanger = 5;

        public void Configure(int size = -1, int stay = -1)
        {
            if (size > -1)
            {
                Size = size;
            }

            if (stay > -1)
            {
                StayDanger = stay;
            }
        }

        public static void InfoAll(Vector3Int coordinates)
        {
            foreach (TDDangerZone zone in Zones)
            {
                if (!zone.Active)
                {
                    Debug.Log($"DangerZone '{zone.name}': Zone disabled");
                    continue;
                }

                Debug.Log($"DangerZone '{zone.name}': coordinates {coordinates} are in zone {zone.Coordinates.Contains(coordinates)}");
                var data = zone.Coordinates
                    .OrderBy(c => c.ManhattanDistance(coordinates))
                    .Select(c => $"{c}={c == coordinates}")
                    .ToList();
                Debug.Log($"DangerZone '{zone.name}': {string.Join(", ", data)}");
            }
        }

        [ContextMenu("Info")]
        private void Info()
        {
            Debug.Log($"DangerZone '{name}': {string.Join(", ", Coordinates)}");
        }


        private void OnEnable()
        {
            // TODO: If we cycle enable disable we might not want to update this...
            Epicenter = Entity.Coordinates;
            Zones.Add(this);
            GridEntity.OnPositionTransition += GridEntity_OnPositionTransition;
        }

        private void OnDisable()
        {
            Zones.Remove(this);
            GridEntity.OnPositionTransition -= GridEntity_OnPositionTransition;
        }

        private void GridEntity_OnPositionTransition(GridEntity entity)
        {
            if (entity != Entity || Epicenter == Entity.Coordinates) return;

            // Debug.Log(PrefixLogMessage($"Updating epicenter {Epicenter} -> {entity.Coordinates}"));
            Epicenter = entity.Coordinates;

            var preexisting = Coordinates.ToHashSet();
            var current = CalculateCurrentZone().ToHashSet();

            foreach (var node in current)
            {
                _Coordinates.Add(node);
                if (DecayingCoordinates.ContainsKey(node))
                {
                    // Debug.Log($"Removing {node} from decay ({DecayingNodes[node]}) since now in zone");
                    DecayingCoordinates.Remove(node);
                }
            }

            foreach (var node in DecayingCoordinates.Keys.ToList())
            {
                DecayingCoordinates[node]--;
                // Debug.Log($"Decaying {node} danger time to {DecayingNodes[node]}");
                if (DecayingCoordinates[node] <= 0)
                {
                    DecayingCoordinates.Remove(node);
                    _Coordinates.Remove(node);
                }
            }

            foreach (var node in preexisting.Except(current).Except(DecayingCoordinates.Keys))
            {
                // Debug.Log($"Adding {node} to decay ({StayDanger}) since no longer in zone");
                DecayingCoordinates[node] = StayDanger;
            }
        }

        private void OnDestroy()
        {
            Zones.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            var dungeon = Dungeon;
            if (dungeon == null || !enabled) return;

            var size = dungeon.GridSize;
            var size3D = new Vector3(size, size, size);

            var color = Color.red;

            foreach (var coordinates in Coordinates)
            {
                var node = dungeon[coordinates];
                if (node == null) continue;

                var center = node.CenterPosition;

                var decay = DecayingCoordinates.GetValueOrDefault(coordinates);
                if (decay > 0)
                {
                    color.a = decay / (float)StayDanger;
                }
                else
                {
                    color.a = 1.0f;
                }

                Gizmos.color = color;
                Gizmos.DrawWireCube(center, size3D);
            }
        }

        #region Load/Save
        public EnemyDangerZoneSave Save() => new EnemyDangerZoneSave()
        {
            Epicenter = Epicenter,
            DecayingCoordinates = new SerializableDictionary<Vector3Int, int>(DecayingCoordinates),
        };

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

            var enemyId = GetComponentInParent<TDEnemy>().Id;

            var zoneSave = lvlSave.enemies.FirstOrDefault(s => s.Id == enemyId && s.Alive)?.dangerZone;
            if (zoneSave != null)
            {
                Epicenter = zoneSave.Epicenter;
                _Coordinates = CalculateCurrentZone().ToHashSet();
                DecayingCoordinates = new Dictionary<Vector3Int, int>(zoneSave.DecayingCoordinates);
                foreach (var coord in DecayingCoordinates.Keys)
                {
                    _Coordinates.Add(coord);
                }
                Debug.Log(PrefixLogMessage("Loaded"));
            }
            else
            {
                _Coordinates.Clear();
                DecayingCoordinates.Clear();
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
