using LMCore.Extensions;
using System.Collections.Generic;
using TiledImporter;
using UnityEngine;

namespace TiledDungeon
{
    public enum TiledNodeRoofRule
    {
        CustomProps,
        ForcedSet,
        ForcedNotSet,
    }

    public class TDNode : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        TiledTile tile;

        [SerializeField]
        TiledDungeon dungeon;

        [SerializeField, HideInInspector]
        private Vector3Int _coordinates;
        public Vector3Int Coordinates => _coordinates;

        [SerializeField, Tooltip("Name of custom properties class that has boolean fields for Down, Up, North, West, East, South")]
        string SidesClass = "Sides";

        [SerializeField]
        GameObject floor;

        [SerializeField]
        GameObject roof;

        [SerializeField]
        GameObject northWall;

        [SerializeField]
        GameObject southWall;

        [SerializeField]
        GameObject westWall;

        [SerializeField]
        GameObject eastWall;

        [SerializeField]
        string WalkableKey;
        public bool Walkable => string.IsNullOrEmpty(WalkableKey) ? true : tile.CustomProperties.Bools.GetValueOrDefault(WalkableKey);

        [SerializeField]
        string FlyableKey;
        public bool Flyable=> string.IsNullOrEmpty(FlyableKey) ? true : tile.CustomProperties.Bools.GetValueOrDefault(FlyableKey);

        public bool HasFloor => floor != null && floor.activeSelf;

        public void Configure(
            Vector3Int coordinates, 
            TiledTile tile, 
            TiledNodeRoofRule roofRule,
            TiledDungeon dungeon
        )
        {
            _coordinates = coordinates;
            this.tile = tile;
            this.dungeon = dungeon;

            var sides = tile.CustomProperties.Classes[SidesClass];
            if (sides == null)
            {
                Debug.LogError($"{tile} as {coordinates} lacks a sides class, can't be used for layouting");
            } else
            {
                floor.SetActive(sides.Bools.GetValueOrDefault("Down"));
                roof.SetActive(
                    roofRule == TiledNodeRoofRule.CustomProps ? sides.Bools.GetValueOrDefault("Up") : roofRule == TiledNodeRoofRule.ForcedSet
                );
                westWall.SetActive(sides.Bools.GetValueOrDefault("West"));
                southWall.SetActive(sides.Bools.GetValueOrDefault("South"));
                northWall.SetActive(sides.Bools.GetValueOrDefault("North"));
                eastWall.SetActive(sides.Bools.GetValueOrDefault("East"));
            }

            transform.localPosition = coordinates.ToPosition(dungeon.Scale);
            name = $"TileNode Elevation {coordinates.y} ({coordinates.x}, {coordinates.z})";
        }

        private void OnDestroy()
        {
            dungeon?.RemoveNode(this);
        }
    }   
}
