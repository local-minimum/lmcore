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

    CrawlerInput2 cInput;
    GridEntity gEntity;

    [SerializeField]
    void Start()
    {
        cInput = GetComponent<CrawlerInput2>();
        gEntity = GetComponent<GridEntity>();
        gEntity.Sync();
    }


    int animationTickId;
    Movement Animation = Movement.None;
    float animationStartTime;
    float animationInterpolationStart;
    float animationDuration;

    bool turning;
    Quaternion activeStartRotation;
    Quaternion activeEndRotation;
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

        animationStartTime = Time.timeSinceLevelLoad;
        animationDuration = endTime - animationStartTime;
        animationInterpolationStart = unadjustedProgress;
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
        animationDuration = duration;
        turning = movement.IsRotation();

        if (turning)
        {
            activeStartRotation = transform.rotation;
            activeEndRotation = gEntity.LookDirection.ApplyRotation(movement).AsQuaternion();
        } else if (movement != Movement.None)
        {
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
        } else
        {
            gEntity.Translate(Animation);
        }

        gEntity.Sync();
        Animation = Movement.None;
    } 

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
        } else
        {
            transform.position = Vector3.Lerp(activeStartPosition, activeEndPosition, progress);
        }
    }
}
