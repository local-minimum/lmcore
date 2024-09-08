using LMCore.IO;
using UnityEngine;

namespace LMCore.Crawler
{
    /* KNOWN BUGS
     *
     *  There are pauses in the jumping
     *  
     *  A some of traversals are None
     *  
     *  The spinner goes crazy
     *  
     *  Ramps causes error
     *  
     *  Stepping up onto a ramp from the side makes odd transition 
     *  
     *  we are not carried by movable platforms
     *  
     *  stepping up off a ladder feels like it skips one part of the animation
     *  
     */

    public delegate void MovementInterpretationEvent(
        GridEntity entity, 
        MovementInterpretation interpretation,
        int tickId,
        float duration);

    public class MovementInterpreter : MonoBehaviour
    {
        public event MovementInterpretationEvent OnMovement; 

        GridEntity _entity;
        GridEntity Entity { 
            get { 
                if (_entity == null)
                {
                    _entity = GetComponentInParent<GridEntity>();
                }
                return _entity; 
            } 
        }

        private void InterpretRefuse(MovementInterpretation interpretation)
        {
            var direction = interpretation.PrimaryDirection;
            var origin = interpretation.Last;

            interpretation.Outcome = MovementInterpretationOutcome.Bouncing;
            interpretation.Steps.Add(new MovementCheckpointWithTransition() {
                Checkpoint = MovementCheckpoint.From(origin.Checkpoint, direction, origin.Checkpoint.LookDirection),
                Transition = origin.Transition,
            });
            interpretation.Steps.Add(new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(origin.Checkpoint, Direction.None, origin.Checkpoint.LookDirection),
                Transition = origin.Transition,
            });

        }

        private bool InterpretRoundOuterCorner(MovementInterpretation interpretation)
        {
            var origin = interpretation.Last;
            var originAnchor = origin.Checkpoint.Anchor;
            if (originAnchor == null) return false;

            // First one step in primary direction
            var intermediaryCoordinates = interpretation.PrimaryDirection.Translate(origin.Checkpoint.Coordinates);
            // Then to make an outer corner we need to move in the direction of the starting anchor
            var targetCoordinates = originAnchor.CubeFace.Translate(intermediaryCoordinates);

            if (!Entity.Dungeon.HasNodeAt(targetCoordinates)) return false;

            var target = Entity.Dungeon[targetCoordinates];

            if (!target.AllowsEntryFrom(Entity, originAnchor.CubeFace.Inverse())) return false;

            var targetAnchorDirection = interpretation
                .PrimaryDirection
                .Inverse();

            if (!target.CanAnchorOn(Entity, targetAnchorDirection)) return false;

            Debug.Log($"Outer corner movement confirmed");

            var targetAnchor = target.GetAnchor(targetAnchorDirection);

            var lookDirection = origin.Checkpoint.LookDirection;
            if (!Entity.RotationRespectsAnchorDirection)
            {
                if (targetAnchorDirection.IsPlanarCardinal())
                {
                    lookDirection = targetAnchorDirection;
                } else
                {
                    lookDirection = origin.Checkpoint.LookDirection;
                }
            }
            // Adding intermediary point
            interpretation.Steps.Add(new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(
                    targetAnchor, 
                    originAnchor.CubeFace.Inverse(),
                    lookDirection
                    ),
                Transition = MovementTransition.Grounded,
            });

            if (Entity.RotationRespectsAnchorDirection)
            {
                if (interpretation.PrimaryDirection == Entity.LookDirection)
                {
                    lookDirection = origin.Checkpoint.LookDirection.PitchDown(Entity.Down, out var _);
                } else if (interpretation.PrimaryDirection.Inverse() == Entity.LookDirection)
                {
                    lookDirection = origin.Checkpoint.LookDirection.PitchUp(Entity.Down, out var _);
                }
            }

            interpretation.Steps.Add(new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(targetAnchor, Direction.None, lookDirection),
                Transition = MovementTransition.Grounded,
            });

            return true;
        }

        private void InterpretTargetNode(MovementInterpretation interpretation, IDungeonNode targetNode)
        {
            var direction = interpretation.PrimaryDirection;
            var origin = interpretation.Last;

            if (targetNode.AllowsEntryFrom(Entity, direction.Inverse()))
            {
                var wantedAnchorDirection = origin.Checkpoint.Anchor == null ? direction : origin.Checkpoint.Anchor.CubeFace;
                if (targetNode.CanAnchorOn(Entity, wantedAnchorDirection))
                {
                    // Normal anchoring on same cube face

                    interpretation.Outcome = origin.Transition == MovementTransition.Grounded 
                        ? MovementInterpretationOutcome.Grounded : MovementInterpretationOutcome.Landing;

                    var targetAnchor = targetNode.GetAnchor(wantedAnchorDirection);

                    if (targetAnchor == null)
                    {
                        Debug.LogError(
                            $"{targetNode} @ {targetNode.Coordinates} lacks anchor in direction {wantedAnchorDirection} though it says {Entity.name} can anchor there."
                            );
                        interpretation.Steps.Add(new MovementCheckpointWithTransition()
                        {
                            Checkpoint = MovementCheckpoint.From(
                                targetNode, 
                                wantedAnchorDirection,
                                interpretation.Last.Checkpoint.LookDirection),
                            Transition = MovementTransition.Grounded,
                        });
                    } else if (targetAnchor.HasEdge(direction.Inverse()))
                    {
                        // Intermediary step
                        interpretation.Steps.Add(new MovementCheckpointWithTransition()
                        {
                            Checkpoint = MovementCheckpoint.From(
                                targetAnchor, 
                                direction.Inverse(),
                                interpretation.Last.Checkpoint.LookDirection),
                            Transition = MovementTransition.Grounded,
                        });
                        interpretation.Steps.Add(new MovementCheckpointWithTransition()
                        {
                            Checkpoint = MovementCheckpoint.From(
                                targetAnchor, 
                                Direction.None,
                                interpretation.Last.Checkpoint.LookDirection),
                            Transition = MovementTransition.Grounded,
                        });
                    }
                    else
                    {
                        interpretation.Steps.Add(new MovementCheckpointWithTransition()
                        {
                            Checkpoint = MovementCheckpoint.From(
                                targetAnchor, 
                                Direction.None,
                                interpretation.Last.Checkpoint.LookDirection),
                            Transition = MovementTransition.Grounded,
                        });
                    }
                    return;
                }

                if (InterpretRoundOuterCorner(interpretation)) { 
                    return; 
                }

                if (targetNode.GetAnchor(direction) == null)
                {
                    // Falling or flying through a tile
                    Debug.Log("Flying or falling through tile");
                    if (origin.Transition == MovementTransition.Grounded)
                    {
                        origin.Transition = MovementTransition.Jump;
                        Debug.Log($"Reclassifying prior transition as a jump: {origin}");
                    }
                    interpretation.Outcome = MovementInterpretationOutcome.Airbourne;
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(targetNode, Direction.None, interpretation.Last.Checkpoint.LookDirection),
                        Transition = MovementTransition.Ungrounded,
                    });
                }
                else
                {
                    if (origin.Transition == MovementTransition.Grounded)
                    {
                        origin.Transition = MovementTransition.Jump;
                    }
                    if (!Entity.TransportationMode.HasFlag(TransportationMode.Flying))
                    {
                        // Bouncing off wall/floor in the direction of the movement
                        interpretation.Outcome = MovementInterpretationOutcome.Bouncing;
                        interpretation.Steps.Add(new MovementCheckpointWithTransition()
                        {
                            Checkpoint = MovementCheckpoint.From(targetNode, direction, interpretation.Last.Checkpoint.LookDirection),
                            Transition = MovementTransition.Ungrounded
                        });
                    } else
                    {
                        // Flying into new tile
                        interpretation.Outcome = MovementInterpretationOutcome.Airbourne;
                    }
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(targetNode, Direction.None, interpretation.Last.Checkpoint.LookDirection),
                        Transition = MovementTransition.Ungrounded
                    });
                }
            } else
            {
                // Refusing movement
                InterpretRefuse(interpretation);
            }
        }

        private void InterpretByDungeon(MovementInterpretation interpretation)
        {
            var direction = interpretation.PrimaryDirection;
            var origin = interpretation.Steps[interpretation.Steps.Count - 1];

            var targetCoordinates = direction.Translate(Entity.Coordinates);
            if (Entity.Dungeon.HasNodeAt(targetCoordinates))
            {
                // Going to a new tile / entering a dungeon
                InterpretTargetNode(interpretation, Entity.Dungeon[targetCoordinates]);
            } else
            {
                // Flying or falling outside the dungeon
                if (origin.Transition == MovementTransition.Grounded)
                {
                    origin.Transition = MovementTransition.Jump;
                    interpretation.Outcome = MovementInterpretationOutcome.Airbourne;
                }
                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                {
                    Checkpoint = MovementCheckpoint.From(
                        targetCoordinates, 
                        Direction.None, 
                        interpretation.Last.Checkpoint.LookDirection),
                    Transition = MovementTransition.Ungrounded
                });
            }
        }

        public MovementInterpretation InterpretMovement(Movement movement)
        {
            if (movement.IsTranslation())
            {
                var direction = Entity.LookDirection.RelativeTranslation3D(Entity.Down, movement);
                if (!Entity.RotationRespectsAnchorDirection && Entity.Anchor.IsPlanarCardinal())
                {
                    if (Entity.LookDirection == Entity.Anchor)
                    {
                        switch (movement)
                        {
                            case Movement.StrafeLeft:
                            case Movement.StrafeRight:
                                // These movements follow normal rules
                                break;
                            case Movement.Forward:
                                direction = Entity.LookDirection.PitchUp(Direction.Down, out var _);
                                break;
                            case Movement.Backward:
                                direction = Entity.LookDirection.PitchDown(Direction.Down, out var _);
                                break;
                        } 
                    } else
                    {
                        Debug.LogWarning($"Movement {movement} for {Entity} not possible becuase enitity is on wall without respecting wall down and not looking at the wall");
                    }
                }

                Debug.Log($"Interpret {movement} for {Entity} is {direction}");
                return InterpretMovement(direction);
            } else if (movement.IsRotation())
            {
                if (Entity.Node?.AllowsRotating(Entity) != true) return null;

                var interpretation = new MovementInterpretation() { 
                    DurationScale = Entity.Abilities.turnDurationScaleFactor,
                };

                var startCheckpoint = MovementCheckpoint.From(Entity);
                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                {
                    Checkpoint = startCheckpoint,
                    Transition = Entity.TransportationMode.HasFlag(TransportationMode.Flying) ? MovementTransition.Grounded : MovementTransition.Ungrounded
                });

                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                {
                    Checkpoint = MovementCheckpoint.From(
                        startCheckpoint, 
                        Direction.None, 
                        startCheckpoint.LookDirection.ApplyRotation(Entity.Down, movement, out var _)),
                    Transition = Entity.TransportationMode.HasFlag(TransportationMode.Flying) ? MovementTransition.Grounded : MovementTransition.Ungrounded
                });

                return interpretation;

            }

            return null;
        }

        public MovementInterpretation InterpretMovement(Direction direction)
        {
            var interpretation = new MovementInterpretation() { 
                PrimaryDirection = direction 
            }; 

            var anchor = Entity.NodeAnchor;
            if (anchor == null)
            {
                var node = Entity.Node;

                if (node == null)
                {
                    // We start outside the dungeon
                    interpretation.Outcome = MovementInterpretationOutcome.Airbourne;
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(Entity),
                        Transition = MovementTransition.Ungrounded,                    
                    });
                    InterpretByDungeon(interpretation);
                } else
                {
                    // We start flying or falling
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(Entity),
                        Transition = MovementTransition.Ungrounded,
                    });

                    var outcome = node.AllowsMovement(Entity, Entity.Anchor, direction);
                    if (outcome == MovementOutcome.Refused)
                    {
                        interpretation.Steps.Add(interpretation.First);
                    } else if (outcome == MovementOutcome.Blocked)
                    {
                        InterpretRefuse(interpretation);
                    } else
                    {
                        var targetCoordinates = node.Neighbour(direction);
                        if (!Entity.Dungeon.HasNodeAt(targetCoordinates))
                        {
                            InterpretByDungeon(interpretation);
                        } else 
                        {
                            InterpretTargetNode(interpretation, Entity.Dungeon[targetCoordinates]);
                        }
                    }
                }
            } else {
                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                {
                    Checkpoint = MovementCheckpoint.From(Entity),
                    Transition = MovementTransition.Grounded,
                });

                var outcome = anchor.Node.AllowsMovement(Entity, anchor.CubeFace, direction);
                if (outcome == MovementOutcome.Refused)
                {
                    interpretation.Steps.Add(interpretation.First);
                }
                else if (outcome == MovementOutcome.Blocked)
                {
                    InterpretRefuse(interpretation);
                }
                else
                {
                    var targetAnchor = anchor.GetNeighbour(direction, out var sameNode);

                    // Intermediary step at edge of starting anchor
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(interpretation.First.Checkpoint, direction, Entity.LookDirection),
                        Transition = MovementTransition.Grounded,
                    });

                    if (sameNode)
                    {
                        // E.g. getting onto a ladder on the wall of the same node

                        if (targetAnchor == null)
                        {
                            Debug.LogError(
                                $"{anchor} says it has neighbour to {direction} in same node but there's no anchor there.");

                            InterpretRefuse(interpretation);
                            return interpretation;

                        }
                        var lookDirection = Entity.LookDirection;
                        if (Entity.RotationRespectsAnchorDirection)
                        {
                            if (Entity.LookDirection == direction)
                            {
                                lookDirection = direction.PitchUp(Entity.Down, out var _);
                            }
                            else if (Entity.LookDirection.Inverse() == direction)
                            {
                                lookDirection = direction.PitchDown(Entity.Down, out var _);
                            }
                        }
                        else if (targetAnchor.CubeFace.IsPlanarCardinal())
                        {
                            lookDirection = targetAnchor.CubeFace;
                        }
                        interpretation.Steps.Add(new MovementCheckpointWithTransition()
                        {
                            Checkpoint = MovementCheckpoint.From(targetAnchor, Direction.None, lookDirection),
                            Transition = MovementTransition.Grounded,
                        });
                    }
                    else if (anchor.Node.AllowExit(Entity, direction))
                    {
                        if (targetAnchor == null)
                        {
                            InterpretByDungeon(interpretation);
                        }
                        else
                        {
                            InterpretTargetNode(interpretation, targetAnchor.Node);
                        }
                    }
                    else
                    {
                        InterpretRefuse(interpretation);
                    }
                }
            }

            return interpretation;
        }

        private void OnEnable()
        {
            GetComponent<CrawlerInput>().OnMovement += MovementInterpreter_OnMovement;
        }

        private void OnDisable()
        {
           GetComponent<CrawlerInput>().OnMovement -= MovementInterpreter_OnMovement;
        }

        private void MovementInterpreter_OnMovement(int tickId, Movement movement, float duration)
        {
            var interpretation = InterpretMovement(movement);
            if (interpretation != null) OnMovement?.Invoke(Entity, interpretation, tickId, duration);
        }
    }
}
