using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class FloatGradientImageFill : MonoBehaviour
    {
        [SerializeField]
        Image image;

        [SerializeField]
        AnimationCurve gradient;

        [SerializeField]
        float startFill = 0;

        float fill;
        public float Fill
        {
            get => fill;
            set
            {
                fill = value;
                image.fillAmount = gradient.Evaluate(value);
            }
        }

        private void Start()
        {
            Fill = startFill;
        }
    }
}
