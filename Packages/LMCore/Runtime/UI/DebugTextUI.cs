using TMPro;
using LMCore.AbstractClasses;

namespace LMCore.UI
{
    public class DebugTextUI : Singleton<DebugTextUI>
    {

        TextMeshProUGUI TextUI;
        void Start()
        {
            TextUI = GetComponentInChildren<TextMeshProUGUI>();
        }

        public string Text
        {
            get { return TextUI?.text; }
            set { 
                if (TextUI != null)
                {
                    TextUI.text = value;
                }                
            }
        }
    }
}
