using LMCore.IO;
using UnityEngine;

namespace LMCore.Crawler
{
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

        private void InterpretBlocked(MovementInterpretation interpretation, string reason)
        {
            Debug.LogWarning($"Blocked movement detected for {Entity.name} @ {Entity.Coordinates}: {reason}");
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
        
        /// <summary>
        /// Checking for transition from one node via another to the target.
        /// 
        /// Climbing up from highest elevation ramp onto the next elevation
        /// Moving onto a high elevation ramp from an above elevation
        /// 
        /// Note that there is no dynamic discovery of these once the interpretation 
        /// has been created.
        /// </summary>
        /// <returns>If movement is allowed</returns>
        private bool ElevationOffsetEdge(MovementInterpretation interpretation)
        {
            var flying = Entity.TransportationMode.HasFlag(TransportationMode.Flying);
            var origin = interpretation.Last;
            if (!interpretation.PrimaryDirection.IsPlanarCardinal() ||
                !(origin.Checkpoint.AnchorDirection == Direction.Down ||
                flying && origin.Checkpoint.AnchorDirection == Direction.None))
                return false;

            var originNode = origin.Checkpoint.Node;
            if (originNode == null) return false;

            var originDown = originNode.GetAnchor(Direction.Down);
            if (originDown == null) return false;

            var originEdge = originDown.GetEdgePosition(interpretation.PrimaryDirection);
            var lookDirection = origin.Checkpoint.LookDirection;
            var dungeon = Entity.Dungeon;

            // Climb up off high ramps
            var targetCoordinates = interpretation.PrimaryDirection.Translate(origin.Checkpoint.Coordinates) + Vector3Int.up;
            if (dungeon.HasNodeAt(targetCoordinates)) {
                var target = dungeon[targetCoordinates]; 
                if (target.AllowsEntryFrom(Entity, interpretation.PrimaryDirection.Inverse()))
                {
                    var intermediaryCoordinates = origin.Checkpoint.Coordinates + Vector3Int.up;
                    if (!dungeon.HasNodeAt(intermediaryCoordinates) || 
                        dungeon[intermediaryCoordinates].AllowsEntryFrom(Entity, Direction.Down) 
                        && dungeon[intermediaryCoordinates].AllowExit(Entity, interpretation.PrimaryDirection))
                    {
                        var anchor = target.GetAnchor(Direction.Down);
                        if (flying)
                        {
                            // TODO: this should check the verticality of scaleheight compared to the floor of above tile even if there's none
                        } else if (anchor != null)
                        {
                            var edgeDelta = anchor.GetEdgePosition(interpretation.PrimaryDirection.Inverse()) - originEdge;
                            var verticality = Vector3.Dot(edgeDelta, Vector3.up);
                            var planarGap = Mathf.Abs(Vector3.Dot(edgeDelta, interpretation.PrimaryDirection.AsLookVector3D()));

                            if (verticality <= Entity.Abilities.minScaleHeight && planarGap < Entity.Abilities.maxForwardJump ||
                                verticality < Entity.Abilities.maxScaleHeight && planarGap < Entity.Abilities.minForwardJump)
                            {
                                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                                {
                                    Checkpoint = MovementCheckpoint.From(originDown, interpretation.PrimaryDirection, lookDirection),
                                    Transition = planarGap < Entity.Abilities.minForwardJump ? MovementTransition.Grounded : MovementTransition.Jump,
                                });

                                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                                {
                                    Checkpoint = MovementCheckpoint.From(anchor, interpretation.PrimaryDirection.Inverse(), lookDirection),
                                    Transition = MovementTransition.Grounded,
                                });

                                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                                {
                                    Checkpoint = MovementCheckpoint.From(anchor, Direction.None, lookDirection),
                                    Transition = MovementTransition.Grounded,
                                });

                                return true;
                            }
                        }
                    }
                }
            }

            // Climbing down onto a high ramp
            targetCoordinates = interpretation.PrimaryDirection.Translate(origin.Checkpoint.Coordinates) + Vector3Int.down;
            if (dungeon.HasNodeAt(targetCoordinates))
            {
                var target = dungeon[targetCoordinates];
                if (target.AllowsEntryFrom(Entity, Direction.Up))
                {
                    var intermediaryCoordinates = interpretation.PrimaryDirection.Translate(origin.Checkpoint.Coordinates);
                    if (!dungeon.HasNodeAt(intermediaryCoordinates) ||
                        dungeon[intermediaryCoordinates].AllowsEntryFrom(Entity, interpretation.PrimaryDirection.Inverse())
                        && dungeon[intermediaryCoordinates].AllowExit(Entity, Direction.Down))
                    {
                        var anchor = target.GetAnchor(Direction.Down);
                        if (flying)
                        {
                            // TODO: this should check the verticality of scaleheight compared to the floor of above tile even if there's none
                        } else if (anchor != null)
                        {
                            var edgeDelta = originEdge - anchor.GetEdgePosition(interpretation.PrimaryDirection.Inverse());
                            var verticality = Mathf.Abs(Vector3.Dot(edgeDelta, Vector3.up));
                            var planarGap = Mathf.Abs(Vector3.Dot(edgeDelta, interpretation.PrimaryDirection.AsLookVector3D()));

                            if (verticality <= Entity.Abilities.minScaleHeight && planarGap < Entity.Abilities.maxForwardJump ||
                                verticality < Entity.Abilities.maxScaleHeight && planarGap < Entity.Abilities.minForwardJump)
                            {
                                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                                {
                                    Checkpoint = MovementCheckpoint.From(originDown, interpretation.PrimaryDirection, lookDirection),
                                    Transition = planarGap < Entity.Abilities.minForwardJump ? MovementTransition.Grounded : MovementTransition.Jump,
                                });

                                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                                {
                                    Checkpoint = MovementCheckpoint.From(anchor, interpretation.PrimaryDirection.Inverse(), lookDirection),
                                    Transition = MovementTransition.Grounded,
                                });

                                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                                {
                                    Checkpoint = MovementCheckpoint.From(anchor, Direction.None, lookDirection),
                                    Transition = MovementTransition.Grounded,
                                });

                                return true;
                            }
                        }
                    }

                }
            }

            return false;
        }

        private bool InterpretRoundOuterCorner(MovementInterpretation interpretation)
        {
            var origin = interpretation.First;
            var originAnchor = origin.Checkpoint.Anchor;
            // Debug.Log($"Checking outer corner on:\n'{interpretation}'");
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

            // Debug.Log($"Outer corner movement intermediary:\n{interpretation}");
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

            // Add the edge step of the first node
            interpretation.Steps.Add(new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(originAnchor, interpretation.PrimaryDirection, lookDirection),
                Transition = MovementTransition.Grounded,
            });

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

            // Debug.Log($"Outer corner movement second intermediary:\n{interpretation}");

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

            Debug.Log($"Outer corner movement confirmed:\n{interpretation}");
            return true;
        }

        private bool IsOuterCorner(Anchor start, Anchor end) =>
            start != null && 
            end != null &&
            start.CubeFace.Translate(start.Node.Coordinates) == end.CubeFace.Translate(end.Node.Coordinates);

        private void InterpretTargetNode(MovementInterpretation interpretation, IDungeonNode targetNode, bool allowEntry)
        {
            var direction = interpretation.PrimaryDirection;
            var origin = interpretation.Last;

            if (allowEntry || targetNode.AllowsEntryFrom(Entity, direction.Inverse()))
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

                if (targetNode.GetAnchor(wantedAnchorDirection) == null)
                { 
                    if (Entity.TransportationMode.HasFlag(TransportationMode.Climbing)) 
                    { 
                        InterpretBlocked(
                            interpretation,
                            $"There's no anchor {wantedAnchorDirection} while we are climbing");
                        return; 
                    }

                    if (targetNode.HasIllusorySurface(wantedAnchorDirection))
                    {
                        interpretation.Outcome = MovementInterpretationOutcome.Airbourne;
                        interpretation.Steps.Add(new MovementCheckpointWithTransition()
                        {
                            Checkpoint = MovementCheckpoint.From(targetNode, wantedAnchorDirection, interpretation.Last.Checkpoint.LookDirection),
                            Transition = MovementTransition.Ungrounded,
                        });
                    } else
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
                }
                else
                {
                    // There's an achor on the same surface direction as the movement
                    // started, but we aren't allowed on it.
                    if (origin.Transition == MovementTransition.Grounded)
                    {
                        origin.Transition = MovementTransition.Jump;
                    }

                    interpretation.Outcome = MovementInterpretationOutcome.Airbourne;

                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(targetNode, Direction.None, interpretation.Last.Checkpoint.LookDirection),
                        Transition = MovementTransition.Ungrounded
                    });
                }
            } else
            {
                // Refusing movement
                InterpretBlocked(
                    interpretation, 
                    $"Target node {targetNode.Coordinates} doesn't allow entry from {direction.Inverse()}");
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
                InterpretTargetNode(interpretation, Entity.Dungeon[targetCoordinates], false);
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
                if (!Entity.RotationRespectsAnchorDirection && Entity.AnchorDirection.IsPlanarCardinal())
                {
                    if (Entity.LookDirection == Entity.AnchorDirection)
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

                Debug.Log($"Interpret {movement} for {Entity} as {direction}");
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

        private void InterpretAnchorMovement(
            MovementInterpretation interpretation,
            Direction direction,
            MovementOutcome outcome,
            Anchor anchor,
            Anchor targetAnchor,
            bool trustExit)
        {
            // Intermediary step at edge of starting anchor
            interpretation.Steps.Add(new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(interpretation.First.Checkpoint, direction, Entity.LookDirection),
                Transition = MovementTransition.Grounded,
            });

            if (outcome == MovementOutcome.Blocked)
            {
                InterpretBlocked(
                    interpretation,
                    $"Anchor {anchor.CubeFace} says movement is blocked");
            } else if (outcome == MovementOutcome.Refused)
            {
                interpretation.Steps.Add(interpretation.First);
            }
            else if (outcome == MovementOutcome.NodeInternal)
            {
                // E.g. getting onto a ladder on the wall of the same node

                if (targetAnchor == null)
                {
                    Debug.LogError(
                        $"{anchor} says it has neighbour to {direction} in same node but there's no anchor there.");

                    InterpretBlocked(
                        interpretation, 
                        "The movement is inside a node but there's no anchor to attach to");
                    return ;

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
            else if (trustExit || anchor.Node.AllowExit(Entity, direction))
            {
                if (targetAnchor == null)
                {
                    InterpretByDungeon(interpretation);
                }
                else
                {
                    // Trusting exit is used when we step on to high ramps
                    InterpretTargetNode(interpretation, targetAnchor.Node, trustExit);
                }
            }
            else
            {
                InterpretBlocked(
                    interpretation, 
                    $"Anchor {anchor.Node.Coordinates} {anchor.CubeFace} doesn't allow exit in direction {direction}");
            }
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
                    if (!ElevationOffsetEdge(interpretation)) InterpretByDungeon(interpretation);
                } else
                {
                    // We start flying or falling
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(Entity),
                        Transition = MovementTransition.Ungrounded,
                    });

                    var outcome = node.AllowsTransition(
                        Entity, 
                        Entity.Coordinates,
                        Entity.AnchorDirection, 
                        direction,
                        out var _,
                        out var targetAnchor
                        );
                    if (outcome == MovementOutcome.Refused)
                    {
                        interpretation.Steps.Add(interpretation.First);
                    } else if (outcome == MovementOutcome.Blocked)
                    {
                        InterpretBlocked(
                            interpretation,
                            $"Node says {direction} is blocked for airbourne entity");
                    } else if (!ElevationOffsetEdge(interpretation))
                    {
                        var targetCoordinates = node.Neighbour(direction);
                        if (!Entity.Dungeon.HasNodeAt(targetCoordinates))
                        {
                            InterpretByDungeon(interpretation);
                        } else if (!IsOuterCorner(Entity.NodeAnchor, targetAnchor) || !InterpretRoundOuterCorner(interpretation))
                        {
                            InterpretTargetNode(interpretation, Entity.Dungeon[targetCoordinates], false);
                        }
                    }
                }
            } else {
                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                {
                    Checkpoint = MovementCheckpoint.From(Entity),
                    Transition = MovementTransition.Grounded,
                });

                var neighbour = anchor.GetNeighbour(direction, Entity, out var outcome);

                if (outcome == MovementOutcome.Refused)
                {
                    interpretation.Steps.Add(interpretation.First);
                    return interpretation;
                }
                else if (outcome == MovementOutcome.Blocked)
                {
                    Debug.LogWarning("Sentinel has said this is blocked");
                    InterpretBlocked(
                        interpretation,
                        $"Anchor {anchor.CubeFace} lacked neighbour to {direction} and said movement was blocked");
                    return interpretation;
                }

                if (neighbour != null)
                {
                    if (!IsOuterCorner(Entity.NodeAnchor, neighbour) || !InterpretRoundOuterCorner(interpretation))
                    {
                        InterpretAnchorMovement(
                            interpretation,
                            direction,
                            outcome,
                            anchor,
                            neighbour,
                            true);

                    }

                    return interpretation;
                }

                outcome = anchor.Node.AllowsTransition(
                    Entity, 
                    Entity.Coordinates, 
                    anchor.CubeFace,
                    direction,
                    out var _,
                    out var _);
                if (outcome == MovementOutcome.Refused)
                {
                    interpretation.Steps.Add(interpretation.First);
                }
                else if (outcome == MovementOutcome.Blocked)
                {
                    InterpretBlocked(
                        interpretation, 
                        $"Current node considers moving on {anchor.CubeFace} in {direction} direction blocked");
                }
                else if (!ElevationOffsetEdge(interpretation))
                {
                    if (direction == anchor.CubeFace && Entity.Falling)
                    {
                        // This only happens if we thought we were anchored but now are falling
                        var targetCoordinates = anchor.Node.Neighbour(direction);
                        if (!Entity.Dungeon.HasNodeAt(targetCoordinates))
                        {
                            InterpretByDungeon(interpretation);
                        }
                        else
                        {
                            InterpretTargetNode(interpretation, Entity.Dungeon[targetCoordinates], false);
                        }
                    }
                    else
                    {
                        var targetAnchor = anchor.GetNeighbour(direction, Entity, out outcome);
                        InterpretAnchorMovement(
                            interpretation,
                            direction,
                            outcome,
                            anchor,
                            targetAnchor,
                            false);

                    }
                }
            }

            return interpretation;
        }

        private void OnEnable()
        {
            var crawlerInput = GetComponent<CrawlerInput>();
            if (crawlerInput != null)
            {
                crawlerInput.OnMovement += MovementInterpreter_OnMovement;
            }
        }

        private void OnDisable()
        {
            var crawlerInput = GetComponent<CrawlerInput>();
            if (crawlerInput != null)
            {
                crawlerInput.OnMovement -= MovementInterpreter_OnMovement;
            }
        }

        int tickId;
        private void MovementInterpreter_OnMovement(int tickId, Movement movement, float duration)
        {
            this.tickId = tickId;
            var interpretation = InterpretMovement(movement);
            Debug.Log(interpretation);
            if (interpretation != null) OnMovement?.Invoke(Entity, interpretation, tickId, duration);
        }

        public void InvokeMovement(Movement movement, float duration)
        {
            var interpretation = InterpretMovement(movement);
            if (interpretation != null) OnMovement?.Invoke(Entity, interpretation, tickId, duration);
            tickId++;
        }
    }
}
