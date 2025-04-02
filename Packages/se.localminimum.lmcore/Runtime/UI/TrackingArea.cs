using LMCore.Extensions;
using UnityEngine;

namespace LMCore.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class TrackingArea : MonoBehaviour
    {
        [SerializeField, Tooltip("Camera used to determine tracking position, if omitted uses main camera")]
        Camera _camera;
        Camera cam => _camera == null ? Camera.main : _camera;

        [SerializeField, Tooltip("The transform that is being moved around tracking the position of the `Target`")]
        RectTransform trackingTransform;

        [SerializeField, Range(0, 1f)]
        float easing = 0.7f;

        bool tracking;
        /// <summary>
        /// The object that tracks 
        /// </summary>
        GameObject _target;
        public GameObject Target
        {
            get => _target;
            set
            {
                if (value != null || _target != null)
                {
                    tracking = true;
                }
                _target = value;
            }
        }

        private void Update()
        {
            if (trackingTransform == null || !tracking) return;

            var rt = GetComponent<RectTransform>();
            var cam = this.cam;

            if (Target == null || cam == null)
            {
                var newPos = Vector3.Lerp(trackingTransform.position, transform.position, easing);
                if (Vector3.SqrMagnitude(newPos - transform.position) < 0.1f)
                {
                    trackingTransform.position = transform.position;
                    tracking = false;
                    Debug.Log("TrackingArea: Returning tracked transform to default position");
                }
                else
                {
                    trackingTransform.position = newPos;
                }
                return;
            }

            var canvas = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

            var viewPortPoint = cam.WorldToViewportPoint(Target.transform.position);

            viewPortPoint.x *= canvas.rect.width;
            viewPortPoint.y *= canvas.rect.height;
            viewPortPoint.z = 0;

            var pt = rt.InverseTransformPoint(viewPortPoint);
            var rect = rt.rect;
            var target = rt.InverseTransformPoint(pt);

            if (!rect.Contains(pt))
            {
                pt = rect.Clamp(pt);
            }

            // Revert to global space
            pt = rt.TransformPoint(pt);
            trackingTransform.position = Vector3.Lerp(trackingTransform.position, pt, easing);
        }
    }
}
