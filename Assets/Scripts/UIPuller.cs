using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIPuller : MonoBehaviour
{
    [SerializeField]
    RectTransform sourceParent;

    [SerializeField, Range(0, 2)]
    float waitFactor = 1f;

    [SerializeField, Range(-5, 5)]
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
        Debug.Log(sourceParent.GetComponentsInChildren<PullUIToTarget>().Where(ui => !ui.Pulled && ui.Target == GetComponent<RectTransform>()).Count());

        StartCoroutine(SequencedPulling(
            sourceParent.GetComponentsInChildren<PullUIToTarget>().Where(ui => !ui.Pulled && ui.Target == GetComponent<RectTransform>())
        ));
    }

    private IEnumerator<WaitForSeconds> SequencedPulling(IEnumerable<PullUIToTarget> ui)
    {
        Pulling = true;
        foreach (PullUIToTarget t in ui)
        {
            t.PullSubject();
            pulled.Add(t);
            yield return new WaitForSeconds(waitFactor * t.EaseDuration + spacingSeconds);
        }
        Pulling = false;
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

            ui.RestoreParent();

            if(enabledReturned) ui.gameObject.SetActive(true);

            yield return new WaitForSeconds(returnSpacingSeconds);
        }

        Returning = false;
    }

    [SerializeField]
    bool disablePulled;
    [SerializeField]
    bool enabledReturned;

    private void Update()
    {
        if (!disablePulled) return;

        for (int i = 0, l = pulled.Count; i < l; i++)
        {
            var p = pulled[i];

            if (p.Pulling || !p.gameObject.activeSelf) continue;

            p.gameObject.SetActive(false);
        }
    }
}
