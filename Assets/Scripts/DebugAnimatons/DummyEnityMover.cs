using LMCore.Crawler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyEnityMover : MonoBehaviour
{
    public Direction PrimaryDirection;
    public MovementInterpretationOutcome Outcome;

    public DummyNode StartNode;
    public Direction StartAnchor;
    public Direction StartLook;

    public DummyNode EndNode;
    public Direction EndAnchor;
    public Direction EndLook;

    public MovementTransition transition;

    public GridEntity GridEntity;

    MovementInterpretation interp;

    [SerializeField, Range(0, 1)]
    float progress;

    [ContextMenu("Restart animation")]
    private void Start()
    {
        GridEntity.RotationRespectsAnchorDirection = false;
        GridEntity.NodeAnchor = StartNode.GetAnchor(StartAnchor);
        GridEntity.LookDirection = StartLook;
        
        GridEntity.Sync();

        interp = new MovementInterpretation()
        {
            PrimaryDirection = PrimaryDirection,
            Outcome = Outcome,
        };

        interp.Steps.Add(new MovementCheckpointWithTransition()
        {
            Checkpoint = MovementCheckpoint.From(GridEntity),
            Transition = transition,
        });

        interp.Steps.Add(new MovementCheckpointWithTransition()
        {
            Checkpoint = MovementCheckpoint.From(EndNode, EndAnchor, EndLook),
            Transition = MovementTransition.Grounded,
        });

        progress = 0;
    }

    private void Update()
    {
        if (interp == null) { return; }

        GridEntity.transform.position = interp.Evaluate(GridEntity, progress, out var rotation, out var checkpoint, out var stepProgress);
        GridEntity.transform.rotation = rotation;
    }
}
