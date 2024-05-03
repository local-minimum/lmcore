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

    CrawlerInput cInput;

    [SerializeField]
    Direction StartLookDirection;

    Vector2Int Position;
    Direction LookDirection;

    [SerializeField, Range(0, 2)]
    float turnTime = 0.2f;

    [SerializeField, Range(0, 2)]
    float translationTime = 0.45f;

    [SerializeField]
    void Start()
    {
        cInput = GetComponent<CrawlerInput>();
        Sync();
    }

    void Sync()
    {
        transform.position = Position.ToPositionFromXZPlane();
        transform.rotation = LookDirection.AsQuaternion();
    }

    Movement ActiveMovement = Movement.None;
    float startTime;
    bool turning;
    Quaternion activeStartRotation;
    Quaternion activeEndRotation;
    Vector3 activeStartPosition;
    Vector3 activeEndPosition;

    void Update()
    {
        if (ActiveMovement == Movement.None) {
            ActiveMovement = cInput.GetMovement();
            startTime = Time.timeSinceLevelLoad;
            turning = ActiveMovement.IsRotation();
            if (turning)
            {
                activeStartRotation = transform.rotation;
                activeEndRotation = LookDirection.ApplyRotation(ActiveMovement).AsQuaternion();
            } else if (ActiveMovement != Movement.None)
            {
                activeStartPosition = transform.position;
                activeEndPosition = LookDirection
                    .RelativeTranslation(ActiveMovement)
                    .Translate(Position)
                    .ToPositionFromXZPlane();

            }
        } else if (turning) {
            float progress = Mathf.Clamp01((Time.timeSinceLevelLoad - startTime) / turnTime);
            transform.rotation = Quaternion.Lerp(activeStartRotation, activeEndRotation, progress);
            if (progress >= 1f)
            {
                LookDirection = LookDirection.ApplyRotation(ActiveMovement);
                ActiveMovement = Movement.None;
            }
        } else
        {
            float progress = Mathf.Clamp01((Time.timeSinceLevelLoad - startTime) / translationTime);
            transform.position = Vector3.Lerp(activeStartPosition, activeEndPosition, progress);
            if (progress >= 1f)
            {
                Position = LookDirection
                    .RelativeTranslation(ActiveMovement)
                    .Translate(Position);
                ActiveMovement = Movement.None;
            }
        }
    }
}
