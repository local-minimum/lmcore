using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LMCore.UI
{
    public delegate void SelectButtonEvent(SimpleButton selected);

    public class SimpleButtonGroup : MonoBehaviour
    {
        public event SelectButtonEvent OnSelectButton;

        [SerializeField]
        private SimpleButton[] Buttons;

        private List<SimpleButton> VisibleButtons => Buttons.Where(b => b.gameObject.activeSelf).ToList();

        [SerializeField]
        private bool WrapSelection = true;

        private SimpleButton _Selected;

        public SimpleButton Selected
        {
            get => _Selected;
            set
            {
                if (value != _Selected)
                {
                    _Selected?.DeSelect();
                }
                _Selected = value;
                OnSelectButton?.Invoke(value);
            }
        }

        /// <summary>
        /// Inputs/North (Forward)
        /// </summary>
        private const string upActionId = "00703aab-a4e6-4870-9afd-59a0a6bb99fa";

        /// <summary>
        /// Inputs/South (Backwards)
        /// </summary>
        private const string downActionId = "324d3a1c-59af-4607-a1c7-87629a3a9903";

        /// <summary>
        /// Inputs/Interact event
        /// </summary>
        private const string interactActionId = "911be32a-e601-4f55-bcb3-49f94aa29fff";

        private PlayerInput _input;

        private PlayerInput Input
        {
            get
            {
                if (_input == null)
                {
                    _input = FindAnyObjectByType<PlayerInput>();
                }
                return _input;
            }
        }

        private void RegisterCallbacks()
        {
            if (Input == null)
            {
                Debug.LogWarning("No keybindings");
                return;
            }

            foreach (var evt in Input.actionEvents)
            {
                switch (evt.actionId)
                {
                    case interactActionId:
                        evt.AddListener(DoInteract);
                        break;

                    case upActionId:
                        evt.AddListener(DoUp);
                        break;

                    case downActionId:
                        evt.AddListener(DoDown);
                        break;

                    default:
                        Debug.Log($"Not assigning {evt.actionId} ({evt.actionName})");
                        break;
                }
            }
        }

        private void UnregisterCallbacks()
        {
            if (Input == null) return;

            foreach (var evt in Input.actionEvents)
            {
                switch (evt.actionId)
                {
                    case interactActionId:
                        evt.RemoveListener(DoInteract);
                        break;

                    case upActionId:
                        evt.RemoveListener(DoUp);
                        break;

                    case downActionId:
                        evt.RemoveListener(DoDown);
                        break;
                }
            }
        }

        public void DoInteract(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Selected?.Click();
        }

        public void DoUp(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Debug.Log("Up");
            var selectedIdx = VisibleButtons.IndexOf(Selected);
            if (selectedIdx < 0)
            {
                VisibleButtons.GetNthOrDefault(0, null)?.Selected();
            }
            else if (selectedIdx == 0)
            {
                if (WrapSelection)
                {
                    VisibleButtons.LastOrDefault()?.Selected();
                }
            }
            else
            {
                VisibleButtons[selectedIdx - 1]?.Selected();
            }
        }

        public void DoDown(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Debug.Log("Down");
            var selectedIdx = VisibleButtons.IndexOf(Selected);
            if (selectedIdx < 0)
            {
                VisibleButtons.LastOrDefault()?.Selected();
            }
            else if (WrapSelection)
            {
                VisibleButtons.GetWrappingNth(selectedIdx + 1)?.Selected();
            }
            else
            {
                VisibleButtons.GetNthOrLast(selectedIdx + 1)?.Selected();
            }
        }

        private void OnEnable()
        {
            SelectDefault();
            RegisterCallbacks();
        }

        public void SelectDefault()
        {
            var defaultBtn = VisibleButtons.FirstOrDefault();
            defaultBtn?.Selected();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }
    }
}