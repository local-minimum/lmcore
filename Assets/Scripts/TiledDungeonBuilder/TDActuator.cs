using LMCore.Crawler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TiledDungeon;
using TiledDungeon.Actions;
using TiledDungeon.Integration;
using UnityEngine;

public class TDActuator : MonoBehaviour
{
    [SerializeField, HideInInspector]
    Vector3Int Coordinates;

    [SerializeField, HideInInspector]
    int[] groups = new int[0];

    [SerializeField, HideInInspector]
    bool repeatable;

    [SerializeField, HideInInspector]
    TDEnumInteraction interaction;

    [SerializeField, HideInInspector]
    Direction anchor;

    [SerializeField]
    AbstractDungeonAction[] pressActions;

    [SerializeField]
    AbstractDungeonAction[] dePressAction;

    bool automaticallyResets => dePressAction == null || dePressAction.Length == 0;

    bool active = true;
    bool lastActionWasPress = false;
    public void Configure(TDNode node)
    {
        var props = node
            .GetObjectProps(o => 
                o.Type == TiledConfiguration.instance.ObjActuatorClass && 
                o.CustomProperties.Int(TiledConfiguration.instance.ObjGroupKey) > 0)
            .ToArray();

        interaction = props
            .FirstOrDefault(prop => prop.StringEnums.ContainsKey(TiledConfiguration.instance.InteractionKey))
            ?.Interaction(TiledConfiguration.instance.InteractionKey) ?? TDEnumInteraction.Interactable;

        anchor = props
            .FirstOrDefault(prop => prop.StringEnums.ContainsKey(TiledConfiguration.instance.AnchorKey))
            ?.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() ?? Direction.None;

        groups = props
            .Select(prop => prop.Int(TiledConfiguration.instance.ObjGroupKey))
            .ToHashSet()
            .ToArray();

        repeatable = props
            .Any(prop => prop.Bool(TiledConfiguration.instance.ObjRepeatableKey));

        Coordinates = node.Coordinates;

        Debug.Log($"Actuator @ {Coordinates}: Interaction({interaction}) Anchor({anchor}) Groups([{string.Join(", ", groups)}]) Repeatable({repeatable})");
    }

    private void OnEnable()
    {
        if (interaction == TDEnumInteraction.Interactable)
        {
            GridEntity.OnInteract += GridEntity_OnInteract;
        } else if (interaction == TDEnumInteraction.Automatic)
        {
            foreach (var mover in Movers.movers)
            {
                mover.OnMoveStart += Mover_OnMoveStart;
                mover.OnMoveEnd += Mover_OnMoveEnd;
            }

            Movers.OnActivateMover += Movers_OnActivateMover;
            Movers.OnDeactivateMover += Movers_OnDeactivateMover;
        }
    }
    private void OnDisable()
    {
        if (interaction == TDEnumInteraction.Interactable)
        {
            GridEntity.OnInteract -= GridEntity_OnInteract;
        } else if (interaction == TDEnumInteraction.Automatic)
        {
            foreach (var mover in Movers.movers)
            {
                mover.OnMoveStart -= Mover_OnMoveStart;
                mover.OnMoveEnd -= Mover_OnMoveEnd;
            }

            Movers.OnActivateMover -= Movers_OnActivateMover;
            Movers.OnDeactivateMover -= Movers_OnDeactivateMover;
        }
    }

    private void Movers_OnActivateMover(IEntityMover mover)
    {
        mover.OnMoveStart += Mover_OnMoveStart;
        mover.OnMoveEnd += Mover_OnMoveEnd;
    }

    private void Movers_OnDeactivateMover(IEntityMover mover)
    {
        mover.OnMoveStart -= Mover_OnMoveStart;
        mover.OnMoveEnd -= Mover_OnMoveEnd;
    }


    GridEntity occupant;
    private void Mover_OnMoveStart(GridEntity entity, List<Vector3Int> positions, List<Direction> anchors)
    {
        if (!active) return;
        var passesPlate = positions
            .Zip(anchors, (pos, anch) => new { pos, anch })
            .Skip(1)
            .Any(state => state.pos == Coordinates && state.anch == anchor);

        if (passesPlate && (automaticallyResets || !lastActionWasPress))
        {
            occupant = entity;
            Press();
        }
    }

    private void Mover_OnMoveEnd(GridEntity enity, bool successful)
    {
        if (!active) return;
        if (occupant != null && lastActionWasPress && (enity.Position != Coordinates || enity.Anchor != anchor))
        {
            Depress();
            occupant = null;
        }
    }

    private void GridEntity_OnInteract(GridEntity entity)
    {
        if (!active || entity.Position != Coordinates) return;

        if (lastActionWasPress && !automaticallyResets)
        {
            Depress();
        } else
        {
            Press();
        }
    }

    void Press()
    {
        foreach (var group in groups) {
            ToggleGroup.instance.Toggle(group);
            Debug.Log($"Actuator {name} @ {Coordinates} is toggling group: {group}");
        }

        foreach (var action in pressActions)
        {
            action.Play(null);
        }

        lastActionWasPress = true;

        active = repeatable;
        Debug.Log($"Actuator {name} @ {Coordinates} is {(active ? "active" : "inactive")} after interaction");
    }

    void Depress()
    {
        foreach (var action in dePressAction)
        {
            action.Play(null);
        }

        lastActionWasPress = false;
    }
}
