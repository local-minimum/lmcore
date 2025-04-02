using LMCore.Utilities;
using UnityEngine;

namespace LMCore.UI
{
    public delegate void PullProgressEvent(BezierUIPuller puller, GameObject pulled, float progress);
    public delegate void PullPhaseEvent(BezierUIPuller puller, GameObject pulled);

    /// <summary>
    /// Pulls object along a bezier curve.
    /// 
    /// Note: Only implemented for stationary canvases at the moment!
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BezierUIPuller : MonoBehaviour
    {
        public static event PullProgressEvent OnPullProgress;
        public static event PullPhaseEvent OnPullStart;
        public static event PullPhaseEvent OnPullEnd;


        public enum PrimaryDirection
        {
            /// <summary>
            /// Longest cardinal axis is primary direction
            /// </summary>
            Infer,
            /// <summary>
            /// The vector from source to target is the primary direction 
            /// </summary>
            Anchors,
            Vertical,
            Horizontal
        };

        [SerializeField, Header("Curve")]
        PrimaryDirection primaryDirection = PrimaryDirection.Vertical;

        [SerializeField, Tooltip("From where things get pulled, if empty uses pulled objects current position")]
        RectTransform source;

        [SerializeField, Tooltip("If target is omitted, current recttransform is used instead")]
        RectTransform target;
        RectTransform Target =>
            target == null ? GetComponent<RectTransform>() : target;

        [SerializeField, Range(-5, 5), Tooltip("Changes orthogonal offset (proportional to distance between anchors) from default 0 offset")]
        float leftArcBias = 0f;

        [SerializeField, Tooltip("x is orthogonal and y is paralell to the primary direction")]
        Vector2 relativeControlPoint1;

        [SerializeField, Tooltip("x is orthogonal and y is paralell to the primary direction")]
        Vector2 relativeControlPoint2;

        [SerializeField, Header("Pull"), Tooltip("How far along the track the object should be pulled at a certain progress")]
        AnimationCurve positionByProgress;

        [SerializeField]
        bool rotate;

        [SerializeField, Range(0, 0.5f)]
        float rotationLookAhead = 0.05f;

        [SerializeField]
        AnimationCurve rotationWeightOverTime;

        [SerializeField, Header("Debug")]
        RectTransform debugPulledObject;

        [SerializeField, Range(0f, 1f)]
        float debugProgress;

        [SerializeField]
        RectTransform debugSource;

        public bool Pulling => pullJob.Valid && !pullJob.durationProgress.Completed;

        Vector3 PrimaryVector(Vector3 sourcePos)
        {
            var targetPos = Target.position;
            var delta = targetPos - sourcePos;

            switch (primaryDirection)
            {
                case PrimaryDirection.Anchors:
                    return delta.normalized;
                case PrimaryDirection.Infer:
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    {
                        return new Vector3(delta.x > 0 ? 1 : -1, 0);
                    }
                    return new Vector3(0, delta.y > 0 ? 1 : -1);
                case PrimaryDirection.Vertical:
                    return new Vector3(0, delta.y > 0 ? 1 : -1);
                case PrimaryDirection.Horizontal:
                    return new Vector3(delta.x > 0 ? 1 : -1, 0);
            }

            return Vector3.zero;
        }

        void GetControlPointOffsets(
            Vector3 source,
            out Vector3 controlOffset1,
            out Vector3 controlOffset2)
        {
            var delta = Target.position - source;

            var primaryDirection = PrimaryVector(source);
            var paraProjected = Vector3.Project(delta, primaryDirection);
            var distance = Vector3.Magnitude(paraProjected);

            // Right ortho
            var orthoDirection = new Vector3(-primaryDirection.y, primaryDirection.x);
            var projectedOrtho = Vector3.Project(delta, orthoDirection);

            if (Vector3.Dot(projectedOrtho, orthoDirection) < leftArcBias * distance)
            {
                // Flip to left ortho
                orthoDirection *= -1f;
            }

            controlOffset1 =
                (primaryDirection * relativeControlPoint1.y * distance) +
                (orthoDirection * relativeControlPoint1.x * distance);

            controlOffset2 =
                (primaryDirection * relativeControlPoint2.y * distance) +
                (orthoDirection * relativeControlPoint2.x * distance);
        }

        public void OnDrawGizmosSelected()
        {
            float sphereSize = 2f;
            var arcResolution = 25;

            var controlOffset1 = pullJob.controlOffset1;
            var controlOffset2 = pullJob.controlOffset2;
            var anchor1 = pullJob.origin;
            var anchor2 = Target.position;

            if (!pullJob.Valid)
            {
                if (source != null)
                {
                    anchor1 = source.position;
                }
                else if (debugSource != null)
                {
                    anchor1 = debugSource.position;
                }
                else if (debugPulledObject != null)
                {
                    anchor1 = debugPulledObject.position;
                }
                else
                {
                    return;
                }

                GetControlPointOffsets(anchor1, out controlOffset1, out controlOffset2);
            }

            var control1 = anchor1 + controlOffset1;
            var control2 = anchor2 + controlOffset2;

            Gizmos.color = Color.gray;
            Gizmos.DrawSphere(control1, sphereSize);
            Gizmos.DrawSphere(control2, sphereSize);
            Gizmos.DrawLine(anchor1, control1);
            Gizmos.DrawLine(anchor2, control2);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(anchor1, sphereSize);
            Gizmos.DrawSphere(anchor2, sphereSize);

            var prev = anchor1;
            for (var i = 0; i <= arcResolution; i++)
            {
                float t = (float)i / arcResolution;
                var current = Bezier.LerpPoint(anchor1, control1, control2, anchor2, t);

                if (i > 0)
                {
                    Gizmos.DrawLine(prev, current);
                }
                prev = current;
            }

            if (!pullJob.Valid)
            {
                UpdatePulled(debugPulledObject, anchor1, debugProgress, Quaternion.identity);
            }
        }

        void UpdatePulled()
        {
            Debug.Log($"BezierPuller: Updating active job {pullJob}");

            var progress = pullJob.durationProgress.Progress;

            UpdatePulled(
                pullJob.pulledTransform,
                pullJob.origin,
                pullJob.controlOffset1,
                pullJob.controlOffset2,
                progress,
                pullJob.originRotation
                );
        }

        void UpdatePulled(RectTransform pulled, Vector3 origin, float progress, Quaternion originRotation)
        {
            if (pulled == null) return;

            GetControlPointOffsets(origin, out var controlOffset1, out var controlOffset2);

            UpdatePulled(
                pulled,
                origin,
                controlOffset1,
                controlOffset2,
                progress,
                originRotation);
        }

        void UpdatePulled(
            RectTransform pulled,
            Vector3 origin,
            Vector3 controlOffset1,
            Vector3 controlOffset2,
            float progress,
            Quaternion originRotation)
        {
            if (pulled == null) return;

            var canvas = GetComponentInParent<Canvas>();
            var control1 = origin + controlOffset1;
            var control2 = Target.position + controlOffset2;

            var scaledProgress = positionByProgress != null ? positionByProgress.Evaluate(progress) : progress;

            pulled.position = Bezier.LerpPoint(origin, control1, control2, Target.position, scaledProgress);

            if (rotate)
            {
                var canvasForward = canvas ? canvas.transform.forward : Vector3.forward;
                var forwardTime = Mathf.Min(1, scaledProgress + rotationLookAhead);

                if (forwardTime > scaledProgress)
                {
                    var forwardPoint = Bezier.LerpPoint(origin, control1, control2, Target.position, forwardTime);

                    var optimalRotation = Quaternion.LookRotation(
                        canvasForward,
                        forwardPoint - pulled.position);

                    if (rotationWeightOverTime != null)
                    {
                        pulled.transform.rotation = Quaternion.Slerp(
                            originRotation,
                            optimalRotation,
                            rotationWeightOverTime.Evaluate(progress));

                    }
                    else
                    {
                        pulled.transform.rotation = optimalRotation;
                    }
                }
            }
        }

        private struct PullJob
        {
            public readonly DurationProgress durationProgress;
            public readonly GameObject pulled;
            public readonly RectTransform pulledTransform;
            public Vector3 origin;
            public Quaternion originRotation;
            public Vector3 controlOffset1;
            public Vector3 controlOffset2;

            public bool Valid => pulled != null && durationProgress.Valid;

            public PullJob(
                GameObject pulled,
                float duration,
                Vector3 origin,
                Vector3 controlOffset1,
                Vector3 controlOffset2)
            {
                this.pulled = pulled;
                pulledTransform = pulled.GetComponent<RectTransform>();

                // TODO: This isn't great if the canvas moves about in world space...
                this.origin = origin;
                originRotation = pulled.transform.rotation;

                this.controlOffset1 = controlOffset1;
                this.controlOffset2 = controlOffset2;

                durationProgress = new DurationProgress(duration);
            }

            public override string ToString() =>
                Valid ?
                $"<PullJob: {pulled}, {durationProgress})>" :
                "<PullJob [Nothing]>";


            public static PullJob Nothing => new PullJob();
        }

        // TODO: Should we handle saving somewhere or just not care if something was being pulled?
        private PullJob pullJob;

        /// <summary>
        /// Pulls a game object along a bezier on a canvas.
        /// 
        /// Note: The curve can be dynamically updated with regards to the target position during the pull,
        /// but all other parameters are locked in once pulling starts.
        /// 
        /// Note: Pulling should probably not be done if another pull job is active, 
        /// but should other job exist, it will emit an OnPullEnd event.
        /// </summary>
        /// <param name="pulled">Pulled object</param>
        /// <param name="duration">How long the pull should be</param>
        /// <param name="overrideSource">If source, if configured, should be respected as the origin of the pull</param>
        public void Pull(GameObject pulled, float duration, bool overrideSource = false)
        {
            if (pullJob.Valid)
            {
                Debug.LogWarning($"Bezier Puller: was already pulling {pullJob}, this will be aborted");
                OnPullEnd?.Invoke(this, pulled.gameObject);
            }

            GetControlPointOffsets(pulled.transform.position, out var controlOffset1, out var controlOffset2);


            Vector3 origin = pulled.transform.position;
            if (source != null && !overrideSource)
            {
                origin = source.position;
            }

            pullJob = new PullJob(pulled, duration, origin, controlOffset1, controlOffset2);

            OnPullStart?.Invoke(this, pulled);
            OnPullProgress?.Invoke(this, pulled, pullJob.durationProgress.Progress);

            Info();
        }

        /// <summary>
        /// Abort active pull job if any exist and potentially emit OnPullEnd event
        /// </summary>
        public void Abort(bool emitCompleteEvent = true)
        {
            if (pullJob.Valid && !pullJob.durationProgress.Completed)
            {
                OnPullEnd?.Invoke(this, pullJob.pulled);
                pullJob = PullJob.Nothing;
            }
        }

        private void Update()
        {
            if (pullJob.Valid)
            {
                UpdatePulled();

                OnPullProgress?.Invoke(this, pullJob.pulled, pullJob.durationProgress.Progress);

                if (pullJob.durationProgress.Completed)
                {
                    OnPullEnd?.Invoke(this, pullJob.pulled);
                    Debug.Log($"BezierPuller: {pullJob} completed");
                    pullJob = PullJob.Nothing;
                }
            }
        }

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log($"BezierPuller {(Pulling ? "Pulling" : "Inactive")}: {pullJob}");
        }
    }
}
