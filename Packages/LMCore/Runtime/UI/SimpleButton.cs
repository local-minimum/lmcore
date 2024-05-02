using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LMCore.UI
{
    public class SimpleButton : MonoBehaviour
    {
        [SerializeField]
        private Color DefaultColor;

        [SerializeField]
        private Color SelectedColor;

        private TextMeshProUGUI _colorTarget;

        private TextMeshProUGUI ColorTarget
        {
            get
            {
                if (_colorTarget == null)
                {
                    _colorTarget = GetComponentInChildren<TextMeshProUGUI>();
                }
                return _colorTarget;
            }
        }

        public string Text
        {
            get => ColorTarget?.text;
            set
            {
                if (ColorTarget)
                {
                    ColorTarget.text = value;
                }
            }
        }

        [SerializeField]
        private UnityEvent OnClick;

        private SimpleButtonGroup _group;

        private SimpleButtonGroup Group
        {
            get
            {
                if (_group == null)
                {
                    _group = GetComponentInParent<SimpleButtonGroup>();
                }
                return _group;
            }
        }

        public void Selected()
        {
            Debug.Log($"Selected {name}");
            if (ColorTarget != null)
            {
                ColorTarget.color = SelectedColor;
            }
            Group.Selected = this;
        }

        public void DeSelect()
        {
            Debug.Log($"Deselected {name}");
            if (ColorTarget != null)
            {
                ColorTarget.color = DefaultColor;
            }
        }

        public void Click()
        {
            Debug.Log($"Clicked {name}");
            OnClick?.Invoke();
        }
    }
}