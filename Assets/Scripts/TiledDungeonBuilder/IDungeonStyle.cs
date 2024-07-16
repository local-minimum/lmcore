using LMCore.Crawler;
using UnityEngine;

namespace TiledDungeon {
    public interface IDungeonStyle
    {        
        public GameObject Get(Transform parent, string classId, string variant = null);
        public GameObject Get(Transform parent, string classId, Direction direction, string variant = null);
        public GameObject Get(Transform parent, string classId, TDEnumOrientation orientation, string variant = null);
        public GameObject Get(Transform parent, string classId, TDEnumOrientation orientation, TDEnumInteraction interaction, string variant = null);
        public GameObject Get(Transform parent, string classId, TDEnumLevel elevation, Direction direction, string variant = null);
        public GameObject Get(Transform parent, string classId, TDEnumTransition transition, string variant = null);
    }
}
