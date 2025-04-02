using UnityEngine;

namespace LMCore.UI
{
    public class UIPaletteCustom : MonoBehaviour, IPaletteBlocker
    {
        [SerializeField]
        string styleId;

        public string StyleId => styleId;

        [ContextMenu("Set children from palette")]
        public void SetFromPalette()
        {
            GetComponentInParent<UIPalette>().SetCustomPalette(this);
        }
    }
}
