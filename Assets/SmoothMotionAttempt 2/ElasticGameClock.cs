using LMCore.AbstractClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void TickStartEvent(int tickId, float expectedDuration);
public delegate void TickEndAjustmentEvent(int tickId, float unadjustedProgress, float adjustedProgress, float endTime);
public delegate void TickEndEvent(int tickId);

public class ElasticGameClock : Singleton<ElasticGameClock> 
{
    public static event TickStartEvent OnTickStart;
    public static event TickEndAjustmentEvent OnTickEndAdjustment;
    public static event TickEndEvent OnTickEnd;

    [Range(0, 2), Tooltip("Minimum pace of ticks")]
    public float minTickDuration = 0.2f;

    [Range(0, 2), Tooltip("Normal pace of ticks")]
    public float baseTickDuration = 0.5f;

    [SerializeField, Range(0, 1), Tooltip("Fraction if tick remainder reserved to current tick")]
    private float adjustmentSplit = 0.5f;

    public int TickId { get; private set; } = 0;


    bool ticking = false;
    bool currentTickIsEndAdjusted = false;
    float currentTickStart;
    float currentTickInitialDuration;
    float currentTickEnd;

    /// <summary>
    /// Fraction of tick that has progressed
    /// </summary>
    public float TickProgress => Mathf.Clamp01(
        (Time.timeSinceLevelLoad - currentTickStart) / (currentTickEnd - currentTickStart)
    );

    /// <summary>
    /// Fraction of tick that would have progressed if no end of tick adjustment was performed
    /// </summary>
    public float TickProgressNonAdjusted => Mathf.Clamp01(
        (Time.timeSinceLevelLoad - currentTickStart) / currentTickInitialDuration
    );

    public float ClampedReferenceDuration => Mathf.Clamp(
        Time.timeSinceLevelLoad - currentTickStart,
        Mathf.Max(minTickDuration, currentTickInitialDuration),
        baseTickDuration
    );

    public bool AdjustEndOfTick()
    {
        if (currentTickIsEndAdjusted || !ticking) return false;

        var now = Time.timeSinceLevelLoad;
        var remainder = (currentTickEnd - now) * adjustmentSplit;
        currentTickEnd = now + remainder;

        currentTickIsEndAdjusted = true;

        OnTickEndAdjustment?.Invoke(TickId, TickProgressNonAdjusted, TickProgress, currentTickEnd);
        return true;
    }

    public bool RequestTick()
    {
        if (ticking) return false;
        var timeSinceLastTick = Time.timeSinceLevelLoad - currentTickStart;

        currentTickStart = Time.timeSinceLevelLoad;
        currentTickInitialDuration = Mathf.Clamp(
            timeSinceLastTick, 
            minTickDuration, 
            baseTickDuration
        );
        currentTickEnd = currentTickStart + currentTickInitialDuration;

        currentTickIsEndAdjusted = false;
        ticking = true;

        TickId++;

        OnTickStart.Invoke(TickId, currentTickInitialDuration);
        return true;
    }

    private void Update()
    {
        if (ticking && TickProgress >= 1)
        {
            ticking = false;
            OnTickEnd?.Invoke(TickId);
        }
    }
}
