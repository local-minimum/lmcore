using LMCore.Crawler;
using LMCore.TiledDungeon.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.Style
{
    public class StyleSelector : AbsDungeonStyle 
    {
        [SerializeField]
        AbsDungeonStyle DefaultStyle;

        [SerializeField]
        List<AbsDungeonStyle> _styles = new List<AbsDungeonStyle>();

        GameObject GetWithStyle(string variant, System.Func<AbsDungeonStyle, GameObject> getter)
        {
            if (string.IsNullOrEmpty(variant)) {
                return getter(DefaultStyle);
            }

            return getter(_styles.FirstOrDefault(style => style.Provides(variant)) ?? DefaultStyle) ?? getter(DefaultStyle);
        }

        public override GameObject Get(Transform parent, string classId, string variant = null)
        {
            return GetWithStyle(variant, style => style.Get(parent, classId, variant));
        }

        public override GameObject Get(Transform parent, string classId, Direction direction, string variant = null)
        {
            return GetWithStyle(variant, style => style.Get(parent, classId, direction, variant));
        }

        public override GameObject Get(Transform parent, string classId, string fallbackClassId, Direction direction, string variant = null)
        {
            return GetWithStyle(variant, style => style.Get(parent, classId, fallbackClassId, direction, variant));
        }

        public override GameObject Get(Transform parent, string classId, TDEnumOrientation orientation, string variant = null)
        {
            return GetWithStyle(variant, style => style.Get(parent, classId, orientation, variant));
        }

        public override GameObject Get(Transform parent, string classId, TDEnumOrientation orientation, TDEnumInteraction interaction, string variant = null)
        {
            return GetWithStyle(variant, style => style.Get(parent, classId, orientation, interaction, variant));
        }

        public override GameObject Get(Transform parent, string classId, TDEnumElevation elevation, Direction direction, string variant = null)
        {
            return GetWithStyle(variant, style => style.Get(parent, classId, elevation, direction, variant));
        }

        public override GameObject Get(Transform parent, string classId, TDEnumTransition transition, string variant = null)
        {
            return GetWithStyle(variant, style => style.Get(parent, classId, transition, variant));
        }
    }
}
