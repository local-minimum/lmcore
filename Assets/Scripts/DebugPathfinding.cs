using LMCore.Crawler;
using LMCore.TiledDungeon;
using UnityEngine;

public class DebugPathfinding : MonoBehaviour
{
    [SerializeField]
    GridEntity entity;

    [SerializeField]
    GridEntity target;

    [SerializeField]
    Vector3Int start;

    [SerializeField]
    int maxSearch = 7;

    [ContextMenu("Info")]
    void Info()
    {
        var success = (entity.Dungeon as TiledDungeon).ClosestPath(entity, start, target.Coordinates, maxSearch, out var path, true);
        if (success)
        {
            Debug.Log($"Found path: {string.Join(", ", path)}");
        } else
        {
            Debug.Log($"Found no path from {start} to {target.Coordinates} for {entity.name} with max depth {maxSearch}");
        }
    }
}
