using LMCore.Juice;
using UnityEngine;

namespace LMCore.UI
{
    [ExecuteInEditMode]
    public class TwoSidedUI : MonoBehaviour
    {
        [SerializeField]
        GameObject front;
        public GameObject Front => front;

        [SerializeField]
        GameObject back;
        public GameObject Back => back;

        [SerializeField]
        TemporalEasing<float> easeToBack;

        [SerializeField]
        TemporalEasing<float> easeToFront;

        bool FrontVisible(float yRotation) => yRotation < 90f || yRotation > 270f;
        /// <summary>
        /// If front can be seen in any way, might be turning to the back side
        /// </summary>
        public bool FrontVisible() => FrontVisible(transform.localEulerAngles.y);
        /// <summary>
        /// Front is visible and there's on easing turning the card around
        /// </summary>
        public bool FrontVisibleResting() => activeEasing == null && FrontVisible();
        /// <summary>
        /// If it is easing to either side
        /// </summary>
        public bool Animating() => activeEasing != null;

        void SetRotation(float angle)
        {
            var euler = transform.localEulerAngles;
            euler.y = angle;
            transform.localEulerAngles = euler;
        }

        [ContextMenu("Show Front")]
        public void ShowFront() => SetRotation(0f);

        [ContextMenu("Show Back")]
        public void ShowBack() => SetRotation(180f);

        [ContextMenu("Flip")]
        public void Flip() => SetRotation(FrontVisible() ? 180 : 0);

        TemporalEasing<float> activeEasing;

        // Called by unity event!
        public void EaseFlip()
        {
            activeEasing = FrontVisible() ? easeToBack : easeToFront;
            activeEasing.EaseStartToEnd();
        }

        [ContextMenu("Normalize")]
        public void Normalize() => SetRotation(FrontVisible() ? 0 : 180);

        bool hidden = false;
        public void Hide()
        {
            hidden = true;
            if (front != null) front.SetActive(false);
            if (back != null) back.SetActive(false);
        }

        public void Show()
        {
            hidden = false;
        }

        public bool Showing => !hidden;

        void Update()
        {
            if (hidden) return;

            if (activeEasing?.IsEasing == true)
            {
                SetRotation(activeEasing.Evaluate());
            }
            else if (activeEasing != null)
            {
                Normalize();
                activeEasing = null;
            }

            var showFront = FrontVisible(transform.eulerAngles.y);

            if (front != null && front.activeSelf != showFront) front.SetActive(showFront);
            if (back != null && back.activeSelf == showFront) back.SetActive(!showFront);
        }
    }
}
