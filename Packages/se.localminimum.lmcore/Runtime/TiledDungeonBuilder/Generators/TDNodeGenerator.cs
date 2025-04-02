using LMCore.Crawler;
using LMCore.EntitySM.State;
using LMCore.Extensions;
using LMCore.Inventory;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Integration;
using LMCore.TiledImporter;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public static class TDNodeGenerator
    {
        static GameObject ConfigurePotentiallyRotated(TDNode node, string className, string rotationClassName, TDNodeConfig config, out Direction direction)
        {
            if (config.Modifications.Any(mod => mod.Tile.Type == className))
            {
                direction = config.FirstValue(
                    rotationClassName,
                    props => props == null ? Direction.None : props.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection()
                );
                if (direction == Direction.None)
                {
                    direction = config.FirstValue(
                        className,
                        props => props == null ? Direction.None : props.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection()
                    );
                }

                if (direction == Direction.None)
                {
                    node.Log($"Getting Un-Rotated version of {className}", Debug.Log);
                    return node.Dungeon.Style.Get(
                        node.transform,
                        className,
                        node.NodeStyle);
                }
                else
                {
                    node.Log($"Getting Rotated {direction} version of {className}", Debug.Log);
                    return node.Dungeon.Style.Get(
                        node.transform,
                        className,
                        direction,
                        node.NodeStyle);

                }
            }

            direction = Direction.None;
            return null;
        }

        static void ConfigureContainer(
            TDNode node,
            GameObject tile,
            Direction anchor,
            Direction facingDirection,
            string containerClass,
            TDNodeConfig nodeConfig,
            bool blockingPassage,
            bool assumeContainer = true)
        {
            if (tile == null) return;

            var container = tile.GetComponent<TDContainer>();

            if (container == null)
            {
                // Only error if it seems Tiled assumes there should be a chest
                if (facingDirection != Direction.None && assumeContainer)
                {
                    node.Log("Container lacks script to configure", Debug.LogError);
                }
                return;
            }
            ;

            if (!container.Configure(
                nodeConfig,
                anchor,
                facingDirection,
                containerClass,
                node.modifications,
                blockingPassage))
            {

                GameObject.DestroyImmediate(container);
            }
        }

        static void ConfigureChest(TDNode node, TDNodeConfig nodeConfig, Transform parent)
        {
            var chest = ConfigurePotentiallyRotated(
                node,
                TiledConfiguration.instance.ChestClass,
                TiledConfiguration.instance.ObjContainerClass,
                nodeConfig,
                out Direction direction);

            ConfigureContainer(node, chest, Direction.Down, direction, TiledConfiguration.instance.ChestClass, nodeConfig, true);

            if (chest != null)
            {
                RemoveFloorLootNode(node);

                if (parent != null)
                {
                    chest.transform.SetParent(parent);
                }
            }
        }

        static void ConfigureFireplace(TDNode node, Transform parent)
        {
            if (!node.modifications.Any(mod => mod.Tile.Type == TiledConfiguration.instance.FirePlaceClass)) return;

            var fireplace = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.instance.FirePlaceClass,
                node.NodeStyle);

            if (fireplace != null)
            {
                RemoveFloorLootNode(node);

                if (parent != null)
                {
                    fireplace.transform.SetParent(parent);
                }
                var customSettings = node.Config.GetObjectProps(o => o.Type == TiledConfiguration.instance.FirePlaceClass).ToList();
                if (customSettings.Any(setting => setting.Bool(TiledConfiguration.instance.ObjInvisibleKey, false)))
                {
                    fireplace.transform.DestroyAllChildren(GameObject.DestroyImmediate);
                }

                var savingTrigger = fireplace.GetComponent<TDSavingTrigger>();
                if (savingTrigger != null)
                {
                    savingTrigger.Managed = customSettings
                        .Any(setting => setting.Bool(TiledConfiguration.instance.ObjManagedKey, false));
                }
            }
        }

        /// <summary>
        /// Position the entity in the level with its relevant anchor.
        /// </summary>
        static void ConfigureEntity(TDNode node, GridEntity entity)
        {
            if (entity != null)
            {
                if (entity.TransportationMode.HasFlag(TransportationMode.Flying))
                {
                    entity.AnchorDirection = Direction.None;
                    entity.Node = node;
                }
                else if (entity.AnchorDirection != Direction.None)
                {
                    if (!node.CanAnchorOn(entity, entity.AnchorDirection))
                    {
                        entity.AnchorDirection = Direction.None;
                        entity.Node = node;
                    }
                    else
                    {
                        entity.NodeAnchor = node.GetAnchor(entity.AnchorDirection);
                    }
                }
                else
                {
                    if (node.CanAnchorOn(entity, Direction.Down))
                    {
                        entity.NodeAnchor = node.GetAnchor(Direction.Down);
                    }
                    else
                    {
                        entity.AnchorDirection = Direction.None;
                        entity.Node = node;
                    }
                }
            }
        }

        private struct EnemyInfo
        {
            public string ClassId;
            public string Id;
            public Direction LookDirection;
            public StateType ForcedState;

            public static EnemyInfo From(TiledCustomProperties props) =>
                new EnemyInfo()
                {
                    ClassId = props.String(TiledConfiguration.instance.ObjEnemyClassIdKey, null),
                    Id = props.String(TiledConfiguration.instance.ObjEnemyIdKey, null),
                    LookDirection = props.Direction(TiledConfiguration.instance.ObjLookDirectionKey, TDEnumDirection.None).AsDirection(),
                    ForcedState = StateTypeExtensions.From(props.String(TiledConfiguration.instance.ObjEnemyForceStateKey, "none")),
                };

            public bool ConfiguresMe(TiledObjectLayer.TObject obj) =>
                obj.Type == TiledConfiguration.instance.EnemyClass &&
                obj.CustomProperties.String(TiledConfiguration.instance.ObjEnemyClassIdKey, null) == ClassId &&
                obj.CustomProperties.String(TiledConfiguration.instance.ObjEnemyIdKey, null) == Id;

            public string FullId => $"{ClassId}-{Id}";
        }

        static List<string> SpawnedEnemies = new List<string>();

        static void ConfigureEnemy(TDNode node, TDNodeConfig nodeConfig)
        {
            if (!node.modifications.Any(mod => mod.Tile.Type == TiledConfiguration.instance.EnemyClass)) return;

            var pool = TDEnemyPool.InstanceOrResource("EnemyPool");
            if (pool == null) return;

            foreach (var info in nodeConfig.GetObjectValues(
                TiledConfiguration.instance.EnemyClass,
                props => EnemyInfo.From(props)))
            {
                if (string.IsNullOrEmpty(info.ClassId)) continue;

                if (SpawnedEnemies.Contains(info.FullId))
                {
                    node.Log($"'{info.FullId}' is duplicate!", Debug.LogError);
                }
                var enemy = pool.GetInstance(info.ClassId, node.transform);
                if (enemy == null)
                {
                    node.Log($"Failed to spawn '{info.ClassId}' enemy", Debug.LogError);
                    continue;
                }
                SpawnedEnemies.Add(info.FullId);

                enemy.name = $"Enemy {info.ClassId}: {info.Id}";
                ConfigureEntity(node, enemy.GetComponent<GridEntity>());
                var configs = nodeConfig.GetObjectProps(obj => info.ConfiguresMe(obj)).ToList();
                // Do this last so that the enemy has become part of the dungeon structure
                enemy.Configure(info.Id, info.LookDirection, info.ForcedState, configs);
            }
        }

        static void ConfigureSafeZones(TDNode node, TDNodeConfig config)
        {
            var props = config.FirstObjectProps(obj => obj.Type == TiledConfiguration.instance.ObjSafeZoneClass);
            if (props == null) return;

            var size = props.Int(TiledConfiguration.instance.ObjSizeKey, 1);
            if (size > 0)
            {
                TDSafeZone zone = node.gameObject.AddComponent<TDSafeZone>();
                zone.Configure(size);
            }
            else
            {
                TDSafeZone zone = node.gameObject.GetComponentInChildren<TDSafeZone>();
                GameObject.DestroyImmediate(zone);
            }

        }

        /// <summary>
        /// Add any checkpoint or area marker on either a TDNode or an Anchor game object 
        /// </summary>
        static void ConfigureCheckpointsAndAreaMarkers(GameObject gameObject, TDNodeConfig nodeConfig)
        {
            // Inform user that their level is weirdly configured
            Direction direction = Direction.None;
            if (gameObject.GetComponent<TDNode>() != null)
            {

            }
            else
            {
                var anchor = gameObject.GetComponent<Anchor>();
                if (anchor == null)
                {
                    Debug.LogWarning($"{gameObject} is neither a TDNode or an Anchor so can't have checkpoints or area markers");
                    return;
                }
                direction = anchor.CubeFace;
            }

            var checkpoints = nodeConfig.GetObjectProps(obj =>
                obj.Type == TiledConfiguration.instance.ObjPathKey &&
                obj.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey, TDEnumDirection.None).AsDirection() == direction);

            foreach (var checkpoint in checkpoints)
            {
                gameObject
                    .AddComponent<TDPathCheckpoint>()
                    .Configure(checkpoint);
            }

            var areas = nodeConfig.GetObjectProps(obj =>
                obj.Type == TiledConfiguration.instance.ObjHomeAreaKey &&
                obj.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey, TDEnumDirection.None).AsDirection() == direction);

            foreach (var area in areas)
            {
                gameObject
                    .AddComponent<TDAreaMarker>()
                    .Configure(area);
            }

        }

        static void ConfigureNPC(TDNode node, TDNodeConfig nodeConfig)
        {
            if (!node.modifications.Any(mod => mod.Tile.Type == TiledConfiguration.instance.NPCClass)) return;

            var roster = NPCRoster.InstanceOrResource("NPCRoster");
            if (roster == null) return;

            foreach (var npcId in nodeConfig.GetObjectValues(
                TiledConfiguration.instance.NPCClass,
                props => props.String(TiledConfiguration.instance.ObjIdKey)))
            {
                var npc = roster.GetInstance(npcId, node.transform);
                if (npc == null) continue;

                ConfigureEntity(node, npc.GetComponent<GridEntity>());
            }
        }

        static void ConfigurePedistal(TDNode node, TDNodeConfig nodeConfig, Transform parent)
        {
            var pedistal = ConfigurePotentiallyRotated(
                node,
                TiledConfiguration.instance.PedistalClass,
                TiledConfiguration.instance.ObjContainerClass,
                nodeConfig,
                out Direction direction
            );

            ConfigureContainer(node, pedistal, Direction.Down, direction, TiledConfiguration.instance.PedistalClass, nodeConfig, true);

            if (pedistal != null)
            {
                RemoveFloorLootNode(node);

                if (parent != null)
                {
                    pedistal.transform.SetParent(parent);
                }
            }
        }

        static void ConfigurePillar(TDNode node, TDNodeConfig config)
        {
            var go = ConfigurePotentiallyRotated(
            node,
            TiledConfiguration.instance.PillarClass,
            TiledConfiguration.instance.PillarClass,
            config,
            out Direction _);

            if (go != null)
            {
                RemoveFloorLootNode(node);
            }
        }

        static void ConfigureWallButtons(TDNode node)
        {
            var buttons = node.modifications.Where(mod => mod.Tile.Type == TiledConfiguration.instance.WallButtonClass);

            foreach (var button in buttons)
            {
                var direction = button.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection();

                var go = node.Dungeon.Style.Get(
                    node.transform,
                    TiledConfiguration.instance.WallButtonClass,
                    direction,
                    node.NodeStyle
                );

                if (go != null)
                    go.GetComponent<TDActuator>()?.Configure(node, direction);
            }
        }

        static void ConfigureFence(TDNode node, Transform parent)
        {
            var fences = node.modifications.Where(mod => mod.Tile.Type == TiledConfiguration.instance.FenceClass);
            var customSettings = node.Config.GetObjectProps(o => o.Type == TiledConfiguration.instance.FenceClass).ToList();

            foreach (var fence in fences)
            {
                var direction = fence.Tile.CustomProperties.Direction(TiledConfiguration.instance.DirectionKey).AsDirection();

                var setting = customSettings
                    .FirstOrDefault(s => s.Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None).AsDirection().Either(direction, Direction.None));

                /*
                if (setting != null)
                {
                    Debug.LogWarning($"{node} has invisible fence direction {direction}: " +
                        $"{setting.Bool(TiledConfiguration.instance.ObjInvisibleKey)} ({setting.ClassType})");
                }
                */

                if (setting == null || !setting.Bool(TiledConfiguration.instance.ObjInvisibleKey, false))
                {
                    var go = node.Dungeon.Style.Get(
                        node.transform,
                        TiledConfiguration.instance.FenceClass,
                        direction,
                        node.NodeStyle
                    );

                    if (go != null)
                    {
                        if (parent != null)
                        {
                            go.transform.SetParent(parent);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates anchor and checks for checkpoints or area markers that should be applied
        /// </summary>
        static void ApplyAnchorRotation(GameObject obj, Direction direction, TDNodeConfig config)
        {
            if (obj == null) return;

            var anchor = obj.GetComponent<Anchor>();
            if (anchor == null) return;

            anchor.PrefabRotation = direction.AsYRotation();

            ConfigureCheckpointsAndAreaMarkers(obj, config);
        }

        /// <summary>
        /// Updates anchor and checks for checkpoints or area markers that should be applied
        /// </summary>
        static void ApplyAnchorRotation(GameObject obj, TDEnumOrientation orientation, TDNodeConfig config)
        {
            if (obj == null) return;

            var anchor = obj.GetComponent<Anchor>();
            if (anchor == null) return;

            anchor.PrefabRotation = orientation == TDEnumOrientation.Horizontal ? AnchorYRotation.CW : AnchorYRotation.None;

            ConfigureCheckpointsAndAreaMarkers(obj, config);
        }

        static bool ConfigureRamps(TDNode node, TDNodeConfig config, out Transform transform)
        {
            var ramp = node.RampModification;
            if (ramp == null)
            {
                transform = null;
                return false;
            }

            var downSlopeDirection = ramp.Tile.CustomProperties.Direction(TiledConfiguration.instance.DownDirectionKey).AsDirection();

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.instance.RampClass,
                ramp.Tile.CustomProperties.Elevation(TiledConfiguration.instance.ElevationKey),
                downSlopeDirection,
                node.NodeStyle
            );

            ApplyAnchorRotation(go, downSlopeDirection.Inverse(), config);

            if (go != null)
            {
                transform = go.transform;
            }
            else
            {
                transform = null;
            }
            return true;
        }

        static void ConfigureTeleporter(TDNode node)
        {
            var teleporterMod = node.modifications.FirstOrDefault(m => m.Tile.Type == TiledConfiguration.instance.TeleporterClass);

            if (teleporterMod != null)
            {
                var go = node.Dungeon.Style.Get(
                    node.transform,
                    TiledConfiguration.instance.TeleporterClass,
                    teleporterMod.Tile.CustomProperties.Transition(TiledConfiguration.instance.TransitionKey),
                    node.NodeStyle
                    );

                node.Log($"Teleporter Entry({node.HasActiveTeleporter}) Id({node.TeleporterWormholdId})", Debug.Log);

                if (go != null)
                {
                    RemoveFloorLootNode(node);
                }
            }

        }

        static void ConfigureLadders(TDNode node, TDNodeConfig config)
        {
            foreach (var tdDirection in TDEnumDirectionExtensions.PlanarDirections)
            {
                var direction = tdDirection.AsDirection();
                if (!node.HasLadder(direction)) continue;

                var dungeon = node.Dungeon;
                GameObject go = null;

                var upCoordinates = Direction.Up.Translate(node.Coordinates);
                if (!node.HasSide(Direction.Up) && dungeon.HasNodeAt(upCoordinates))
                {
                    var aboveNode = dungeon[upCoordinates];
                    var upForwardCoordinates = direction.Translate(upCoordinates);
                    if (!aboveNode.HasSide(direction) && !aboveNode.HasSide(Direction.Down)
                        && dungeon.HasNodeAt(upForwardCoordinates))
                    {
                        var upForwardNode = dungeon[upForwardCoordinates];
                        if (upForwardNode.HasSide(Direction.Down))
                        {
                            go = node.Dungeon.Style.Get(
                                node.transform,
                                TiledConfiguration.instance.LadderTopClass,
                                TiledConfiguration.instance.LadderClass,
                                direction,
                                node.NodeStyle
                            );
                        }
                    }
                }
                if (go == null)
                {
                    go = node.Dungeon.Style.Get(
                        node.transform,
                        TiledConfiguration.instance.LadderClass,
                        direction,
                        node.NodeStyle
                    );
                }

                ApplyAnchorRotation(go, direction, config);
            }
        }

        static void ConfigureDoors(TDNode node, TDNodeConfig config)
        {

            var doorInfo = node.modifications.FirstOrDefault(TDNode.DoorFilter);
            if (doorInfo == null) return;

            var orientation =
                doorInfo.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey);

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.instance.DoorClass,
                orientation,
                doorInfo.Tile.CustomProperties.InteractionOrDefault(TiledConfiguration.instance.InteractionKey, TDEnumInteraction.Closed),
                node.NodeStyle
            );

            var interactionLimitationDirection = config.FirstValue(
                TiledConfiguration.instance.DoorClass,
                props => props == null ?
                    TDEnumDirection.None :
                    props.Direction(TiledConfiguration.instance.InteractionDirectionKey, TDEnumDirection.None)
            ).AsDirection();

            var edge = doorInfo.Tile.CustomProperties
                .Direction(TiledConfiguration.instance.DirectionKey, TDEnumDirection.None)
                .AsDirection();

            var door = go != null ? go.GetComponent<TDDoor>() : null;

            if (door != null)
            {
                door.Configure(
                    node.modifications.Where(TDNode.DoorFilter).ToArray(),
                    edge,
                    interactionLimitationDirection
                );
            }

            if (go != null)
            {
                RemoveFloorLootNode(node);
            }
        }

        static Transform ConfigureWallCrack(TDNode node)
        {
            var wallCrack = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.WallCrackClass);
            if (wallCrack == null) return null;

            var orientation = wallCrack.Tile.CustomProperties
                .Orientation(TiledConfiguration.instance.OrientationKey, TDEnumOrientation.None);

            if (orientation == TDEnumOrientation.None)
            {
                Debug.LogWarning($"There's a wall crack configured at {node.Coordinates} but the tile {wallCrack} does not have any orientation");
                return null;
            }

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.instance.WallCrackClass,
                orientation,
                node.NodeStyle
            );

            // Prefab should be configured for vertical passage, this
            // realigns
            if (orientation == TDEnumOrientation.Horizontal)
            {
                var constraint = go.GetComponentInChildren<EntityConstraint>();
                if (constraint != null)
                {
                    // It doesn't matter if CW or CCW we just need to shift
                    // contrained directions one step
                    constraint.RotateRefused(AnchorYRotation.CW);
                }
            }

            RemoveFloorLootNode(node);
            ConfigureMovingPlatform(node, go);

            return go == null ? null : go.transform;
        }

        static void ConfigureObstructions(TDNode node)
        {
            var obstruction = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.ObstructionClass);
            if (obstruction == null) return;

            var orientation = obstruction.Tile.CustomProperties
                .Orientation(TiledConfiguration.instance.OrientationKey, TDEnumOrientation.None);

            GameObject go = null;
            if (orientation != TDEnumOrientation.None)
            {
                go = node.Dungeon.Style.Get(
                    node.transform,
                    TiledConfiguration.instance.ObstructionClass,
                    orientation,
                    node.NodeStyle
                );
            }
            else
            {
                var sides = TDSidesClass.From(
                    obstruction.Tile.CustomProperties.Class(TiledConfiguration.instance.SidesClassKey),
                    TiledNodeRoofRule.CustomProps);

                // We only know floor obstructions for now
                if (sides != null && sides.Down)
                {
                    var cardinals = sides.Directions.Where(d => d.IsPlanarCardinal()).ToList();
                    if (cardinals.Count == 0)
                    {
                        // Get island obstruction
                        go = node.Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.ObstructionClass,
                            node.NodeStyle
                        );
                    }
                    else if (cardinals.Count == 1)
                    {
                        // Get one sided obstruction
                        go = node.Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.ObstructionClass,
                            cardinals.First(),
                            node.NodeStyle
                        );
                    }
                    else
                    {
                        Debug.LogWarning($"{node} has obstruction but we don't know what prefab to get for {sides}");
                    }
                }
            }

            if (go != null)
            {
                RemoveFloorLootNode(node);
            }
        }

        static void ConfigureGrates(TDNode node)
        {
            var grate = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.GrateClass);
            if (grate == null) return;

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.InstanceOrCreate().GrateClass,
                grate.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey),
                node.NodeStyle
            );

            if (go != null)
            {
                RemoveFloorLootNode(node);
            }
        }

        static void ConfigureMovingPlatform(TDNode node, GameObject floor)
        {
            // We already have a moving platform
            if (floor.GetComponent<TDMovingPlatform>() != null)
            {
                return;
            }

            var mover = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.MovingPlatformClass);
            if (mover == null)
            {
                TDPassivePlatform.Configure(node, Direction.Down, floor);
                return;
            }

            var conf = node.Config;
            var platform = floor.AddComponent<TDMovingPlatform>();
            platform.Configure(conf);
        }

        static bool ConfigureShooter(TDNode node, TDNodeConfig config, Direction direction)
        {
            var shooter = node.modifications.FirstOrDefault(mod =>
                mod.Tile.Type == TiledConfiguration.InstanceOrCreate().ShooterClass &&
                mod.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().AnchorKey, TDEnumDirection.Unknown).AsDirection() == direction);

            if (shooter == null) return false;

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.InstanceOrCreate().ShooterClass,
                direction,
                node.NodeStyle
            );

            if (go == null) return false;

            go.GetComponentInChildren<TDProjectileShooter>()?.Configure(direction);

            ApplyAnchorRotation(go, direction, config);
            return true;
        }

        static bool ConfigureWallSpike(TDNode node, TDNodeConfig config, Direction direction)
        {
            var spikes = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.InstanceOrCreate().WallSpikeTrapClass);

            if (spikes == null || spikes.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().AnchorKey).AsDirection() != direction) return false;

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.instance.WallSpikeTrapClass,
                direction,
                node.NodeStyle
            );

            if (go == null) return false;

            go.GetComponent<TDSpikeTrap>()?.Configure(node, node.modifications);

            ApplyAnchorRotation(go, direction, config);
            return true;
        }

        static Transform ConfigureIllusory(TDNode node, TDNodeConfig config, Direction direction)
        {
            node.Log($"I have illusionary wall {direction}", Debug.Log);

            var go = node.Dungeon.Style.Get(
                node.transform,
                TiledConfiguration.InstanceOrCreate().IllusoryTileClass,
                direction,
                node.NodeStyle
            );

            if (go == null) return null;

            go.GetComponent<TDIllusoryCubeSide>().Configure(direction);

            ApplyAnchorRotation(go, direction, config);

            return go.transform;
        }

        static Transform ConfigureCube(TDNode node, TDNodeConfig config)
        {
            Transform down = null;
            var Dungeon = node.Dungeon;
            var hasTrapDoor = node.HasTrapDoor;
            var aboveNode = node.Coordinates + Vector3Int.up;
            var illusionMods = node.modifications.Where(TDNode.IllusoryFilter).ToList();

            var hasWallCrack = node.modifications.Any(mod => mod.Tile.Type == TiledConfiguration.instance.WallCrackClass);

            foreach (var direction in DirectionExtensions.AllDirections)
            {
                if (hasWallCrack && direction != Direction.Down) continue;

                var upNode = direction == Direction.Up && Dungeon.HasNodeAt(aboveNode) ? Dungeon[aboveNode] : null;

                if (direction == Direction.Down)
                {
                    if (hasTrapDoor)
                    {
                        var orientation = node.TrapdoorModification.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey, TDEnumOrientation.None);
                        var trapdoor = Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.TrapDoorClass,
                            orientation,
                            node.NodeStyle
                        );

                        if (trapdoor != null)
                        {
                            trapdoor.name = $"TrapDoor ({direction})";

                            var door = trapdoor.GetComponent<TDDoor>();

                            if (door != null)
                            {
                                door.Configure(
                                    node.modifications.Where(TDNode.TrapDoorFilter).ToArray(),
                                    Direction.None,
                                    Direction.None
                                );
                            }

                            ApplyAnchorRotation(trapdoor, orientation, config);

                            ConfigureMovingPlatform(node, trapdoor);

                            RemoveFloorLootNode(node);

                            down = trapdoor.transform;
                            continue;
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to spawn trapdoor at {node.Coordinates} with orientation {orientation} and style {node.NodeStyle}");
                        }
                    }
                    else if (node.HasFloorHole)
                    {
                        var hole = Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.HoleClass,
                            node.NodeStyle);

                        if (hole != null)
                        {
                            hole.name = $"Hole {direction}";
                        }

                        RemoveFloorLootNode(node);
                        ConfigureMovingPlatform(node, hole);
                        continue;
                    }
                    else if (hasWallCrack)
                    {
                        down = ConfigureWallCrack(node);
                        continue;
                    }
                    else if (node.HasPressurePlate)
                    {
                        var plate = Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.PressurePlateClass,
                            node.NodeStyle
                        );

                        if (plate != null)
                        {
                            plate.name = $"Pressure Plate ({direction})";

                            var pressurePlate = plate.GetComponent<TDActuator>();

                            if (pressurePlate != null)
                            {
                                pressurePlate.Configure(node, direction);
                            }

                            var orientation = node.PressurePlateModification.Tile.CustomProperties.Orientation(TiledConfiguration.instance.OrientationKey, TDEnumOrientation.None);
                            ApplyAnchorRotation(plate, orientation, config);

                            ConfigureMovingPlatform(node, plate);
                            down = plate.transform;
                            continue;
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to spawn preasure plate at {node.Coordinates} with style {node.NodeStyle}");
                        }
                    }
                    // Area markers and checkpoints configured inside
                    else if (ConfigureRamps(node, config, out var rampTransform))
                    {
                        down = rampTransform;
                        continue;
                    }
                    else if (node.HasStairs)
                    {
                        var stairsDirection = node.StairsModification.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().DirectionKey).AsDirection();
                        var stairs = Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.StairsClass,
                            stairsDirection,
                            node.NodeStyle
                        );
                        if (stairs != null)
                        {
                            stairs.name = $"TrapDoor UpIs({stairsDirection})";

                            ApplyAnchorRotation(stairs, stairsDirection, config);

                            RemoveFloorLootNode(node);

                            down = stairs.transform;
                            continue;
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to spawn stairs at {node.Coordinates} {stairsDirection} with style {node.NodeStyle}");
                        }
                    }
                }
                else if (direction == Direction.Up)
                {
                    if (node.HasSide(Direction.Up) && upNode)
                    {
                        if (upNode.HasIllusion(Direction.Down))
                        {
                            ConfigureIllusory(node, config, Direction.Up);
                            continue;
                        }

                    }
                }

                if (illusionMods.Any(mod => mod.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().DirectionKey, TDEnumDirection.None).AsDirection() == direction))
                {
                    var illusoryTransform = ConfigureIllusory(node, config, direction);
                    if (direction == Direction.Down)
                    {
                        down = illusoryTransform;
                    }

                    continue;
                }

                if (!node.HasSide(direction)) continue;

                if (node.HasSpikes(direction))
                {
                    var spikes = Dungeon.Style.Get(
                        node.transform,
                        TiledConfiguration.instance.SpikeTrapClass,
                        direction,
                        node.NodeStyle
                    );

                    if (spikes != null)
                    {
                        spikes.name = $"Spikes ({direction})";

                        spikes.GetComponent<TDSpikeTrap>()?.Configure(node, node.modifications);

                        // Spikes don't have any rotation but could have anchor with checkpoint / area marker
                        ConfigureCheckpointsAndAreaMarkers(spikes, config);

                        if (direction == Direction.Down)
                        {
                            down = spikes.transform;
                        }

                        continue;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to spawn spikes at {node.Coordinates} {direction} with style {node.NodeStyle}");
                    }
                }

                if (direction == Direction.Up && upNode != null && (upNode.HasTrapDoor || upNode.HasFloorHole)) continue;

                if (direction.IsPlanarCardinal())
                {
                    var neighbour = direction.Translate(node.Coordinates);
                    var neighbourConfig = Dungeon.GetNodeConfig(neighbour, false);
                    var hasAlcove = neighbourConfig
                        ?.Modifications
                        .Any(nMod =>
                            nMod.Tile.Type == TiledConfiguration.instance.AlcoveClass
                            && nMod.Tile.CustomProperties.Direction(TiledConfiguration.instance.AnchorKey).AsDirection() == direction.Inverse()
                        ) ?? false;

                    if (hasAlcove)
                    {
                        // TODO: Possibly it should get its styling from the neighbour tile rather than this
                        var alcove = Dungeon.Style.Get(
                            node.transform,
                            TiledConfiguration.instance.AlcoveClass,
                            direction,
                            node.NodeStyle);

                        alcove.name = direction.ToString();

                        ConfigureContainer(
                            node,
                            alcove,
                            direction,
                            direction.Inverse(),
                            TiledConfiguration.instance.AlcoveClass,
                            neighbourConfig,
                            false,
                            false);

                        ApplyAnchorRotation(alcove, direction, config);

                        continue;
                    }
                    else if (ConfigureWallSpike(node, config, direction) || ConfigureShooter(node, config, direction))
                    {
                        continue;
                    }
                }

                var go = Dungeon.Style.Get(
                    node.transform,
                    TiledConfiguration.instance.BaseTileClass,
                    direction,
                    node.NodeStyle);

                if (go == null)
                {
                    Debug.LogError($"Failed to generate {node.Coordinates} side {direction} with style '{node.NodeStyle}'");
                }
                else
                {

                    go.name = direction.ToString();
                    if (direction.IsPlanarCardinal())
                    {
                        ApplyAnchorRotation(go, direction, config);
                    }
                    else if (direction == Direction.Down)
                    {
                        down = go.transform;
                    }
                }

                if (direction == Direction.Up && upNode != null)
                {
                    var upIsMovingFloor = upNode.GetComponentInChildren<TDMovingPlatform>();
                    if (upIsMovingFloor != null)
                    {
                        upIsMovingFloor.AddAttachedObject(go.transform, direction);
                    }
                    var upIsPassiveFloor = upNode.GetComponentInChildren<TDPassivePlatform>();
                    if (upIsPassiveFloor != null)
                    {
                        upIsPassiveFloor.Backside = go.transform;
                    }
                }
                else if (direction == Direction.Down)
                {
                    ConfigureMovingPlatform(node, go);
                }
            }

            return down;
        }

        public static void RemoveFloorLootNode(TDNode node)
        {
            var anchor = node.GetAnchor(Direction.Down);
            if (anchor == null) return;

            var floorloot = anchor.GetComponent<FloorLootNode>();
            if (floorloot == null) return;

            Debug.Log($"Removing floor loot from {anchor}");

            GameObject.DestroyImmediate(floorloot.inventory);
            GameObject.DestroyImmediate(floorloot);

            var worldDisplay = anchor.GetComponent<WorldInventoryDisplay>();
            if (worldDisplay != null)
            {
                foreach (var location in worldDisplay.Locations)
                {
                    GameObject.DestroyImmediate(location.gameObject);
                }
                GameObject.DestroyImmediate(worldDisplay);
            }

            GameObject.DestroyImmediate(floorloot);
        }

        public static void ConfigureWallWedges(TDNode node)
        {
            var wallWedgeModification = node.modifications.FirstOrDefault(mod => mod.Tile.Type == TiledConfiguration.instance.CornerWedgeClass);
            if (wallWedgeModification == null) return;

            var corners = wallWedgeModification.Tile.CustomProperties.Class(TiledConfiguration.instance.CornerClassKey);
            if (corners == null)
            {
                Debug.LogWarning($"{node} has a modification tile that is a corner wedge class without any corners custom property class");
                return;
            }

            var illusionMods = node.modifications.Where(TDNode.IllusoryFilter).ToList();

            var dungeon = node.Dungeon;
            var offset = 0.5f * dungeon.GridSize;
            foreach (var cornerDirection in TDCornersClass.From(corners).Corners)
            {
                bool skip = false;

                foreach (var direction in cornerDirection.Directions())
                {
                    if (illusionMods.Any(mod => mod.Tile.CustomProperties.Direction(TiledConfiguration.InstanceOrCreate().DirectionKey, TDEnumDirection.None).AsDirection() == direction))
                    {
                        skip = true;
                        break;
                    }

                    var neighbour = direction.Translate(node.Coordinates);
                    var neighbourConfig = dungeon.GetNodeConfig(neighbour, false);
                    if (neighbourConfig
                        ?.Modifications
                        .Any(nMod => nMod.Tile.Type == TiledConfiguration.instance.WallCrackClass) == true)
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip) continue;

                var go = dungeon.Style.Get(
                    node.transform,
                    TiledConfiguration.instance.CornerWedgeClass,
                    node.NodeStyle);

                go.name = $"Wall Wedge {cornerDirection} corner";

                // TODO: These hard-coded rotations might not be inherit for everyone.
                // It just assumes the rotation I had while making my wedge
                switch (cornerDirection)
                {
                    case Corner.NorthWest:
                        go.transform.localPosition = new Vector3(-offset, 0, offset);
                        go.transform.localEulerAngles = new Vector3(-90, 0);
                        break;
                    case Corner.NorthEast:
                        go.transform.localPosition = new Vector3(offset, 0, offset);
                        go.transform.localEulerAngles = new Vector3(-90, 90);
                        break;
                    case Corner.SouthEast:
                        go.transform.localPosition = new Vector3(offset, 0, -offset);
                        go.transform.localEulerAngles = new Vector3(-90, 180);
                        break;
                    case Corner.SouthWest:
                        go.transform.localPosition = new Vector3(-offset, 0, -offset);
                        go.transform.localEulerAngles = new Vector3(-90, -90);
                        break;
                }
            }
        }

        public static void ConfigureCustom(TDNode node, TDNodeConfig config)
        {
            foreach (var props in config.GetObjectProps(o => o.Type == TiledConfiguration.instance.ObjCustomClass))
            {
                if (props == null) continue;

                var contentId = props.String(TiledConfiguration.instance.ObjCustomIdKey);
                if (contentId == null) continue;

                TDCustomContent.instance.AddCustom(node, contentId, props);
            }
        }

        public static void Reset()
        {
            SpawnedEnemies.Clear();
        }

        public static void Configure(
            TDNode node,
            TiledTile tile,
            TDNodeConfig config,
            TiledDungeon dungeon
        )
        {
            node.tile = tile;
            node.modifications = config.Modifications;
            node.Points = config.Points;
            node.Rects = config.Rects;

            node.Dungeon = dungeon;

            node.UpdateSides(TDSidesClass.From(
                tile.CustomProperties.Classes[TiledConfiguration.instance.SidesClassKey],
                config.RoofRule
            ));

            node.transform.localPosition = node.Coordinates.ToPosition(dungeon.GridSize);
            node.name = $"TileNode Elevation {node.Coordinates.y} ({node.Coordinates.x}, {node.Coordinates.z})";

            var down = ConfigureCube(node, config);
            ConfigureWallWedges(node);
            ConfigureGrates(node);
            ConfigureObstructions(node);
            ConfigureDoors(node, config);
            ConfigureLadders(node, config);
            ConfigureFireplace(node, down);
            ConfigureFence(node, down);
            ConfigureTeleporter(node);
            ConfigureWallButtons(node);
            ConfigurePillar(node, config);
            ConfigurePedistal(node, config, down);
            ConfigureChest(node, config, down);
            ConfigureNPC(node, config);
            ConfigureEnemy(node, config);
            TDRelay.Configure(node);
            ConfigureCheckpointsAndAreaMarkers(node.gameObject, config);
            ConfigureSafeZones(node, config);
            ConfigureCustom(node, config);

            node.Log("Generated", Debug.Log);
        }
    }
}
