using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    public interface IPaletteBlocker { };

    public class UIPalette : MonoBehaviour, IPaletteBlocker
    {
        [System.Serializable]
        private struct Style
        {
            [Header("Button Style")]
            public ColorBlock buttonStyle;

            [Header("Button Highlight Style")]
            public ColorBlock buttonHighlightStyle;

            [Header("Button Text Color")]
            public ColorBlock btnTextColor;

            [Header("Text Color")]
            public Color textColor;
        }

        [System.Serializable]
        private struct CustomStyle
        {
            public string styleId;

            [Header("Button Style")]
            public bool useCustomButtonStyle;
            public ColorBlock buttonStyle;

            [Header("Button Highlight Style")]
            public bool useCustomButtonHighlightStyle;
            public ColorBlock buttonHighlightStyle;

            [Header("Button Text Color")]
            public bool useCustomBtnTextColor;
            public ColorBlock btnTextColor;

            [Header("Text Color")]
            public bool useCustomTextColor;
            public Color textColor;

            public Style ToStyle(Style baseStyle)
            {
                return new Style()
                {
                    buttonStyle = useCustomButtonStyle ? buttonStyle : baseStyle.buttonStyle,
                    buttonHighlightStyle = useCustomButtonHighlightStyle ? buttonHighlightStyle : baseStyle.buttonHighlightStyle,
                    btnTextColor = useCustomBtnTextColor ? btnTextColor : baseStyle.btnTextColor,
                    textColor = useCustomTextColor ? textColor : baseStyle.textColor,
                };
            }
        }

        [Tooltip("When offline, colors are not updated"), SerializeField]
        bool offline = true;

        [SerializeField]
        Style baseStyle;

        [SerializeField]
        List<CustomStyle> customStyles = new List<CustomStyle>();

        /// <summary>
        /// Returns true if child is permissable according to the filter
        /// </summary>
        bool StopAtPaletteBlocker(GameObject child) =>
            child.GetComponent<IPaletteBlocker>() == null;

        /// <summary>
        /// Returns true if child is permissable according to the filter
        /// </summary>
        bool StopAtPaletteOrButtonFilter(GameObject child) =>
            StopAtPaletteBlocker(child) && child.GetComponent<Button>() == null;

        IEnumerable<T> GetTargets<T>(
            GameObject parent,
            System.Func<GameObject, T> predicate,
            bool includeParent = true,
            System.Func<GameObject, bool> filter = null)
        {
            return parent
                .WalkChildrenRecursively(filter ?? StopAtPaletteBlocker)
                .Concat(new List<GameObject>() { includeParent ? parent : null })
                .Select(child => child.GetComponent<T>())
                .Where(child => child != null);
        }

        bool ApplyButtonStyle(Button btn, Style style)
        {
            if (btn == null) return false;

            // Debug.Log($"UI Palette: Applyinhg {style} to button '{btn}'");

            btn.colors = style.buttonStyle;

            foreach (var txt in GetTargets(
                btn.gameObject,
                child => child.GetComponent<TextMeshProUGUI>(),
                filter: StopAtPaletteOrButtonFilter))
            {
                txt.color = btn.interactable ? style.btnTextColor.normalColor : style.btnTextColor.disabledColor;
                var highlight = txt.GetComponent<ButtonHighlight>();
                if (highlight != null)
                {
                    highlight.colors = style.btnTextColor;
                }
            }

            foreach (var highlight in GetTargets(
                btn.gameObject,
                child => child.GetComponent<ButtonHighlight>(),
                filter: StopAtPaletteOrButtonFilter))
            {
                highlight.colors = style.buttonHighlightStyle;
            }

            return true;
        }

        void ApplyTextStyle(TextMeshProUGUI txt, Style style)
        {
            if (txt == null) return;

            txt.color = style.textColor;
        }

        void ApplyStyleToChildren(GameObject target, Style style)
        {
            foreach (var btn in GetTargets(target, child => child.GetComponent<Button>()))
            {
                ApplyButtonStyle(btn, style);
            }


            foreach (var txt in GetTargets(
                target,
                child => child.GetComponent<TextMeshProUGUI>(),
                filter: StopAtPaletteOrButtonFilter))
            {
                ApplyTextStyle(txt, style);
            }

        }

        public void ApplyRecursively(GameObject target)
        {
            if (offline) return;

            ApplyStyleToChildren(target, baseStyle);

            foreach (var custom in GetComponentsInChildren<UIPaletteCustom>())
            {
                SetCustomPalette(custom);
            }
        }

        private void OnValidate()
        {
            ApplyRecursively(gameObject);
        }

        public void SetCustomPalette(UIPaletteCustom customRoot)
        {
            if (customStyles.Any(cs => cs.styleId == customRoot.StyleId))
            {
                // Debug.Log($"UI Palette: Applying custom style '{customRoot.StyleId}' to '{customRoot.name}'");

                var style = customStyles.First(cs => cs.styleId == customRoot.StyleId).ToStyle(baseStyle);

                if (!ApplyButtonStyle(customRoot.GetComponent<Button>(), style))
                {
                    ApplyTextStyle(customRoot.GetComponent<TextMeshProUGUI>(), style);
                }

                ApplyStyleToChildren(customRoot.gameObject, style);
            }
            else
            {
                Debug.LogWarning($"UI Palette: '{customRoot.name}' asked for custom style '{customRoot.StyleId}', but it doesn't exist");
            }
        }
    }
}
