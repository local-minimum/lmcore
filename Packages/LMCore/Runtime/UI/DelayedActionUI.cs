using LMCore.AbstractClasses;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class DelayedActionUI : Singleton<DelayedActionUI, DelayedActionUI>
    {
        [SerializeField]
        private TMPro.TextMeshProUGUI TextGUI;

        [SerializeField]
        private Image ProgressImage;

        [SerializeField]
        private GameObject DisplayRoot;

        private System.Action onWaitDone;
        private float waitStart;
        private float waitDuration;

        public void ShowMessage(string message, System.Action onWaitDone, float waitDuration = 1)
        {
            waitStart = Time.timeSinceLevelLoad;
            this.waitDuration = Mathf.Max(waitDuration, 0.0001f);
            TextGUI.text = message;
            this.onWaitDone = onWaitDone;
            ProgressImage.fillAmount = 0;
            DisplayRoot.SetActive(true);
        }

        public void CancelMessage(string message)
        {
            if (message == TextGUI.text)
            {
                ClearMessage();
            }
        }

        private void ClearMessage()
        {
            onWaitDone = null;
            DisplayRoot.SetActive(false);
        }

        private void Start()
        {
            if (onWaitDone == null) ClearMessage();
        }

        private void Update()
        {
            if (onWaitDone == null) { return; }

            var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - waitStart) / waitDuration);

            if (progress == 1)
            {
                onWaitDone();
                ClearMessage();
            }
            else
            {
                ProgressImage.fillAmount = progress;
            }
        }
    }
}