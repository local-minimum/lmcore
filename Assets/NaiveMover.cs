using LMCore.Crawler;
using LMCore.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LMCore.Extensions;

public class NaiveMover : MonoBehaviour
{
    [SerializeField]
    private int StepSize = 3;

    CrawlerInput cInput;

    [SerializeField]
    Direction StartLookDirection;

    Vector2Int Position;
    Direction LookDirection;

    void Start()
    {
        cInput = GetComponent<CrawlerInput>();
        Sync();
    }

    void Translate(Movement movement)
    {
        Position = LookDirection.RelativeTranslation(movement).Translate(Position);
    }

    void RotateMovement(Movement movement)
    {
        LookDirection = LookDirection.ApplyRotation(movement);
    }

    void Sync()
    {
        transform.position = Position.ToPositionFromXZPlane();
        transform.rotation = LookDirection.AsQuaternion();
    }

    void Update()
    {
        if (cInput.QueueDepth > 0)
        {
            var movement = cInput.GetMovement();

            if (movement.IsTranslation()) Translate(movement);
            if (movement.IsRotation()) RotateMovement(movement);
            
            Sync();
        }
    }
}
