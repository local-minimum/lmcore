using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LMCore.Crawler;
using LMCore.Extensions;
using LMCore.IO;

public class GridEntity : MonoBehaviour
{
    [SerializeField]
    Direction StartLookDirection;

    public Vector2Int Position { get; set; }
    public Direction LookDirection { get; set; }

    private void Start()
    {
        LookDirection = StartLookDirection;
        Sync();
    }

    public void Sync()
    {
        transform.position = Position.ToPositionFromXZPlane();
        transform.rotation = LookDirection.AsQuaternion();
    }
    public void Translate(Movement movement)
    {
        Position = LookDirection.RelativeTranslation(movement).Translate(Position);
    }

    public void Rotate(Movement movement)
    {
        LookDirection = LookDirection.ApplyRotation(movement);
    }
}
