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

        [SerializeField, HideInInspector]
        TileModification[] modifications;

        [SerializeField, HideInInspector]
        TiledObjectLayer.Point[] Points;

        [SerializeField, HideInInspector]
        TiledObjectLayer.Rect[] Rects;

        TiledDungeon _dungeon;
        public TiledDungeon Dungeon
        {
            get
            {
                if (_dungeon == null)
                {
                    _dungeon = GetComponentInParent<TiledDungeon>();
                }
                return _dungeon;
            }

            private set { _dungeon = value; }
        }

        [SerializeField, HideInInspector]
        private Vector3Int _coordinates;
        public Vector3Int Coordinates
        {
            get => _coordinates;
            set => _coordinates = value;
        }

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
        GameObject obstructionNS;

        [SerializeField]
        GameObject obstructionWE;

        [SerializeField]
        TDDoor doorNS;

        [SerializeField]
        TDDoor doorWE;

        string ObstructionClass = "Obstruction";

        string DoorClass = "Door";

        [SerializeField]
        GameObject ladderN;

        [SerializeField]
        GameObject ladderW;

        [SerializeField]
        GameObject ladderS;

        [SerializeField]
        GameObject ladderE;

        string LadderClass = "Ladder";
        
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

        void ConfigureOriented(
            TileModification[] modifications,
            GameObject vertical,
            GameObject horizontal,
            System.Func<TileModification, bool> modFilter,
            bool obstructs
        )
        {
            var grates = modifications.Where(modFilter).ToList();

            vertical?.SetActive(false);
            horizontal?.SetActive(false);

            if (grates.Count == 0)
            {
                if (obstructs) Obstructed = false;
                return;
            }

            if (grates.Where(g => g.Tile.CustomProperties.StringEnums.GetValueOrDefault(OrientationClass).Value == "Vertical").Count() > 0) {
                if (vertical != null)
                {
                    vertical.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"Tile @ {Coordinates} doesn't support north<->south entity");
                }
            }

            if (grates.Where(g => g.Tile.CustomProperties.StringEnums.GetValueOrDefault(OrientationClass).Value == "Horizontal").Count() > 0) {
                if (horizontal != null)
                {
                    horizontal.SetActive(true);
                } else
                {
                    Debug.LogWarning($"Tile @ {Coordinates} doesn't support west<->east entity");
                }
            }

            if (obstructs) Obstructed = true;
        } 

        void ConfigureGrates(TileModification[] modifications) =>
            ConfigureOriented(
                modifications,
                grateNS,
                grateWE,
                mod => mod.Tile.Type == GrateClass,
                true
            );

        void ConfigureObstructions(TileModification[] modifications) =>
            ConfigureOriented(
                modifications,
                obstructionNS,
                obstructionWE,
                mod => mod.Tile.Type == ObstructionClass,
                true
            );

        void ConfigureDoors(TileModification[] modifications)
        {
            System.Func<TileModification, bool> filter =
                mod => mod.Tile.Type == DoorClass;

            ConfigureOriented(
                modifications,
                doorNS.gameObject,
                doorWE.gameObject,
                filter,
                true
            );

            foreach (TDDoor door in new[] { doorNS, doorWE })
            {
                if (door == null || !door.gameObject.activeSelf) continue;

                door.Configure(
                    Coordinates, 
                    modifications.Where(filter).ToArray(),
                    Points,
                    Rects
                );
            }
        }

        void ConfigureLadders(TileModification[] modifications)
        {
            System.Func<string, bool> hasLadder = direction =>
                modifications.Length > 0 &&
                modifications.Any(mod => 
                    mod.Tile.Type == LadderClass 
                    && mod.Tile.CustomProperties.StringEnums.GetValueOrDefault("Anchor")?.Value == direction
                );

            
            ladderN.SetActive(hasLadder("North"));
            ladderW.SetActive(hasLadder("West"));
            ladderS.SetActive(hasLadder("South"));
            ladderE.SetActive(hasLadder("East"));
        }

        public void Configure(
            TiledTile tile, 
            TiledNodeRoofRule roofRule,
            TiledDungeon dungeon,
            TileModification[] modifications,
            TiledObjectLayer.Point[] points,
            TiledObjectLayer.Rect[] rects
        )
        {
            this.tile = tile;
            this.modifications = modifications;
            Dungeon = dungeon;
            Points = points;
            Rects = rects;


            var sides = tile.CustomProperties.Classes[SidesClass];
            if (sides == null)
            {
                Debug.LogError($"{tile} as {Coordinates} lacks a sides class, can't be used for layouting");
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

            transform.localPosition = Coordinates.ToPosition(dungeon.Scale);
            name = $"TileNode Elevation {Coordinates.y} ({Coordinates.x}, {Coordinates.z})";

            ConfigureGrates(modifications);
            ConfigureObstructions(modifications);
            ConfigureDoors(modifications);
            ConfigureLadders(modifications);
        }

        private void OnDestroy()
        {
            Dungeon?.RemoveNode(this);
        }
    }   
}
