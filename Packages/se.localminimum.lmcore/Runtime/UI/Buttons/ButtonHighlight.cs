using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class ButtonHighlight : MonoBehaviour
    {
        public delegate void HighlightEvent(Button button);
        public static event HighlightEvent OnSelected;
        public static event HighlightEvent OnHoverEnter;
        public static event HighlightEvent OnHoverExit;

        private static Button lastSelectedButton;
        private static Button lastHoveredButton;

        public enum Status { Normal, Selected, Hovered, Pressed };

        bool hovered;
        bool selected;
        bool pressed;

        ButtonGroup group => GetComponentInParent<ButtonGroup>();

        Status status
        {
            get
            {
                if (pressed) return Status.Pressed;

                var g = group;
                if (g != null) return g.CalculateStatus(button);

                if (hovered) return Status.Hovered;
                if (selected) return Status.Selected;
                return Status.Normal;
            }
        }

        [SerializeField]
        Graphic target;

        [SerializeField]
        private ColorBlock _colors;
        public ColorBlock colors
        {
            get => _colors;
            set
            {
                _colors = value;
                SyncColor(true);
            }
        }

        private Button _button;
        public Button button
        {
            get
            {
                if (_button == null)
                {
                    _button = GetComponentInParent<Button>(true);
                }
                return _button;
            }
        }

        private EventTrigger trigger
        {
            get
            {
                var btn = button;
                if (btn == null)
                {
                    Debug.LogError(PrefixLogMessage("Doesn't have a button as a parent"));
                    return null;
                }
                var trigger = btn.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = button.gameObject.AddComponent<EventTrigger>();
                }
                return trigger;
            }
        }

        string PrefixLogMessage(string message) =>
                $"ButtonHighlight ({status}) '{name}' of '{(button == null ? "[NONE]" : button.name)}': {message}";

        private void OnEnable()
        {
            if (trigger == null)
            {
                Debug.LogError(PrefixLogMessage($"Doesn't have a button as a parent"));
                return;
            }

            AddListenerToTrigger(EventTriggerType.PointerEnter, HandleHover);
            AddListenerToTrigger(EventTriggerType.PointerExit, HandleDeHover);
            AddListenerToTrigger(EventTriggerType.Select, HandleSelect);
            AddListenerToTrigger(EventTriggerType.Deselect, HandleDeSelect);
            AddListenerToTrigger(EventTriggerType.PointerDown, HandlePressed);
            AddListenerToTrigger(EventTriggerType.PointerUp, HandleDePressed);

            if (button is LMButton)
            {
                var lmButton = button as LMButton;
                lmButton.OnInteractableChange += LmButton_OnInteractableChange;
            }

            OnSelected += ButtonHighlight_OnSelected;
            OnHoverEnter += ButtonHighlight_OnHoverEnter;
            OnHoverExit += ButtonHighlight_OnHoverExit;

            var buttonGroup = GetComponentInParent<ButtonGroup>();
            if (buttonGroup != null)
            {
                buttonGroup.Register(this);
            }

            selected = EventSystem.current.currentSelectedGameObject == button.gameObject;
            Debug.Log(PrefixLogMessage("Awakened"));
            SyncColor(true, true);
        }

        private void OnDisable()
        {
            var eventTrigger = trigger;
            if (eventTrigger == null)
            {
                Debug.LogError(PrefixLogMessage("Doesn't have a button as a parent"));
            }

            foreach (var entry in triggerEntries)
            {
                eventTrigger.triggers.Remove(entry);
            }

            if (button is LMButton)
            {
                var lmButton = button as LMButton;
                lmButton.OnInteractableChange -= LmButton_OnInteractableChange;
            }

            OnSelected -= ButtonHighlight_OnSelected;
            OnHoverEnter -= ButtonHighlight_OnHoverEnter;
            OnHoverExit -= ButtonHighlight_OnHoverExit;

            hovered = false;
            selected = false;
            pressed = false;
        }

        private void LmButton_OnInteractableChange(bool interactable)
        {
            SyncColor();
        }

        private void ButtonHighlight_OnSelected(Button button)
        {
            // This callback is here to handle other buttons
            // not the one that got the event, that is already
            // handled in the HandleX callback
            if (selected && button != this.button)
            {
                selected = false;
                Debug.Log(PrefixLogMessage("I'm no longer selected"));
                SyncColor();
            }
        }

        private void ButtonHighlight_OnHoverExit(Button button)
        {
            // This callback is here to handle other buttons
            // not the one that got the event, that is already
            // handled in the HandleX callback
            if (selected && button != this.button)
            {
                SyncColor();
            }
        }

        private void ButtonHighlight_OnHoverEnter(Button button)
        {
            // This callback is here to handle other buttons
            // not the one that got the event, that is already
            // handled in the HandleX callback
            if (selected && button != this.button)
            {
                SyncColor();
            }
        }

        List<EventTrigger.Entry> triggerEntries = new();

        void AddListenerToTrigger(
            EventTriggerType eventID,
            System.Action callback)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = eventID;
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
            triggerEntries.Add(entry);
        }

        private void HandleHover()
        {
            hovered = true;
            SyncColor();

            if (lastHoveredButton != button)
            {
                lastHoveredButton = button;
                OnHoverEnter?.Invoke(button);
            }
        }

        private void HandleDeHover()
        {
            hovered = false;
            SyncColor();

            if (lastHoveredButton == button)
            {
                lastHoveredButton = null;
                OnHoverExit?.Invoke(button);
            }
        }

        private void HandleSelect()
        {
            selected = true;

            if (lastSelectedButton != button)
            {
                // Debug.Log(PrefixLogMessage("Reporting my button as selected"));

                lastSelectedButton = button;
                OnSelected?.Invoke(button);
            }

            SyncColor();
        }

        private void HandleDeSelect()
        {
            if (group != null) return;

            selected = false;

            if (lastSelectedButton == button)
            {
                lastSelectedButton = null;
                OnSelected?.Invoke(null);
            }

            SyncColor();
        }

        private void HandlePressed()
        {
            pressed = true;
            SyncColor();
        }

        private void HandleDePressed()
        {
            pressed = false;
            SyncColor();
        }

        Color targetColor
        {
            get
            {
                if (colors == null) return Color.white;

                var btn = button;
                if (btn == null)
                {
                    Debug.LogError(PrefixLogMessage("Doesn't have a button as a parent"));
                    return Color.white;
                }

                if (!btn.interactable) return colors.disabledColor;

                switch (status)
                {
                    case Status.Hovered:
                        return colors.highlightedColor;
                    case Status.Pressed:
                        return colors.pressedColor;
                    case Status.Selected:
                        return colors.selectedColor;
                    default:
                        return colors.normalColor;
                }
            }
        }

        Color lastColor;

        [ContextMenu("Info")]
        void Info() =>
            Debug.Log(PrefixLogMessage($"Last selected({lastSelectedButton}) Last hovered({lastHoveredButton})"));

        [ContextMenu("Update colors")]
        void UpdateColors() => SyncColor(true);

        string NameColor(Color color, bool scaled)
        {
            if (color == (scaled ? colors.selectedColor * colors.colorMultiplier : colors.selectedColor)) return "<Selected Color>";
            if (color == (scaled ? colors.disabledColor * colors.colorMultiplier : colors.disabledColor)) return "<Disabled Color>";
            if (color == (scaled ? colors.pressedColor * colors.colorMultiplier : colors.pressedColor)) return "<Pressed Color>";
            if (color == (scaled ? colors.highlightedColor * colors.colorMultiplier : colors.highlightedColor)) return "<Highlight Color>";
            if (color == (scaled ? colors.normalColor * colors.colorMultiplier : colors.normalColor)) return "<Normal Color>";
            return "<Random Color>";
        }

        public void SyncColor(bool instant = false, bool force = false)
        {
            if (target == null)
            {
                Debug.LogWarning(PrefixLogMessage("Ignoring color sync because missing target graphics"));
                return;
            }

            var color = targetColor * colors.colorMultiplier;
            // Debug.Log(PrefixLogMessage($"Syncing color {NameColor(color, true)}, current color {NameColor(lastColor, true)}"));

            if (lastColor == color && !force)
            {
                // Debug.Log(PrefixLogMessage($"Ignoring color {NameColor(color, true)} because it's identical to my current color {NameColor(lastColor, true)}"));
                return;
            }

            target.CrossFadeColor(
                color,
                instant || colors.fadeDuration < 0 ? 0 : colors.fadeDuration,
                true,
                true);

            lastColor = color;
        }

        [ContextMenu("Copy from button")]
        void CopyFromButton()
        {
            _colors = button.colors;
        }
    }
}
