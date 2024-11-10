using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    /// <summary>
    /// A Gradient Bar is a progress bar that doesn't tell the truth
    /// </summary>
    public class GradientBar : MonoBehaviour
    {
        [SerializeField, Tooltip("The bar which uses fill to show 0-1 progres")]
        private Image Bar;

        [SerializeField, Tooltip("Must start at 0 and go to 1 for both axis.")]
        private AnimationCurve gradient;

        private int _MaxValue;

        public int MaxValue
        {
            get => _MaxValue;
            set
            {
                _MaxValue = value;
                UpdateGradient();
            }
        }

        private int _CurrentValue;

        public int CurrentValue
        {
            get => _CurrentValue;
            set
            {
                _CurrentValue = value;
                UpdateGradient();
            }
        }

        private void UpdateGradient() => UpdateGradient(Mathf.Clamp01((float)CurrentValue / MaxValue));

        private void UpdateGradient(float progress)
        {
            Bar.fillAmount = gradient.Evaluate(progress);
        }
    }
}