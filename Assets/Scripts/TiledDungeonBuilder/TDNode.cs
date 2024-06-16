using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
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
        GameObject grateNS;

        [SerializeField]
        GameObject grateWE;

        [SerializeField]
        string GrateClass = "Grate";

        [SerializeField]
        string OrientationClass = "Orientation";

        [SerializeField]
        string WalkableKey;
        public bool Walkable => !Obstructed && string.IsNullOrEmpty(WalkableKey) ? true : tile.CustomProperties.StringEnums.GetValueOrDefault(WalkableKey).Value == "Always";

        [SerializeField]
        string FlyableKey;
        public bool Flyable => !Obstructed && string.IsNullOrEmpty(FlyableKey) ? true : tile.CustomProperties.StringEnums.GetValueOrDefault(FlyableKey).Value == "Always";

        public bool HasFloor => floor != null && floor.activeSelf;

        public bool Obstructed { get; set; }

        public void ConfigureGrates(TileModification[] modifications)
        {
            var grates = modifications.Where(mod => mod.Tile.Type == GrateClass).ToList();

            grateNS?.SetActive(false);
            grateWE?.SetActive(false);

            if (grates.Count == 0)
            {
                Obstructed = false;
                return;
            }

            if (grates.Where(g => g.Tile.CustomProperties.StringEnums.GetValueOrDefault(OrientationClass).Value == "Vertical").Count() > 0) {
                if (grateNS != null)
                {
                    grateNS.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"Tile @ {Coordinates} doesn't support north<->south grates");
                }
            }

            if (grates.Where(g => g.Tile.CustomProperties.StringEnums.GetValueOrDefault(OrientationClass).Value == "Horizontal").Count() > 0) {
                if (grateWE != null)
                {
                    grateWE.SetActive(true);
                } else
                {
                    Debug.LogWarning($"Tile @ {Coordinates} doesn't support west<->east grates");
                }
            }

            Obstructed = true;
        }

        public void Configure(
            Vector3Int coordinates, 
            TiledTile tile, 
            TiledNodeRoofRule roofRule,
            TiledDungeon dungeon,
            TileModification[] modifications
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

            ConfigureGrates(modifications);
        }

        private void OnDestroy()
        {
            dungeon?.RemoveNode(this);
        }
    }   
}
