using LMCore.Crawler;
using LMCore.TiledDungeon;
using UnityEngine;

public class DebugPathfinding : MonoBehaviour
{
    [Header("From (entity if start is 0,0,0)")]
    [SerializeField]
    GridEntity entity;

    [SerializeField]
    Vector3Int start;

    Vector3Int from => start == Vector3Int.zero ? entity.Coordinates : start;

    [Header("To (target else target coord)")]
    [SerializeField]
    GridEntity target;

    [SerializeField]
    Vector3Int targetCoordinates;

    Vector3Int to => target == null ? targetCoordinates : target.Coordinates;


    [Header("Settings")]
    [SerializeField]
    int maxSearch = 7;

    [ContextMenu("Info")]
    void Info()
    {

        var dungeon = (entity?.Dungeon as TiledDungeon);
        if (dungeon == null)
        {
            Debug.Log("Can't locate dungeon");
            return;
        }

        var success = dungeon.ClosestPath(entity, from, to, maxSearch, out var path, true);
        if (success)
        {
            Debug.Log($"Found path: {from}({entity.AnchorDirection}) {string.Join(", ", path)}");
        } else
        {
            Debug.Log($"Found no path from {from} to {to} for {entity.name} with max depth {maxSearch}");
        }
    }
}
