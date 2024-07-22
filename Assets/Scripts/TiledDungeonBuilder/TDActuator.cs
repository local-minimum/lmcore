using LMCore.Crawler;
using System.Collections.Generic;
using System.Linq;
using TiledDungeon;
using TiledDungeon.Actions;
using UnityEngine;

public class TDActuator : MonoBehaviour
{
    [SerializeField, HideInInspector]
    Vector3Int Coordinates;

    [SerializeField, HideInInspector]
    int[] groups = new int[0];

    [SerializeField, HideInInspector]
    bool repeatable;

    [SerializeField]
    AbstractDungeonAction[] pressActions;

    bool active = true;

    public void Configure(TDNode node)
    {
        var props = node
            .GetObjectProps(o => 
                o.Type == TiledConfiguration.instance.ObjActuatorClass && 
                o.CustomProperties.Int(TiledConfiguration.instance.ObjGroupKey) > 0)
            .ToArray();

        groups = props
            .Select(prop => prop.Int(TiledConfiguration.instance.ObjGroupKey))
            .ToHashSet()
            .ToArray();

        foreach (var prop in props)
        {
            Debug.Log($"Actuator: {prop.Int(TiledConfiguration.instance.ObjGroupKey)}");
        }

        repeatable = props
            .Any(prop => prop.Bool(TiledConfiguration.instance.ObjRepeatableKey));

        Coordinates = node.Coordinates;
    }

    private void OnEnable()
    {
        GridEntity.OnInteract += GridEntity_OnInteract;
    }

    private void OnDisable()
    {
        
        GridEntity.OnInteract -= GridEntity_OnInteract;
    }

    private void GridEntity_OnInteract(GridEntity entity)
    {
        if (!active || entity.Position != Coordinates) return;

        foreach (var group in groups) {
            ToggleGroup.instance.Toggle(group);
            Debug.Log($"Toggling group {group}");
        }

        foreach (var action in pressActions)
        {
            action.Play(null);
        }

        active = repeatable;
        Debug.Log($"After press we are {active}");
    }

}
