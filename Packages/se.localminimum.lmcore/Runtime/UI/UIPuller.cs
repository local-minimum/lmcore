using LMCore.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.UI
{
    public class UIPuller : MonoBehaviour
    {
        [SerializeField]
        RectTransform sourceParent;

        [SerializeField]
        RectTransform _targetParent;
        RectTransform targetParent => _targetParent ?? (transform as RectTransform);

        [SerializeField, Range(0, 2), Tooltip("Does not affect PullOne calls")]
        float waitFactor = 1f;

        [SerializeField, Range(-5, 5), Tooltip("Does not affect PullOne calls")]
        float spacingSeconds = 0f;

        [SerializeField]
        float returnSpacingSeconds = 0.5f;

        public bool Pulling { get; private set; } = false;

        List<PullUIToTarget> pulled = new List<PullUIToTarget>();

        [ContextMenu("Pull from source parent")]
        public void Pull()
        {
            if (Pulling) return;

            Debug.Log(sourceParent.GetComponentsInChildren<PullUIToTarget>().Count());
            Debug.Log(sourceParent.GetComponentsInChildren<PullUIToTarget>().Where(ui => !ui.Pulled).Count());
            Debug.Log(sourceParent.GetComponentsInChildren<PullUIToTarget>().Where(ui => !ui.Pulled && ui.Target == targetParent).Count());

            StartCoroutine(SequencedPulling(
                sourceParent.GetComponentsInChildren<PullUIToTarget>().Where(ui => !ui.Pulled && ui.Target == targetParent)
            ));
        }

        Dictionary<PullUIToTarget, System.Action<PullUIToTarget>> onPulledCallbacks = new Dictionary<PullUIToTarget, System.Action<PullUIToTarget>>();

        List<DelayedEffect> delayedEffects = new List<DelayedEffect>();

        public void PullOne(GameObject go, System.Action<PullUIToTarget> callback, float initialDelay = 0f)
        {
            var ui = go.GetComponent<PullUIToTarget>();
            if (ui == null) return;
            Pulling = true;

            ui.Target = targetParent;
            onPulledCallbacks[ui] = callback;

            if (initialDelay == 0f)
            {
                pulled.Add(ui);
                ui.PullSubject();
            }
            else
            {
                delayedEffects.Add(new DelayedEffect(
                    initialDelay,
                    () =>
                    {
                        pulled.Add(ui);
                        ui.PullSubject();
                    }));
            }
        }

        private IEnumerator<WaitForSeconds> SequencedPulling(IEnumerable<PullUIToTarget> ui)
        {
            Pulling = true;
            foreach (PullUIToTarget t in ui)
            {
                t.PullSubject();
                pulled.Add(t);
                yield return new WaitForSeconds((waitFactor * t.EaseDuration) + spacingSeconds);
            }
        }

        public bool Returning { get; private set; }

        [ContextMenu("Return to source parent")]
        public void Return()
        {
            if (Returning) return;

            StartCoroutine(SequenceReturning());
        }

        private IEnumerator<WaitForSeconds> SequenceReturning()
        {
            Returning = true;

            while (pulled.Count > 0)
            {
                var ui = pulled[0];
                pulled.Remove(ui);

                ui.EaseToOriginalTransform();

                if (activateReturned) ui.gameObject.SetActive(true);

                yield return new WaitForSeconds(returnSpacingSeconds);
            }

            Returning = false;
        }

        [SerializeField]
        bool inactivateAfterPulled;
        [SerializeField]
        bool activateReturned;

        private void Update()
        {
            if (!Pulling) return;

            delayedEffects = delayedEffects.Where(e => !e.Check()).ToList();

            for (int i = 0, l = pulled.Count; i < l; i++)
            {
                var p = pulled[i];

                if (p.Pulling) continue;

                if (inactivateAfterPulled)
                {
                    p.gameObject.SetActive(false);
                }

                pulled.Remove(p);
                onPulledCallbacks[p]?.Invoke(p);
                onPulledCallbacks.Remove(p);

                i--;
                l--;
            }

            // Some cards may be waiting to be pulled so we wait until
            // their callbacks have been invoked!
            if (onPulledCallbacks.Count == 0)
            {
                Pulling = false;
            }
        }
    }
}
