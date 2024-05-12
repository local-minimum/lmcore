using UnityEngine;

namespace LMCore.Juice
{
    public class NodeShaker : MonoBehaviour
    {
        [SerializeField, Range(0, 2)]
        float magnitude = 0.1f;

        [SerializeField, Range(0, 2)]
        float duration = 0.1f;


        public void Shake()
        {
            shakeEnd = Time.timeSinceLevelLoad + duration;
            shaking = true;
        }

        bool shaking;
        float shakeEnd;

        private void Update()
        {
            if (!shaking) return;

            if (Time.timeSinceLevelLoad > shakeEnd)
            {
                shaking = false;
                transform.localPosition = Vector3.zero;
                return;
            }

            var offset = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)).normalized * Random.Range(0.25f * magnitude, magnitude);
            transform.localPosition = offset;
        }
    }
}
