using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Crawler
{
    public enum MomventTransition { None, Grounded, Jump, Ungrounded };

    public struct MovementCheckpointWithTransition
    {
        public MovementCheckpoint Checkpoint { get; set; }
        public MomventTransition Transition { get; set; }
    }

    public enum MovementInterpretationOutcome
    {
        Grounded, Landing, Airbourne, Bouncing
    }

    public class  MovementInterpretation
    {
        public Direction PrimaryDirection { get; set; }
        public MovementInterpretationOutcome Outcome { get; set; }
        public List<MovementCheckpointWithTransition> Steps { get; set; } = new List<MovementCheckpointWithTransition>();

        public MovementCheckpointWithTransition Start => Steps[0];
        public MovementCheckpointWithTransition Finish => Steps.Last();
    }

    public class MovementInterpreter : MonoBehaviour
    {
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
            var origin = interpretation.Steps[interpretation.Steps.Count - 1];

            interpretation.Outcome = MovementInterpretationOutcome.Bouncing;
            interpretation.Steps.Add(new MovementCheckpointWithTransition() {
                Checkpoint = MovementCheckpoint.From(origin.Checkpoint, direction),
                Transition = origin.Transition,
            });
            interpretation.Steps.Add(new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(origin.Checkpoint),
                Transition = origin.Transition,
            });

        }

        private bool InterpretRoundOuterCorner(MovementInterpretation interpretation)
        {
            var origin = interpretation.Steps[interpretation.Steps.Count - 1];
            var originAnchor = origin.Checkpoint.Anchor;
            if (originAnchor == null) return false;

            var targetCoordinates = originAnchor.CubeFace 
                .Translate(interpretation.PrimaryDirection.Translate(origin.Checkpoint.Coordinates));

            if (!Entity.Dungeon.HasNodeAt(targetCoordinates)) return false;

            var target = Entity.Dungeon[targetCoordinates];

            if (!target.AllowsEntryFrom(Entity, originAnchor.CubeFace.Inverse())) return false;

            var targetAnchorDirection = interpretation
                .PrimaryDirection
                .PitchDown(originAnchor.CubeFace, out var _);

            if (!target.CanAnchorOn(Entity, targetAnchorDirection)) return false;

            var targetAnchor = target.GetAnchor(targetAnchorDirection);

            // Adding intermediary point
            interpretation.Steps.Add(new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(targetAnchor, originAnchor.CubeFace.Inverse()),
                Transition = MomventTransition.Grounded,
            });

            interpretation.Steps.Add(new MovementCheckpointWithTransition()
            {
                Checkpoint = MovementCheckpoint.From(targetAnchor),
                Transition = MomventTransition.Grounded,
            });

            return true;
        }

        private void InterpretTargetNode(MovementInterpretation interpretation, IDungeonNode targetNode)
        {
            var direction = interpretation.PrimaryDirection;
            var origin = interpretation.Steps[interpretation.Steps.Count - 1];

            if (targetNode.AllowsEntryFrom(Entity, direction.Inverse()))
            {
                var wantedAnchorDirection = origin.Checkpoint.Anchor == null ? direction : origin.Checkpoint.Anchor.CubeFace;
                if (targetNode.CanAnchorOn(Entity, wantedAnchorDirection))
                {
                    // Normal anchoring on same cube face


                    interpretation.Outcome = origin.Transition == MomventTransition.Grounded 
                        ? MovementInterpretationOutcome.Grounded : MovementInterpretationOutcome.Landing;

                    var targetAnchor = targetNode.GetAnchor(wantedAnchorDirection);
                    // Intermediary step
                    if (targetAnchor.HasEdge(direction.Inverse()))
                    {
                        interpretation.Steps.Add(new MovementCheckpointWithTransition()
                        {
                            Checkpoint = MovementCheckpoint.From(targetAnchor, direction.Inverse()),
                            Transition = MomventTransition.Grounded,
                        });
                    }

                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(targetAnchor),
                        Transition = MomventTransition.Grounded,
                    });
                    return;
                }

                if (InterpretRoundOuterCorner(interpretation)) { 
                    return; 
                }

                if (targetNode.GetAnchor(direction) == null)
                {
                    // Falling or flying through a tile
                    if (origin.Transition == MomventTransition.Grounded)
                    {
                        origin.Transition = MomventTransition.Jump;
                    }
                    interpretation.Outcome = MovementInterpretationOutcome.Airbourne;
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(targetNode),
                        Transition = MomventTransition.Ungrounded,
                    });
                }
                else
                {
                    if (origin.Transition == MomventTransition.Grounded)
                    {
                        origin.Transition = MomventTransition.Jump;
                    }
                    if (!Entity.TransportationMode.HasFlag(TransportationMode.Flying))
                    {
                        // Bouncing off wall/floor in the direction of the movement
                        interpretation.Outcome = MovementInterpretationOutcome.Bouncing;
                        interpretation.Steps.Add(new MovementCheckpointWithTransition()
                        {
                            Checkpoint = MovementCheckpoint.From(targetNode, direction),
                            Transition = MomventTransition.Ungrounded
                        });
                    } else
                    {
                        // Flying into new tile
                        interpretation.Outcome = MovementInterpretationOutcome.Airbourne;
                    }
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(targetNode),
                        Transition = MomventTransition.Ungrounded
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
                if (origin.Transition == MomventTransition.Grounded)
                {
                    origin.Transition = MomventTransition.Jump;
                    interpretation.Outcome = MovementInterpretationOutcome.Airbourne;
                }
                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                {
                    Checkpoint = MovementCheckpoint.From(targetCoordinates),
                    Transition = MomventTransition.Ungrounded
                });
            }
        }

        public MovementInterpretation InterpretMovement(Direction direction, float progress)
        {
            var interpretation = new MovementInterpretation() { PrimaryDirection = direction }; 

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
                        Transition = MomventTransition.Ungrounded,
                    });
                    InterpretByDungeon(interpretation);
                } else
                {
                    // We start flying or falling
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(Entity),
                        Transition = MomventTransition.Ungrounded,
                    });

                    var targetCoordinates = node.Neighbour(direction);
                    if (!Entity.Dungeon.HasNodeAt(targetCoordinates))
                    {
                        InterpretByDungeon(interpretation);
                    } else 
                    {
                        InterpretTargetNode(interpretation, Entity.Dungeon[targetCoordinates]);
                    }
                }
            } else {
                var targetAnchor = anchor.GetNeighbour(direction, out var sameNode);

                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                {
                    Checkpoint = MovementCheckpoint.From(Entity),
                    Transition = MomventTransition.Grounded,
                });
                // Intermediary step at edge of starting anchor
                interpretation.Steps.Add(new MovementCheckpointWithTransition()
                {
                    Checkpoint = MovementCheckpoint.From(interpretation.Start.Checkpoint, direction),
                    Transition = MomventTransition.Grounded,
                });

                if (sameNode)
                {
                    // E.g. getting onto a ladder on the wall of the same node
                    interpretation.Steps.Add(new MovementCheckpointWithTransition()
                    {
                        Checkpoint = MovementCheckpoint.From(targetAnchor),
                        Transition = MomventTransition.Grounded,
                    });
                } else if (anchor.Node.AllowExit(Entity, direction))
                {
                    if (targetAnchor == null)
                    {
                        InterpretByDungeon(interpretation);
                    } else
                    {
                        InterpretTargetNode(interpretation, targetAnchor.Node);
                    }
                } else
                {
                    InterpretRefuse(interpretation);
                }
            }

            return interpretation;
        }
    }
}
