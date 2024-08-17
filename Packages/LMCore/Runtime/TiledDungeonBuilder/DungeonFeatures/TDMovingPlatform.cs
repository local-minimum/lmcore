using LMCore.Crawler;
using LMCore.IO;
using LMCore.TiledDungeon.Integration;
using UnityEngine;

namespace LMCore.TiledDungeon.DungeonFeatures
{
    public class TDMovingPlatform : MonoBehaviour
    {
        [SerializeField, Tooltip("Time waiting when invoking loop condition")]
        float loopDelay = 2f;

        [SerializeField, Tooltip("As Tile distance per second")]
        float moveSpeed = 1f;

        [SerializeField, HideInInspector]
        Vector3Int OriginCoordinates;

        Vector3Int CurrentCoordinates => GetComponentInParent<TDNode>().Coordinates;

        [SerializeField, HideInInspector]
        Direction MoveDirection = Direction.None;

        [SerializeField, HideInInspector]
        TDEnumLoop Loop = TDEnumLoop.None;

        [SerializeField, HideInInspector]
        TDEnumInteraction Interaction = TDEnumInteraction.Automatic;

        protected string PrefixLogMessage(string message) => $"Moving Platform {CurrentCoordinates} (origin {OriginCoordinates}): {message}";
        
        public void Configure(TDNodeConfig conf)
        {
            OriginCoordinates = conf.Coordinates;

            var platform = conf.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.MovingPlatformClass); 
            if (platform == null)
            {
                Debug.LogWarning(PrefixLogMessage("Could not find any configuration"));
                return;
            }

            MoveDirection = platform.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection();
            Loop = platform.Loop(TiledConfiguration.instance.LoopKey);
            Interaction = platform.Interaction(TiledConfiguration.instance.InteractionKey, TDEnumInteraction.Automatic);
            moveSpeed = platform.Float(TiledConfiguration.instance.VelocityKey, moveSpeed);
            loopDelay = platform.Float(TiledConfiguration.instance.PauseKey, loopDelay);
        }

        [SerializeField, HideInInspector]
        SerializableDictionary<Vector3Int, Direction> managedOffsetSides = new SerializableDictionary<Vector3Int, Direction>();

        public void AddAttachedObject(Transform attached, Direction cubeSide)
        {
            attached.transform.SetParent(transform);
            if (cubeSide == Direction.None) return;

            var otherNode = attached.GetComponentInParent<TDNode>();
            var offset = otherNode.Coordinates - GetComponentInParent<TDNode>().Coordinates;

            managedOffsetSides.Add(offset, cubeSide);
        }

    }
}
