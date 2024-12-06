using LMCore.Crawler;
using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DummyLevel : MonoBehaviour, IDungeon
{
    public List<DummyNode> nodes = new List<DummyNode>();

    [ContextMenu("Sync all nodes in childre")]
    void FetchAndSyncNodes()
    {
        nodes = GetComponentsInChildren<DummyNode>().ToList();

        for (int i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            Debug.Log(nodes[i].name);
            node.name = $"Node {i}: {node.Coordinates}";
            node.transform.position = node.Coordinates.ToPosition(GridSize);        
        }
    }

    public IDungeonNode this[Vector3Int coordinates] => nodes.Find(n => n.Coordinates == coordinates);

    public string MapName => "Dummy";

    public float GridSize => 3;

    public GridEntity Player => null;

    public List<IDungeonNode> FindTeleportersById(int id)
    {
        return null;
    }

    public bool HasNodeAt(Vector3Int coordinates) => nodes.Any(n => n.Coordinates == coordinates);

    public Vector3 Position(GridEntity entity)
    {
        return entity.Coordinates.ToPosition(GridSize);
    }

    public Vector3 Position(Vector3Int coordinates, Direction anchor, bool rotationRespectsAnchorDirection)
    {
        return coordinates.ToPosition(GridSize) + anchor.AsLookVector3D().ToDirection(GridSize * 0.5f);
    }

    public GridEntity GetEntity(string identifier)
    {
        return null;
    }
}
