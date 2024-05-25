using UnityEngine;
using LMCore.Juice;

namespace LMCore.UI
{
    [ExecuteInEditMode]
    public class TwoSidedUI : MonoBehaviour
    {
        [SerializeField]
        GameObject front;

        [SerializeField]
        GameObject back;

        [SerializeField]
        TemporalEasing<float> easeToBack;

        [SerializeField]
        TemporalEasing<float> easeToFront;

        bool FrontVisible(float yRotation) => yRotation < 90f || yRotation > 270f;
        bool FrontVisible() => FrontVisible(transform.localEulerAngles.y);

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

        public void EaseFlip()
        {
            activeEasing = FrontVisible() ? easeToBack : easeToFront;
            activeEasing.EaseStartToEnd();
        }

        [ContextMenu("Normalize")]
        public void Normalize() => SetRotation(FrontVisible() ? 0 : 180);

        void Update()
        {
            if (activeEasing?.IsEasing == true)
            {
                SetRotation(activeEasing.Evaluate());
            }

            var showFront = FrontVisible(transform.eulerAngles.y);

            if (front != null) front.SetActive(showFront);
            if (back != null) back.SetActive(!showFront);
        }
    }
}
