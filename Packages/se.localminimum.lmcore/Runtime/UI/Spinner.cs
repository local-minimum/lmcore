using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class Spinner : MonoBehaviour
    {
        [SerializeField]
        private Color NoColor;

        [SerializeField]
        private Color FullColor;

        [SerializeField]
        private AnimationCurve SpinnerColorLerp;

        [SerializeField]
        private AnimationCurve SpinningSpeed;

        [SerializeField]
        private float SpinningSpeedMultiplier = 1;

        [SerializeField]
        private float SpinDuration = 2f;

        private float SpinStart;

        [SerializeField]
        private Image Img;

        private bool Spinning;
        private Quaternion StartRotation;

        public void Spin(float spinDuration = -1f)
        {
            SpinStart = Time.timeSinceLevelLoad;
            if (spinDuration > 0f)
            {
                SpinDuration = spinDuration;
            }
            Spinning = true;
        }

        public void Stop()
        {
            Spinning = false;
            Img.color = NoColor;
            Img.transform.rotation = StartRotation;
        }

        private void Start()
        {
            Img.color = NoColor;
            StartRotation = Img.transform.rotation;
        }

        private void Update()
        {
            if (!Spinning) { return; }

            var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - SpinStart) / SpinDuration);

            Img.color = Color.Lerp(NoColor, FullColor, SpinnerColorLerp.Evaluate(progress));
            Img.transform.Rotate(new Vector3(0, 0, SpinningSpeed.Evaluate(progress) * SpinningSpeedMultiplier * Time.deltaTime));

            if (progress == 1)
            {
                Stop();
            }
        }
    }
}