using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class FloatGradientImageFill : MonoBehaviour
    {
        [SerializeField]
        Image image;

        [SerializeField, Tooltip("Material must have a _Progress float variable which gets set instead of the image's fillAmount")]
        bool fillInMaterial;

        [SerializeField]
        Material customMat;

        [SerializeField]
        AnimationCurve gradient;

        [SerializeField, Range(0, 1)]
        float startFill = 0;

        float fill;
        bool instancedMat;
        public float Fill
        {
            get => fill;
            set
            {
                fill = value;
                if (fillInMaterial)
                {
                    if (!instancedMat)
                    {
                        image.material = Instantiate(customMat);
                        instancedMat = true;
                    }
                    image.material.SetFloat("_Progress", value);
                }
                else
                {
                    image.fillAmount = gradient.Evaluate(value);
                }
            }
        }

        [ContextMenu("Set fill to start-fill")]

        void OnEnable()
        {
            Fill = startFill;
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"{image.name} ({(fillInMaterial ? "Mat" : "Img")}) is {Fill} filled.");
        }
    }
}
