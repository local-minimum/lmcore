using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LMCore.Extensions;
using LMCore.IO;
using LMCore.Crawler;

public class NaiveSmoothMovement : MonoBehaviour
{
    [SerializeField]
    private int StepSize = 3;

    [SerializeField, Range(0, 1), Tooltip("Part of tick used for turns, should not be 0")]
    float turnDurationFactor = 1f;

    CrawlerInput2 cInput;
    GridEntity gEntity;

    private GridEntityController _gController;
    private GridEntityController gController
    {
        get
        {
            if (_gController == null)
            {
                _gController = GetComponent<GridEntityController>();
            }
            return _gController;
        }
    }

    void Awake()
    {
        cInput = GetComponent<CrawlerInput2>();
        gEntity = GetComponent<GridEntity>();
        gEntity.Sync();
        GameSettings.InstantMovement.OnChange += InstantMovement_OnChange;
        enabled = !GameSettings.InstantMovement.Value;
    }

    private void OnDestroy()
    {
        GameSettings.InstantMovement.OnChange -= InstantMovement_OnChange;
    }

    private void InstantMovement_OnChange(bool value)
    {
        enabled = !value;
    }

    int animationTickId;
    Movement Animation = Movement.None;
    float animationStartTime;
    float animationInterpolationStart;
    float animationDuration;

    bool turning;
    Quaternion activeStartRotation;
    Quaternion activeEndRotation;

    bool allowedTranslation;
    Vector3 activeStartPosition;
    Vector3 activeEndPosition;

    private void OnEnable()
    {
        if (cInput == null)
        {
            cInput = GetComponent<CrawlerInput2>();
        }

        cInput.OnMovement += CInput_OnMovement;
        ElasticGameClock.OnTickEnd += ElasticGameClock_OnTickEnd;
        ElasticGameClock.OnTickEndAdjustment += ElasticGameClock_OnTickEndAdjustment;
    }


    private void OnDisable()
    {
        cInput.OnMovement -= CInput_OnMovement;
        ElasticGameClock.OnTickEnd -= ElasticGameClock_OnTickEnd;
        ElasticGameClock.OnTickEndAdjustment -= ElasticGameClock_OnTickEndAdjustment;
    }

    private void ElasticGameClock_OnTickEndAdjustment(int tickId, float unadjustedProgress, float adjustedProgress, float endTime)
    {
        if (animationTickId != tickId) return;

        var turnCompensatedUnadjustedProgress = turning ? Mathf.Clamp01(unadjustedProgress / turnDurationFactor) : unadjustedProgress;

        animationStartTime = Time.timeSinceLevelLoad;

        var remainder = endTime - animationStartTime;
        if (turnCompensatedUnadjustedProgress < 1)
        {

            animationDuration = turning ? remainder * turnDurationFactor : remainder;
        } else
        {
            animationDuration = 0.00000001f;
        }
        animationInterpolationStart = turnCompensatedUnadjustedProgress;
    }

    private void ElasticGameClock_OnTickEnd(int tickId)
    {
        if (animationTickId == tickId)
        {
            EndAnimation();
        }
    }

    private void CInput_OnMovement(int tickId, Movement movement, float duration)
    {
        if (Animation != Movement.None) { EndAnimation(); }

        animationTickId = tickId;
        Animation = movement;
        animationStartTime = Time.timeSinceLevelLoad;
        animationInterpolationStart = 0;
        turning = movement.IsRotation();
        animationDuration = turning ? duration * turnDurationFactor : duration;

        if (turning)
        {
            activeStartRotation = transform.rotation;
            activeEndRotation = gEntity.LookDirection.ApplyRotation(movement).AsQuaternion();
        } else if (movement != Movement.None)
        {
            allowedTranslation = gController.CanMoveTo(movement, StepSize);
            activeStartPosition = transform.position;
            activeEndPosition = gEntity.LookDirection
                .RelativeTranslation(movement)
                .Translate(gEntity.Position)
                .ToPositionFromXZPlane();
        }
    }

    void EndAnimation()
    {
        if (turning)
        {
            gEntity.Rotate(Animation);
        } else if (allowedTranslation)
        {
            gEntity.Translate(Animation);
        }

        gEntity.Sync();
        Animation = Movement.None;
    }

    [SerializeField, Range(0, 0.5f)]
    float bounceAtProgress = 0.4f;

    void Update()
    {
        if (Animation == Movement.None) { return; }

        var progress = animationInterpolationStart + (1 - animationInterpolationStart) * Mathf.Clamp01((Time.timeSinceLevelLoad - animationStartTime) / animationDuration);

        if (progress == 1)
        {
            EndAnimation();
            return;
        }

        if (turning)
        {
            transform.rotation = Quaternion.Lerp(activeStartRotation, activeEndRotation, progress);
        } else if (allowedTranslation || progress < bounceAtProgress)
        {
            transform.position = Vector3.Lerp(activeStartPosition, activeEndPosition, progress);
        } else
        {
            transform.position = Vector3.Lerp(activeStartPosition, activeEndPosition, 2 * bounceAtProgress - progress);
        }
    }
}
