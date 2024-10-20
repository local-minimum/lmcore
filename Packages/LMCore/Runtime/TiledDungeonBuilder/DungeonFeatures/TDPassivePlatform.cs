using LMCore.Crawler;
using LMCore.TiledDungeon.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDPassivePlatform :  TDFeature
    {
        [SerializeField, HideInInspector]
        string movingPlatformId;

        [SerializeField, HideInInspector]
        Direction cubeFace;

        [HideInInspector]
        public Transform Backside;

        protected string PrefixLogMessage(string message) =>
            $"Plassive Platform to '{movingPlatformId}' @ {Coordinates}: {message}";

        void Start()
        {
            InitStartCoordinates();
            var platform = Node.Dungeon.GetComponentsInChildren<TDMovingPlatform>()
                .FirstOrDefault(mp => mp.Identifier == movingPlatformId);

            if (platform == null)
            {
                Debug.LogError(PrefixLogMessage("Could not find my moving platform"));
                return;
            }

            platform.AddAttachedObject(transform, cubeFace);
            if (Backside != null)
            {
                platform.AddAttachedObject(Backside, cubeFace.Inverse()); 
            }
        }
        
        public static void Configure(TDNode node, Direction direction, GameObject go)
        {
            var tConf = TiledConfiguration.InstanceOrCreate();

            var props = node
                .Config
                .GetObjectProps(o => o.Type == tConf.ObjPassivePlatformClass)
                .FirstOrDefault(o => o.Direction(tConf.DirectionKey, TDEnumDirection.None).AsDirection() == direction);

            if (props == null) return;

            var identifier = props.String(tConf.ObjBelongsToKey, null);

            Debug.Log($"Passive platform candidate @ {node.Coordinates} belonging to '{identifier}'");
            if (string.IsNullOrEmpty(identifier)) return;

            var platform = go.AddComponent<TDPassivePlatform>();
            platform.movingPlatformId = identifier;
            platform.cubeFace = direction;
        }
    }
}
