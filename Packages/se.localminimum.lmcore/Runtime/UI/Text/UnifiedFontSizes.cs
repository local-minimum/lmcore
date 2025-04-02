using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LMCore.UI
{
    [ExecuteInEditMode]
    public class UnifiedFontSizes : MonoBehaviour
    {
        [System.Serializable]
        struct FontSizeSetting
        {
            public TextMeshProUGUI TextField;
            public float relativeScale;
            public bool drivesScaling;
        }

        [SerializeField, Tooltip("Fields drive the size of fonts")]
        List<FontSizeSetting> fields = new List<FontSizeSetting>();

        [SerializeField]
        float minFont = 4;

        [SerializeField]
        float maxFont = 700;

        [SerializeField]
        bool driveAllChildTexts;

        [SerializeField, Range(0, 3)]
        float defaultRelativeScale = 1;

        bool setSizes;

        [ContextMenu("Recalculate")]
        void Recalculate()
        {
            FreeSizes();
        }

        void FreeSizes()
        {

            foreach (var f in fields.Where(f => f.drivesScaling))
            {
                if (f.TextField.enableAutoSizing)
                {
                    f.TextField.fontSizeMin = minFont * f.relativeScale;
                    f.TextField.fontSizeMax = maxFont * f.relativeScale;
                }
            }
            setSizes = true;
        }

        void SetSizes()
        {
            var neededFontSize = fields
                .Where(f => f.drivesScaling)
                .Select(f =>
                {
                    return new
                    {
                        size = f.TextField.fontSize / f.relativeScale,
                        f.TextField.name,
                    };
                })
                .OrderBy(f => f.size)
                .First();

            // Debug.Log($"{name}: {neededFontSize.name} drives font size to {neededFontSize.size}");

            if (driveAllChildTexts)
            {
                foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    var field = fields.FirstOrDefault(f => f.TextField == t);
                    var fontScale = field.TextField == null ? defaultRelativeScale : field.relativeScale;
                    var size = neededFontSize.size * fontScale;
                    if (t.enableAutoSizing)
                    {
                        t.fontSizeMax = size;
                        t.fontSizeMin = size;
                    }
                    else
                    {
                        t.fontSize = size;
                    }
                }
            }

            setSizes = false;
        }

        private void OnGUI()
        {
            Recalculate();
        }

        private void Update()
        {
            if (setSizes)
            {
                SetSizes();
            }
        }
    }
}
