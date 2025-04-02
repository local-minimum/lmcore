using UnityEngine;

namespace LMCore.UI
{
    [RequireComponent(typeof(FadeUI))]
    public class FadeUIAtDistance : MonoBehaviour
    {
        [SerializeField, Tooltip("If empty uses main camera")]
        Camera myCamera;

        [SerializeField]
        float fadeOutAtDistance;
        [SerializeField]
        float fadeInAtDistance;

        Camera cam => myCamera == null ? Camera.main : myCamera;

        FadeUI _fader;
        FadeUI fader
        {
            get
            {
                if (_fader == null)
                {
                    _fader = GetComponent<FadeUI>();
                }
                return _fader;
            }
        }

        Canvas _canvas;
        Canvas canvas
        {
            get
            {
                if (_canvas == null)
                {
                    _canvas = GetComponentInParent<Canvas>(true);
                }
                return _canvas;
            }
        }

        private void Update()
        {
            var distance = (canvas.transform.position - cam.transform.position).magnitude;
            var state = fader.State;
            if (distance > fadeOutAtDistance && state != FadeUI.FadeState.FadedOut && fader.ActiveFade != FadeUI.FadeDirection.Out)
            {
                fader.FadeOut();
            }
            else if (distance < fadeInAtDistance && state != FadeUI.FadeState.FadedIn && fader.ActiveFade != FadeUI.FadeDirection.In)
            {
                fader.FadeIn();
            }
        }
    }
}
