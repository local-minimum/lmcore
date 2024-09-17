using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PositionTracker : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField]
    float sampleInterval = 0.02f;

    [SerializeField]
    float trailRetentionTime = 1f;
    [SerializeField]
    int minTrailLength = 40;

    private struct Sample
    {
        public float time;
        public Vector3 position;
        public Vector3 forward;

        public Sample(Transform transform)
        {
            time = Time.realtimeSinceStartup;
            position = transform.position;
            forward = transform.forward;
        }
    }

    private List<Sample> samples = new List<Sample>();

    void Update()
    {
        if (samples.Count == 0)
        {
            samples.Add(new Sample(transform));
            return;
        }
        var last = samples.Last();
        if (Time.realtimeSinceStartup - last.time >= sampleInterval)
        {
            samples.Add(new Sample(transform));
        }

        var n = samples.Count;
        if (n <= minTrailLength) { return; }

        for (int i = 0; i < n - minTrailLength; i++)
        {
            var sample = samples[i];
            if (Time.realtimeSinceStartup - sample.time <= trailRetentionTime)
            {
                if (i == 0) return;

                samples = samples.Skip(i).ToList();
                return;
            }
        }
    }

    [SerializeField]
    int rayEveryNth = 10;

    [SerializeField]
    float rayLength = 0.25f;

    [SerializeField]
    Color SlowSpeedColor = Color.white;

    [SerializeField]
    Color FastSpeedColor = Color.red;

    [SerializeField, Tooltip("Units per second for max color")]
    float fastSpeed = 1f;

    private void OnDrawGizmosSelected()
    {
        if (samples.Count == 0) return;

        Gizmos.color = Color.magenta;

        var prev = samples[0];
        Gizmos.DrawRay(prev.position, prev.forward * rayLength);

        for (int i = 1, n = samples.Count; i < n; i++) {
            var sample = samples[i];

            var speed = Vector3.Magnitude(prev.position - sample.position) / (sample.time - prev.time);
            Gizmos.color = Color.Lerp(SlowSpeedColor, FastSpeedColor, speed / fastSpeed);
            Gizmos.DrawLine(prev.position, sample.position);

            if (i % rayEveryNth == 0)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(prev.position, prev.forward * rayLength);
            }
            prev = sample;
        }
    }
#endif
}
