using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class UIAlert : MonoBehaviour, IPaletteBlocker
    {
        [System.Serializable]
        private struct AlertStyle
        {
            public Sprite icon;
            public Color iconColor;
            public Color textColor;
            public Color primaryColor;
            public Color highlightColor;
        }

        public enum Severity { Success, Info, Warning, Error, Critical };

        [SerializeField]
        Severity _severity;
        public Severity severity
        {
            get => _severity;
            private set
            {
                _severity = value;
                Sync();
            }
        }

        [Header("Components")]
        [SerializeField]
        TextMeshProUGUI message;

        [SerializeField]
        Image primaryImage;

        [SerializeField]
        Image highlightImage;

        [SerializeField]
        Image iconImage;

        [Header("Styling")]
        [SerializeField]
        AlertStyle successStyle;

        [SerializeField]
        AlertStyle infoStyle;

        [SerializeField]
        AlertStyle warningStyle;

        [SerializeField]
        AlertStyle errorStyle;

        [SerializeField]
        AlertStyle criticalStyle;

        public void Configure(string message, Severity severity = Severity.Info)
        {
            this.message.text = message;
            this.message.gameObject.SetActive(!string.IsNullOrEmpty(message));
            this.severity = severity;
        }

        private void OnValidate()
        {
            Sync();
        }

        AlertStyle Style
        {
            get
            {
                switch (severity)
                {
                    case Severity.Success:
                        return successStyle;
                    case Severity.Warning:
                        return warningStyle;
                    case Severity.Error:
                        return errorStyle;
                    case Severity.Critical:
                        return criticalStyle;
                    default:
                        return infoStyle;

                }
            }
        }

        [ContextMenu("Sync")]
        void Sync()
        {
            var style = Style;
            if (iconImage != null)
            {
                if (style.icon == null)
                {
                    iconImage.gameObject.SetActive(false);
                }
                else
                {
                    iconImage.sprite = style.icon;
                    iconImage.color = style.iconColor;
                    iconImage.gameObject.SetActive(true);
                }
            }

            if (message != null)
            {
                message.gameObject.SetActive(!string.IsNullOrEmpty(message.text));
                message.color = style.textColor;
            }

            if (primaryImage != null)
            {
                primaryImage.color = style.primaryColor;
            }

            if (highlightImage != null)
            {
                highlightImage.color = style.highlightColor;
            }
        }
    }
}
