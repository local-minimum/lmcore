using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslateAnchorsUI : MonoBehaviour
{
    [SerializeField]
    TemporalEasing<Vector2> easing = new TemporalEasing<Vector2>();

    Vector2 anchorMin;
    Vector2 anchorMax;

    private void Awake()
    {
        var rt = GetComponent<RectTransform>();
        anchorMax = rt.anchorMax;
        anchorMin = rt.anchorMin;
    }

    public void TransitionToStart()
    {
        if (!easing.IsEasing) { 
            easing.EaseEndToStart();
            isEnd = false;
        } else
        {
            easing.AbortEase();
        }
    }

    public void TransitionToEnd()
    {
        if (!easing.IsEasing)
        {
            easing.EaseStartToEnd();
        } else
        {
            easing.AbortEase();
        }
    }

    public void Toggle()
    {
        if (isEnd)
        {
            TransitionToStart();
        } else
        {
            TransitionToEnd();
        }
    }

    bool isEnd = true;

    void UpdateAnchors(Vector2 offset)
    {
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = anchorMin + offset;
        rt.anchorMax = anchorMax + offset;
    }

    private void Start()
    {
        UpdateAnchors(easing.EndValue);
    }

    private void OnEnable()
    {
        easing.OnEaseEnd += Easing_OnEaseEnd;
    }

    private void OnDisable()
    {
        easing.OnEaseEnd -= Easing_OnEaseEnd;
    }

    private void Easing_OnEaseEnd(bool isAtEnd)
    {
        isEnd = isAtEnd;
    }

    private void Update()
    {
        if (easing.IsEasing)
        {
            UpdateAnchors(easing.Evaluate());
        }
    }
}
