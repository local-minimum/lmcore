using System.Linq;
using UnityEngine;

namespace LMCore.UI
{
    public delegate void FadeCompleteEvent(FadeUI.FadeDirection direction);

    [RequireComponent(typeof(CanvasGroup))]
    public class FadeUI : MonoBehaviour
    {
        public event FadeCompleteEvent OnFadeComplete;
        public enum FadeDirection { None, In, Out };
        public enum FadeState { Unknown, FadedIn, FadedOut };

        private FadeDirection direction = FadeDirection.None;
        public FadeDirection ActiveFade => direction;

        [SerializeField]
        AnimationCurve fadeIn;

        [SerializeField]
        AnimationCurve fadeOut;

        AnimationCurve activeFade;
        public bool Fading => activeFade != null;

        CanvasGroup _group;
        CanvasGroup group
        {
            get
            {
                if (_group == null)
                {
                    _group = GetComponent<CanvasGroup>();
                }
                return _group;
            }
        }

        public FadeState State
        {
            get
            {
                var currentValue = group.alpha;
                if (fadeIn != null && fadeIn.keys.LastOrDefault().value == currentValue) return FadeState.FadedIn;
                if (fadeOut != null && fadeOut.keys.LastOrDefault().value == currentValue) return FadeState.FadedOut;
                return FadeState.Unknown;
            }
        }

        public void FadeIn() => ConfigureFade(FadeDirection.In);

        public void FadeOut() => ConfigureFade(FadeDirection.Out);

        float animationStart;
        float animationDuration;

        void ConfigureFade(FadeDirection direction)
        {
            this.direction = direction;

            switch (direction)
            {
                case FadeDirection.In:
                    activeFade = fadeIn;
                    break;
                case FadeDirection.Out:
                    activeFade = fadeOut;
                    break;
                default:
                    activeFade = null;
                    break;
            }

            if (activeFade == null) return;

            animationStart = Time.timeSinceLevelLoad;
            animationDuration = activeFade.keys[activeFade.keys.Length - 1].time;
            // Debug.Log($"Fade {direction} on {name} will take {animationDuration}s");
        }

        void UpdateFade()
        {
            if (activeFade == null)
            {
                if (direction == FadeDirection.In)
                {
                    group.alpha = 1f;
                }
                else if (direction == FadeDirection.Out)
                {
                    group.alpha = 0f;
                }

                EndFade();
                return;
            }

            var animationTime = Mathf.Min(animationDuration, Time.timeSinceLevelLoad - animationStart);
            group.alpha = activeFade.Evaluate(animationTime);

            // Debug.Log($"Fading alpha {group.alpha} at {animationTime}s");

            if (animationTime == animationDuration)
            {
                EndFade();
            }
        }

        void EndFade()
        {
            // Debug.Log($"Fade {direction} on {name} comleted");
            var activeDirection = direction;

            activeFade = null;
            direction = FadeDirection.None;
            OnFadeComplete?.Invoke(activeDirection);
        }

        private void Update()
        {
            if (direction != FadeDirection.None) UpdateFade();
        }
    }
}
