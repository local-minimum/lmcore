using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LMCore.UI
{
    public class TooltipUI : MonoBehaviour
    {
        protected List<AbsFitter> _fitters;
        protected List<AbsFitter> fitters
        {
            get
            {
                if (_fitters == null)
                {
                    _fitters = GetComponents<AbsFitter>().ToList();
                }

                return _fitters;
            }
        }

        [SerializeField]
        protected TextMeshProUGUI Title;

        [SerializeField]
        protected RectTransform Content;

        [SerializeField]
        float anchorOffset = 5;

        [SerializeField]
        FadeUI fade;

        private void OnEnable()
        {
            foreach (var fitter in fitters)
            {
                fitter.OnRecalculateSize += ReCalculateAnchor;
            }

            if (fade != null)
            {
                fade.OnFadeComplete += Fade_OnFadeComplete;
            }
        }

        private void OnDisable()
        {
            foreach (var fitter in fitters)
            {
                fitter.OnRecalculateSize += ReCalculateAnchor;
            }

            if (fade != null)
            {
                fade.OnFadeComplete -= Fade_OnFadeComplete;
            }
        }

        private void Fade_OnFadeComplete(FadeUI.FadeDirection direction)
        {
            if (direction == FadeUI.FadeDirection.Out)
            {
                gameObject.SetActive(false);
            }
        }

        protected RectTransform anchor;

        /// <summary>
        /// Anchor transform to supplied anchor with an offset
        /// </summary>
        /// <param name="anchor">Anchor to align to</param>
        public void Anchor(RectTransform anchor)
        {
            this.anchor = anchor;
        }

        protected void ReCalculateAnchor()
        {
            if (anchor == null) return;

            var min = anchor.rect.min;
            var max = anchor.rect.max;

            var myRectTransform = transform as RectTransform;

            // Above to the rigth
            Anchor(anchor, myRectTransform, 0, 0, min, max);

            // Validate position by checking its corners fall inside canvas
            var corners = new Vector3[4];
            myRectTransform.GetWorldCorners(corners);
            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = (canvas.transform as RectTransform).rect;
            bool requireRelocation = false;
            int xPivot = 0;
            int yPivot = 0;

            for (int i = 0; i < 4; i++)
            {
                var corner = canvas.transform.InverseTransformPoint(corners[i]);
                if (!canvasRect.Contains(corner))
                {
                    if (xPivot == 0 && corner.x > canvasRect.xMax)
                    {
                        xPivot = 1;
                    }
                    if (yPivot == 0 && corner.y > canvasRect.yMax)
                    {
                        yPivot = 1;
                    }
                    requireRelocation = true;
                }
            }

            if (requireRelocation)
            {
                Anchor(anchor, myRectTransform, xPivot, yPivot, min, max);
            }
        }

        void Anchor(RectTransform anchor, RectTransform rt, int xPivot, int yPivot, Vector2 min, Vector2 max)
        {
            rt.pivot = new Vector2(xPivot, yPivot);
            var pos = anchor.transform.position;
            pos.x += xPivot == 1 ? max.x : min.x;
            pos.y += yPivot == 1 ? min.y - anchorOffset : max.y + anchorOffset;
            rt.position = pos;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (fade != null)
            {
                fade.FadeIn();
            }
        }

        public void Hide()
        {
            if (fade != null)
            {
                fade.FadeOut();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

    }
}
