using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.SaveLoad;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public delegate void DiscoverIllusionEvent(Vector3Int position, Direction direction);

    public class TDIllusoryCubeSide : MonoBehaviour, IOnLoadSave
    {
        public static event DiscoverIllusionEvent OnDiscoverIllusion;

        [SerializeField, HideInInspector]
        Direction direction;

        [SerializeField]
        string DiscoverTrigger = "Discover";

        [SerializeField]
        string DiscoveredTrigger = "Discovered";

        TDNode _node;
        TDNode Node { 
            get { 
                if (_node == null)
                {
                    _node = GetComponentInParent<TDNode>();
                }
                return _node; 
            } 
        }

        public Vector3Int Position => Node.Coordinates;

        [SerializeField]
        Animator animator;

        bool Discovered;

        int IOnLoadSave.OnLoadPriority => 100;

        public void Configure(Direction direction)
        {
            this.direction = direction;
        }

        private void OnEnable()
        {
            OnDiscoverIllusion += TDIllusoryCubeSide_OnDiscoverIllusion;
            foreach (var mover in Movers.movers)
            {
                if (mover.Entity.EntityType == GridEntityType.PlayerCharacter)
                {
                    mover.OnMoveStart += Mover_OnMoveStart;
                }
            }

            Movers.OnActivateMover += Movers_OnActivateMover;
            Movers.OnDeactivateMover += Movers_OnDeactivateMover;
        }

        private void OnDisable()
        {
            OnDiscoverIllusion -= TDIllusoryCubeSide_OnDiscoverIllusion;
            foreach (var mover in Movers.movers)
            {
                if (mover.Entity.EntityType == GridEntityType.PlayerCharacter)
                {
                    mover.OnMoveStart -= Mover_OnMoveStart;
                }
            }

            Movers.OnActivateMover -= Movers_OnActivateMover;
            Movers.OnDeactivateMover -= Movers_OnDeactivateMover;
        }

        private void Movers_OnDeactivateMover(IEntityMover mover)
        {
            if (mover.Entity.EntityType == GridEntityType.PlayerCharacter)
            {
                mover.OnMoveStart -= Mover_OnMoveStart;
            }
        }

        private void Movers_OnActivateMover(IEntityMover mover)
        {
            if (mover.Entity.EntityType == GridEntityType.PlayerCharacter)
            {
                mover.OnMoveStart += Mover_OnMoveStart;
            }
        }

        private void Mover_OnMoveStart(GridEntity entity, List<Vector3Int> positions, List<Direction> anchors)
        {
            for (int i = 0, n = positions.Count - 1; i < n; i++) {
                var pos = positions[i];
                var next = positions[i + 1];

                if (next == pos) continue;

                var direction = (next - pos).AsDirection();
                if (pos == Position && direction == this.direction)
                {
                    Discovered = true;
                    animator.SetTrigger(DiscoverTrigger);
                    OnDiscoverIllusion?.Invoke(Position, direction);
                    return;
                }
                if (next == Position && direction == direction.Inverse())
                {
                    Discovered = true;
                    animator.SetTrigger(DiscoverTrigger);
                    OnDiscoverIllusion?.Invoke(Position, direction.Inverse());
                    return;
                }
            }
        }

        private void TDIllusoryCubeSide_OnDiscoverIllusion(Vector3Int position, Direction direction)
        {
            var inverseDirection = direction.Inverse();

            if (!Discovered && direction.Translate(position) == Position && this.direction == inverseDirection)
            {
                Discovered = true;
                animator.SetTrigger(DiscoverTrigger);
            }
        }

        public IllusionSave Save() => new IllusionSave() { 
            position = Position,
            discovered = Discovered,
            direction = direction,
        };

        void OnLoadGameSave(GameSave save)
        {
            if (save == null)
            {
                return;
            }

            var lvl = Node.Dungeon.MapName;

            Discovered = save.levels[lvl]
                ?.illusions
                .FirstOrDefault(ill => ill.position == Position && ill.direction == direction)
                ?.discovered ?? false;

           if (Discovered)
           {
                animator.SetTrigger(DiscoveredTrigger);
           }
        }

        public void OnLoad<T>(T save) where T : new()
        {
            if (save is GameSave)
            {
                OnLoadGameSave(save as GameSave);
            }
        }
    }
}
