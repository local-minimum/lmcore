using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDPassivePlatform :  TDFeature, IMovingCubeFace, IOnLoadSave
    {
        [SerializeField, HideInInspector]
        string movingPlatformId;

        [SerializeField, HideInInspector]
        Direction cubeFace;

        [HideInInspector]
        public Transform Backside;

        public Vector3 VirtualNodeCenter =>
            transform.position + (Dungeon?.GridSize ?? 3f) * 0.5f * Vector3.up;

        protected string PrefixLogMessage(string message) =>
            $"Plassive Platform to '{movingPlatformId}' @ {Coordinates}: {message}";

        bool registered = false;

        void RegisterToPlatform()
        {
            if (registered) return; 

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
            
            registered = true;
        }

        void Start()
        {
            InitStartCoordinates();

            RegisterToPlatform();
            var anchor = Anchor;
            if (anchor != null) {
                anchor.ManagingMovingCubeFace = this;
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

        // Needs to be higher than TDMovingPlatform
        public int OnLoadPriority => 50;

        public void OnLoad<T>(T save) where T : new()
        {
            RegisterToPlatform();
        }
    }
}
