using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void EaseEndEvent(bool isAtEnd);

[System.Serializable]
public class TemporalEasing<T> 
{
    public event EaseEndEvent OnEaseEnd;

    public T StartValue;
    public T EndValue;

    [SerializeField]
    private float fullEaseDuration;

    [SerializeField]
    AnimationCurve easing;

    float easeStart;
    public bool IsEasing { get; private set; } = false;

    float progressStart;
    float progressEnd;
    float duration;

    public void EaseStartToEnd()
    {
        if (IsEasing) { return; }

        progressStart = 0;
        progressEnd = 1;
        easeStart = Time.timeSinceLevelLoad;
        IsEasing = true;
        duration = fullEaseDuration;
    }

    public void EaseEndToStart()
    {
        if (IsEasing) { return; }

        progressStart = 1;
        progressEnd = 0;
        easeStart = Time.timeSinceLevelLoad;
        IsEasing = true;
        duration = fullEaseDuration;
    }

    public void AbortEase()
    {
        if (!IsEasing) { return; }

        progressStart = Progress;
        progressEnd = progressEnd == 0 ? 1 : 0;
        easeStart = Time.timeSinceLevelLoad;
        duration = fullEaseDuration * Mathf.Abs(progressEnd - progressStart);
        IsEasing = duration == 0 ? false : true;
    }

    private float TimeProgress => Mathf.Clamp01((Time.timeSinceLevelLoad - easeStart) / duration);
    public float Progress => Mathf.Lerp(progressStart, progressEnd, TimeProgress);

    public T Evaluate()
    {
        if (!IsEasing) { return StartValue; }

        var progress = easing.Evaluate(Progress);

        var t = typeof(T);

        if (TimeProgress == 1)
        {
            IsEasing = false;
            OnEaseEnd?.Invoke(progress == 1);
        }

        if (t == typeof(Vector2))
        {
            return (T) Convert.ChangeType(
                Vector2.Lerp(
                (Vector2)Convert.ChangeType(StartValue, t),
                (Vector2)Convert.ChangeType(EndValue, t),
                progress
            ), t);
        }
        if (t == typeof(float))
        {
            return (T)Convert.ChangeType(
                Mathf.Lerp(
                    (float)Convert.ChangeType(StartValue, t),
                    (float)Convert.ChangeType(EndValue, t),
                    progress
                ),
                t
            );
        }



        return progress == 1 ? EndValue : StartValue;
    }
}
