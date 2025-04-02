using LMCore.Extensions;
using System.Linq;
using UnityEngine;

namespace LMCore.UI
{
    public class UIPaletteSentinell : MonoBehaviour
    {
        UIPalette palette
        {
            get
            {
                var blocker = gameObject.WalkParentsRecursively(
                    p => p.GetComponent<IPaletteBlocker>() != null,
                    p => p.GetComponent<IPaletteBlocker>())
                    .FirstOrDefault();

                if (blocker is UIPalette)
                {
                    return blocker as UIPalette;
                }

                return null;
            }
        }

        private void Start()
        {
            var customPalette = GetComponent<UIPaletteCustom>();
            if (customPalette.enabled)
            {
                customPalette.SetFromPalette();
                return;
            }

            var palette = this.palette;

            if (palette == null)
            {
                Debug.LogWarning($"UI Palette Sentinell {name} doesn't have a parent palette");
                return;
            }

            palette.ApplyRecursively(gameObject);
        }
    }
}
