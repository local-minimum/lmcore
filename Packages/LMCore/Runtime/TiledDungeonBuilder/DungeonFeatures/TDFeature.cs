using LMCore.Crawler;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public abstract class TDFeature : MonoBehaviour
    {
        #region Coordinates
        public Vector3Int Coordinates => GetComponentInParent<TDNode>().Coordinates;
        Vector3Int? _startCoordinates = null;
        protected Vector3Int StartCoordinates
        {
            get
            {
                if (_startCoordinates == null)
                {
                    _startCoordinates = Coordinates;
                }
                return _startCoordinates ?? Coordinates;
            }
        }
        protected void InitStartCoordinates()
        {
            if (_startCoordinates == null)
            {
                _startCoordinates = Coordinates;
            }
        }
        #endregion

        public TDNode Node => GetComponentInParent<TDNode>();
        public Anchor Anchor => GetComponentInParent<Anchor>();
    }
}
