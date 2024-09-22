using LMCore.Crawler;
using UnityEngine;
using LMCore.TiledDungeon.Integration;

namespace LMCore.TiledDungeon {
    [System.Serializable]
    public abstract class AbsDungeonStyle : MonoBehaviour
    {        
        public abstract GameObject Get(Transform parent, string classId, string variant = null);
        public abstract GameObject Get(Transform parent, string classId, Direction direction, string variant = null);
        public abstract GameObject Get(Transform parent, string classId, string fallbackClassId, Direction direction, string variant = null);
        public abstract GameObject Get(Transform parent, string classId, TDEnumOrientation orientation, string variant = null);
        public abstract GameObject Get(Transform parent, string classId, TDEnumOrientation orientation, TDEnumInteraction interaction, string variant = null);
        public abstract GameObject Get(Transform parent, string classId, TDEnumElevation elevation, Direction direction, string variant = null);
        public abstract GameObject Get(Transform parent, string classId, TDEnumTransition transition, string variant = null);
    }
}
