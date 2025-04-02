using LMCore.Crawler;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDDecoration : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        Vector3Int Coordinates;

        [SerializeField, HideInInspector]
        Direction AnchorDirection;

        TDNode _Node;
        TDNode Node
        {
            get
            {
                if (_Node == null)
                {
                    _Node = GetComponentInParent<TDNode>();
                }
                return _Node;
            }
        }

        Anchor _Anchor;
        Anchor Anchor
        {
            get
            {
                if (_Anchor == null)
                {
                    _Anchor = GetComponentInParent<Anchor>();
                }
                return _Anchor;
            }
        }

        string PrefixLogMessage(string message) =>
            $"Decoration {name} @ {Coordinates} / {AnchorDirection}: {message}";


        [ContextMenu("Fix to Node and or Anchor")]
        private void OnValidate()
        {
            var node = Node;
            if (node == null) return;

            bool updated = Coordinates != node.Coordinates;
            Coordinates = node.Coordinates;

            if (Anchor == null)
            {
                updated = updated || AnchorDirection != Direction.None;
                AnchorDirection = Direction.None;
            }
            else
            {
                updated = updated || AnchorDirection != Anchor.CubeFace;
                AnchorDirection = Anchor.CubeFace;
            }

            if (updated) Debug.Log(PrefixLogMessage("Anchored"));
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log(PrefixLogMessage(""));
        }

        public bool InPlace
        {
            get
            {
                if (Node == null) return false;
                return Node.Coordinates == Coordinates;
            }
        }

        public void Place(TiledDungeon dungeon)
        {
            var node = dungeon[Coordinates];
            if (node == null)
            {
                Debug.LogError(PrefixLogMessage("Not possible because no node there in dungeon!"));
                return;
            }

            if (AnchorDirection != Direction.None)
            {
                var anchor = node.GetAnchor(AnchorDirection);
                if (anchor == null)
                {
                    Debug.LogWarning(PrefixLogMessage($"Didn't find an anchor for {AnchorDirection}, will attach to node instead"));
                }
                else
                {
                    transform.SetParent(anchor.transform, false);
                    return;
                }
            }

            transform.SetParent(node.transform, false);
        }

        public void Remove(Transform storage)
        {
            if (!InPlace)
            {
                transform.SetParent(storage, true);
                Debug.Log(PrefixLogMessage($"Storing in {storage} without scaling, though it wasn't in the dungeon"));
                return;
            }

            var localPosition = transform.localPosition;
            var localScale = transform.localScale;
            var localRotation = transform.localRotation;

            transform.SetParent(storage, false);
            transform.localRotation = localRotation;
            transform.localScale = localScale;
            transform.localPosition = localPosition;

            Debug.Log(PrefixLogMessage($"Stored in {storage}"));
        }
    }
}
