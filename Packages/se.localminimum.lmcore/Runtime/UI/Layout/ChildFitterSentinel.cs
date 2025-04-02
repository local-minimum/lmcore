using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class ChildFitterSentinel : MonoBehaviour, ILayoutController
    {
        List<AbsFitter> _fitters;
        List<AbsFitter> fitters
        {
            get
            {
                if (_fitters == null)
                {
                    var fitter = transform.parent.GetComponentInParent<AbsFitter>(true);
                    if (fitter == null)
                    {
                        Debug.LogError($"Sentinel '{name}' should be on a child to the ChildFitter component");
                    }
                    else
                    {
                        _fitters = fitter.GetComponents<AbsFitter>().ToList();
                    }
                }
                return _fitters;
            }
        }

        bool _dirty = false;

        private void OnEnable()
        {
            _dirty = true;
            var ownFitter = GetComponent<AbsFitter>();
            if (ownFitter != null)
            {
                ownFitter.OnRecalculateSize += ChildFitterSentinel_OnRecalculateSize;
            }
        }

        private void OnDisable()
        {
            var ownFitter = GetComponent<AbsFitter>();
            if (ownFitter != null)
            {
                ownFitter.OnRecalculateSize -= ChildFitterSentinel_OnRecalculateSize;
            }
        }

        private void ChildFitterSentinel_OnRecalculateSize()
        {
            _dirty = true;
        }

        [ContextMenu("Force Dirty")]
        public void SetDirty()
        {
            _dirty = true;
        }

        private void LateUpdate()
        {
            if (_dirty)
            {
                _dirty = false;
                if (fitters != null)
                {
                    foreach (var fitter in fitters)
                    {
                        // Debug.Log($"Asking {fitter} to recalculate");
                        fitter.Recalculate();
                    }
                }
            }
        }

        bool FitterCaresAbout(Dimension dimension) =>
            fitters != null && fitters.Any(f => f.Dimensions.HasFlag(dimension));

        public void SetLayoutHorizontal()
        {
            if (FitterCaresAbout(Dimension.Width)) return;
            _dirty = true;

        }

        public void SetLayoutVertical()
        {
            if (FitterCaresAbout(Dimension.Height)) return;
            _dirty = true;
        }
    }
}
