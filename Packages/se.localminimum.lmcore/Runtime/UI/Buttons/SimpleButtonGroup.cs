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

        [SerializeField]
        bool Navigational = true;

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

        private void RegisterCallbacks()
        {
            throw new System.NotImplementedException("Simple buttons aren't really used anymore, this isn't supported. Use LMButton or just a Button");
        }

        private void UnregisterCallbacks()
        {
            throw new System.NotImplementedException("Simple buttons aren't really used anymore, this isn't supported. Use LMButton or just a Button");
        }

        public void DoInteract(InputAction.CallbackContext context)
        {
            if (!Navigational) return;
            Debug.Log("Interact button clicked");
            Selected?.Click();
        }

        public void DoUp(InputAction.CallbackContext context)
        {
            if (!Navigational) return;

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
            if (!Navigational) return;

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
            if (!Navigational) return;

            var defaultBtn = VisibleButtons.FirstOrDefault();
            defaultBtn?.Selected();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }


        private bool _interactable = true;
        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                foreach (var button in VisibleButtons)
                {
                    button.enabled = value;
                }
            }
        }
    }
}