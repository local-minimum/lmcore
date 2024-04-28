using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LMCore.UI
{
    [System.Serializable]
    public struct SlideshowFrame
    {
        public float time;
        public Sprite sprite;
        [TextArea]
        public string subtitle;

        public SlideshowFrame(float time, Sprite sprite, string subtitle)
        {
            this.time = time;
            this.sprite = sprite;
            this.subtitle = subtitle;
        }

        public SlideshowFrame(float time, Sprite sprite)
        {
            this.time = time;
            this.sprite = sprite;
            subtitle = null;
        }

        public SlideshowFrame(float time, string subtitle)
        {
            this.time = time;
            sprite = null;
            this.subtitle = subtitle;
        }

        public SlideshowFrame(float time)
        {
            this.time = time;
            sprite = null;
            subtitle = null;
        }
    }

    public class Slideshow : MonoBehaviour
    {
        [SerializeField]
        string NextScene = "Menu";

        [SerializeField]
        SlideshowFrame[] frames;

        [SerializeField, Range(0, 2)]
        float InitialDelay;

        int frameIndex = 0;

        [SerializeField]
        Image MainImage;

        [SerializeField]
        TextMeshProUGUI Subtitle;

        [SerializeField]
        SimpleButton SkipButton;

        [SerializeField]
        string ButtonTextAfterShow = "Leave";

        [SerializeField]
        AudioSource Speaker;

        void UpdateFrame(SlideshowFrame frame)
        {
            if (frame.sprite != null)
            {
                MainImage.sprite = frame.sprite;
                if (!MainImage.enabled)
                {
                    MainImage.enabled = true;
                }
            }
            if (!string.IsNullOrEmpty(frame.subtitle) || frame.sprite == null && frame.subtitle != null)
            {
                Subtitle.text = frame.subtitle;
            }
        }

        private void OnEnable()
        {
            MainImage.enabled = false;
            Subtitle.text = "";
        }

        private void Update()
        {
            if (frameIndex >= frames.Length) { return; }

            if (Time.timeSinceLevelLoad < InitialDelay)
            {
                return;
            }
            else if (!Speaker.isPlaying && frameIndex == 0)
            {
                Speaker.Play();
            }

            var frame = frames[frameIndex];
            if (Time.timeSinceLevelLoad > frame.time + InitialDelay)
            {
                UpdateFrame(frame);
                frameIndex++;

                if (frameIndex == frames.Length)
                {
                    SkipButton.Text = ButtonTextAfterShow;
                }
            }
        }

        public void DoSkipContinue()
        {
            SceneManager.LoadScene(NextScene);
        }
    }
}