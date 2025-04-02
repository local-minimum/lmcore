using LMCore.Crawler;
using LMCore.TiledImporter;
using System.Collections.Generic;

namespace LMCore.TiledDungeon.Integration
{
    [System.Serializable]
    public class TDCornersClass
    {
        public bool NorthWest;
        public bool NorthEast;
        public bool SouthEast;
        public bool SouthWest;

        public TDCornersClass() { }

        public static TDCornersClass From(TiledCustomClass corners)
        {
            if (corners == null) return null;

            return new TDCornersClass()
            {
                NorthWest = corners.Bool("NorthWest"),
                NorthEast = corners.Bool("NorthEast"),
                SouthEast = corners.Bool("SouthEast"),
                SouthWest = corners.Bool("SouthWest")
            };
        }


        /// <summary>
        /// Returns number of set corners
        /// </summary>
        public int Count
        {
            get
            {
                var count = NorthWest ? 0 : 1;
                count += NorthEast ? 0 : 1;
                count += SouthEast ? 0 : 1;
                count += SouthWest ? 0 : 1;

                return count;
            }
        }

        public IEnumerable<Corner> Corners
        {
            get
            {
                if (NorthWest) yield return Corner.NorthWest;
                if (NorthEast) yield return Corner.NorthEast;
                if (SouthEast) yield return Corner.SouthEast;
                if (SouthWest) yield return Corner.SouthWest;
            }
        }
    }
}
