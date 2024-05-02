using LMCore.AbstractClasses;
using TMPro;

namespace LMCore.UI
{
    public class DebugTextUI : Singleton<DebugTextUI>
    {
        private TextMeshProUGUI TextUI;

        private void Start()
        {
            TextUI = GetComponentInChildren<TextMeshProUGUI>();
        }

        public string Text
        {
            get { return TextUI?.text; }
            set
            {
                if (TextUI != null)
                {
                    TextUI.text = value;
                }
            }
        }
    }
}