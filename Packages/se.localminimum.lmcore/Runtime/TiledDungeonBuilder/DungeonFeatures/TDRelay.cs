using LMCore.Crawler;
using LMCore.TiledDungeon.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDRelay : TDFeature
    {
        [SerializeField, HideInInspector]
        bool bidirectional;

        [SerializeField, HideInInspector]
        float restTime;
        public float Rest => restTime;

        [SerializeField, HideInInspector]
        List<Direction> inDirections = new List<Direction>();

        [SerializeField, HideInInspector]
        List<Direction> outDirections = new List<Direction>();

        public static void Configure(TDNode node)
        {
            var tConf = TiledConfiguration.InstanceOrCreate();

            var props = node
                .Config
                .GetObjectProps(o => o.Type == tConf.ObjRelayClass)
                .ToArray();

            if (props.Length == 0) return;

            var relay = node.gameObject.AddComponent<TDRelay>();

            relay.bidirectional = props
                .FirstOrDefault(prop => prop.Bools.ContainsKey(tConf.ObjBidirectionalKey))
                ?.Bool(tConf.ObjBidirectionalKey) ?? false;

            relay.restTime = props
                .FirstOrDefault(prop => prop.Floats.ContainsKey(tConf.ObjRestKey))
                ?.Float(tConf.ObjRestKey) ?? 0.0f;

            for (int i = 0; i < 9; i++)
            {
                var inKey = tConf.ObjInDirectionPatternKey.Replace("%", i.ToString());

                var inDirection = props
                    .FirstOrDefault(prop => prop.StringEnums.ContainsKey(inKey))
                    ?.Direction(inKey).AsDirection() ?? Direction.None;

                var outKey = tConf.ObjOutDirectionPatternKey.Replace("%", i.ToString());

                var outDirection = props
                    .FirstOrDefault(prop => prop.StringEnums.ContainsKey(outKey))
                    ?.Direction(outKey).AsDirection() ?? Direction.None;

                if (inDirection == Direction.None || outDirection == Direction.None) break;

                relay.inDirections.Add(inDirection);
                relay.outDirections.Add(outDirection);
            }
        }

        public override string ToString() =>
            $"Relay @ {Coordinates}: Bidirectional({bidirectional}) Rest({restTime}) Ins({string.Join(", ", inDirections)}) Outs({string.Join(", ", outDirections)})";

        [ContextMenu("Info")]
        void Info()
        {
            Debug.Log(this);
        }

        void Start()
        {
            InitStartCoordinates();
        }

        /// <summary>
        /// Answers if relay manages entry from a certain direction
        /// </summary>
        /// <param name="inDirection">entry direction. Note that if an object has a move direction, it needs to be inverted to check against this</param>
        /// <param name="outDirection">exit direction. This would correspond to an objects new move direction</param>
        /// <returns></returns>
        public bool Relays(Direction inDirection, out Direction outDirection)
        {
            var idx = inDirections.IndexOf(inDirection);
            if (idx >= 0)
            {
                outDirection = outDirections[idx];
                return true;
            }

            if (bidirectional)
            {
                idx = outDirections.IndexOf(inDirection);
                if (idx >= 0)
                {
                    outDirection = inDirections[idx];
                    return true;
                }
            }

            outDirection = Direction.None;
            return false;
        }
    }
}
