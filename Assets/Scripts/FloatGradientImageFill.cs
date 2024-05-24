using UnityEngine;
using UnityEngine.UI;

public class FloatGradientImageFill : MonoBehaviour
{
    [SerializeField]
    Image image;

    [SerializeField]
    AnimationCurve gradient;

    public float Fill
    {
        get => image.fillAmount;
        set => image.fillAmount = gradient.Evaluate(value);
    }
}
