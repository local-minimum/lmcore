using LMCore.Crawler;
using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DummyNode : MonoBehaviour, IDungeonNode
{
    private IDungeon dungeon;
    public IDungeon Dungeon {
        get {
            if (dungeon == null) { 
                dungeon = GetComponentInParent<IDungeon>(); 
            }
            return dungeon;
        }
    }

    [SerializeField]
    private Vector3Int _Coordinates;
    public Vector3Int Coordinates => _Coordinates;

    public Vector3 CenterPosition => transform.position + Vector3.up * Dungeon.GridSize * 0.5f;

    public bool HasFloor => true;

    public bool Walkable => true;

    public bool Flyable => true;

    public void AddOccupant(GridEntity entity)
    {
    }

    public bool AllowExit(GridEntity entity, Direction direction) => true;

    public bool AllowsEntryFrom(GridEntity entity, Direction direction, bool checkOccupancyRules = true) => true;


    public MovementOutcome AllowsTransition(GridEntity entity, Vector3Int origin, Direction originAnchorDirection, Direction direction, out Vector3Int targetCoordinates, out Anchor targetAnchor, bool checkOccupancyRule = true)
    {
        targetCoordinates = direction.Translate(origin);
        targetAnchor = null; 
        return MovementOutcome.NodeExit;
    }
    
    public bool AllowsRotating(GridEntity entity) => true;

    public bool CanAnchorOn(GridEntity entity, Direction anchor) => true;

    private Dictionary<Direction, Anchor> anchors;

    public Anchor GetAnchor(Direction direction) {
        if (anchors == null)
        {
            anchors = new Dictionary<Direction, Anchor>(
                GetComponentsInChildren<Anchor>()
                .Select(n => new KeyValuePair<Direction, Anchor>(n.CubeFace, n)));
        }

        if (anchors.ContainsKey(direction)) return anchors[direction];

        if (direction == Direction.None) return null;

        var aGO = new GameObject($"{direction}");
        aGO.transform.position = Coordinates.ToPosition(Dungeon.GridSize);
        aGO.transform.SetParent(transform);

        var a = aGO.AddComponent<Anchor>();
        a.Dungeon = Dungeon;
        a.Node = this;
        a.SetPrefabCubeFace(direction);
        
        anchors[direction] = a;

        return a;
    }

    public Vector3 GetEdge(Direction anchor) => GetAnchor(anchor) != null ?
        anchors[anchor].CenterPosition :
        CenterPosition + anchor.AsLookVector3D().ToDirection(Dungeon.GridSize * 0.5f);

    public Vector3 GetEdge(Direction anchor, Direction edge) => GetAnchor(anchor) != null ?
        anchors[anchor].GetEdgePosition(edge):
        CenterPosition
        + anchor.AsLookVector3D().ToDirection(Dungeon.GridSize * 0.5f)
        + edge.AsLookVector3D().ToDirection(Dungeon.GridSize * 0.5f);



    public Vector3Int Neighbour(Direction direction) =>
        direction.Translate(Coordinates);

    public bool MayInhabit(GridEntity entity, bool push = true) => true;
    public void RemoveOccupant(GridEntity entity)
    {
    }

    public void Reserve(GridEntity entity)
    {
    }

    public bool HasIllusorySurface(Direction direction) => false;

    public void RemoveReservation(GridEntity entity)
    {
    }

    public MovementOutcome AllowsTransition(GridEntity entity, Direction direction, out Vector3Int targetCoordinates, out Anchor targetAnchor, bool checkOccupancyRule = true)
    {
        targetAnchor = null;
        targetCoordinates = direction.Translate(Coordinates);
        return MovementOutcome.NodeExit;
    }

    public Vector3Int Neighbour(GridEntity entity, Direction direction, out Anchor targetAnchor)
    {
        targetAnchor = null;
        return direction.Translate(Coordinates);
    }
}
