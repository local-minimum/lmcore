using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LMCore.UI
{
    public class ButtonGroup : MonoBehaviour
    {
        List<Button> buttons = new List<Button>();
        List<ButtonHighlight> highlights = new List<ButtonHighlight>();

        string PrefixLogMessage(string message) =>
                $"ButtonGroup '{name}' Hovered({(hovered == null ? "[NULL]" : hovered.name)}) Selected({(selected == null ? "[NULL]" : selected.name)}) {lastFocus}: {message}";

        public void Register(ButtonHighlight highlight)
        {
            if (highlight.button != null && !buttons.Contains(highlight.button))
            {
                buttons.Add(highlight.button);
            }

            if (!highlights.Contains(highlight))
            {
                highlights.Add(highlight);
            }
        }

        private void OnEnable()
        {
            highlights.AddRange(GetComponentsInChildren<ButtonHighlight>(true));

            ButtonHighlight.OnSelected += ButtonHighlight_OnSelected;
            ButtonHighlight.OnHoverEnter += ButtonHighlight_OnHoverEnter;
            ButtonHighlight.OnHoverExit += ButtonHighlight_OnHoverExit;
        }

        private void OnDisable()
        {
            highlights.Clear();

            ButtonHighlight.OnSelected -= ButtonHighlight_OnSelected;
            ButtonHighlight.OnHoverEnter -= ButtonHighlight_OnHoverEnter;
            ButtonHighlight.OnHoverExit -= ButtonHighlight_OnHoverExit;
        }

        private enum FocusType { None, Selected, Hovered };

        FocusType lastFocus = FocusType.None;

        Button selected;
        Button hovered;

        public ButtonHighlight.Status CalculateStatus(Button button)
        {
            if (!buttons.Contains(button))
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying)
                {
#endif
                    Debug.LogWarning(PrefixLogMessage($"Don't know of button '{button.name}'"));
#if UNITY_EDITOR
                }
#endif
                return ButtonHighlight.Status.Normal;
            }

            if (lastFocus == FocusType.Selected && button == selected) return ButtonHighlight.Status.Selected;
            if (lastFocus == FocusType.Hovered && button == hovered) return ButtonHighlight.Status.Hovered;

            return ButtonHighlight.Status.Normal;
        }

        private void ButtonHighlight_OnHoverExit(Button button)
        {
            if (hovered == button)
            {
                hovered = null;

                lastFocus = selected == null ? FocusType.None : FocusType.Selected;

                Debug.Log(PrefixLogMessage("Hover status purged from group"));
            }

            UpdateHighlights();
        }

        private void ButtonHighlight_OnHoverEnter(Button button)
        {
            if (hovered == button) return;

            if (buttons.Contains(button))
            {
                hovered = button;
                lastFocus = FocusType.Hovered;

                Debug.Log(PrefixLogMessage("Button gained hover"));
            }

            UpdateHighlights();
        }

        private void ButtonHighlight_OnSelected(Button button)
        {
            if (buttons.Contains(button))
            {
                selected = button;
                lastFocus = FocusType.Selected;
            }
            else
            {
                selected = null;
                lastFocus = hovered == null ? FocusType.None : FocusType.Hovered;
            }

            UpdateHighlights();
        }

        public void ForceSyncSelected(bool forgetHover = true)
        {
            var current = EventSystem.current.currentSelectedGameObject;
            var currentBtn = current != null ? current.GetComponent<Button>() : null;

            if (forgetHover)
            {
                hovered = null;
            }

            if (currentBtn != selected)
            {
                if (currentBtn != null && buttons.Contains(currentBtn))
                {
                    selected = currentBtn;
                    lastFocus = FocusType.Selected;
                }
                else
                {
                    selected = null;
                    if (lastFocus == FocusType.Selected)
                    {
                        lastFocus = hovered == null ? FocusType.None : FocusType.Hovered;
                    }
                }
            }
            else
            {
                lastFocus = FocusType.Selected;
            }

            UpdateHighlights();
        }

        void UpdateHighlights()
        {
            foreach (var highlight in highlights)
            {
                highlight.SyncColor(force: true);
            }
        }
    }
}
