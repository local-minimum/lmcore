using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledDungeon.SaveLoad;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public delegate void DiscoverIllusionEvent(Vector3Int position, Direction direction);

    public class TDIllusoryCubeSide : MonoBehaviour, IOnLoadSave
    {
        public static event DiscoverIllusionEvent OnDiscoverIllusion;

        public override string ToString() =>
            $"Illusory {direction} Side of {Node.Coordinates} is {(Discovered ? "discovered" : "undiscovered")}";

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log(ToString());
        }

        [SerializeField, HideInInspector]
        Direction direction;
        public Direction CubeFace => direction;

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

        public Vector3Int Coordinates => Node.Coordinates;

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
            GridEntity.OnMove += GridEntity_OnMove;
        }

        private void OnDisable()
        {
            OnDiscoverIllusion -= TDIllusoryCubeSide_OnDiscoverIllusion;
            GridEntity.OnMove -= GridEntity_OnMove;
        }

        bool underConsideration;
        Vector3Int movementStart;

        bool DidPassIllusion(Vector3Int movementEnd)
        {
            var direction = (movementEnd - movementStart).AsDirectionOrNone();

            // Debug.Log($"{this}: {direction}, start({movementStart}) end({movementEnd}) vs {Coordinates}");

            return movementStart == Coordinates && direction == this.direction ||
                movementEnd == Coordinates && direction.Inverse() == this.direction;
        }

        private void GridEntity_OnMove(GridEntity entity)
        {
            if (entity.EntityType != GridEntityType.PlayerCharacter || Discovered) { return; }

            if (entity.Moving == MovementType.Stationary)
            {
                if (DidPassIllusion(entity.Coordinates))
                {
                    Discovered = true;
                    animator.SetTrigger(DiscoverTrigger);
                    OnDiscoverIllusion?.Invoke(Coordinates, direction);
                }
            }
            else if (entity.Moving.HasFlag(MovementType.Translating))
            {
                movementStart = entity.Coordinates;
                underConsideration = movementStart.ChebyshevDistance(Coordinates) == 1;
            }
            else
            {
                underConsideration = false; 
            }
        }

        private void TDIllusoryCubeSide_OnDiscoverIllusion(Vector3Int position, Direction direction)
        {
            var inverseDirection = direction.Inverse();

            if (!Discovered && direction.Translate(position) == Coordinates && this.direction == inverseDirection)
            {
                Discovered = true;
                animator.SetTrigger(DiscoverTrigger);
            }
        }

        public IllusionSave Save() => new IllusionSave() { 
            position = Coordinates,
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
                .FirstOrDefault(ill => ill.position == Coordinates && ill.direction == direction)
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
